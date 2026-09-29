<#
.SYNOPSIS
    Builds everything and makes the release zip, laid out to unpack over an SPT folder.

.EXAMPLE
    .\scripts\package.ps1 -SPTPath C:\SPT

.NOTES
    -SPTPath is an SPT 4.1 install that has been launched once: the client plugin compiles
    against its Assembly-CSharp, which the SPT Launcher patches on first start. Needs the
    .NET 10 SDK.

    The zip leaves out icons.json on purpose: unpacking over an existing install would replace
    the user's own icons. The plugin runs without one, and the editor writes it on first save.
#>
param(
    [string] $SPTPath = $env:SPT_PATH,
    [string] $Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent

if (-not $SPTPath -or -not (Test-Path (Join-Path $SPTPath "EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll"))) {
    throw "Pass -SPTPath: an SPT 4.1 install folder that has been launched once (e.g. -SPTPath C:\SPT)."
}

# The .NET 10 SDK: on PATH, or a user-local install
$dotnet = "dotnet"
$local = Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"
if (Test-Path $local) {
    $dotnet = $local
}

function Invoke-Step([string] $title, [scriptblock] $step) {
    Write-Host "== $title" -ForegroundColor Cyan
    & $step
    if ($LASTEXITCODE -ne 0) {
        throw "$title failed"
    }
}

Invoke-Step "Server mod" { & $dotnet build "$root\src\ChangeIcons\ChangeIcons.csproj" -c Release -v q -nologo }
Invoke-Step "Client plugin" { & $dotnet build "$root\src\ChangeIcons.Client\ChangeIcons.Client.csproj" -c Release -v q -nologo "-p:SPTPath=$SPTPath" }
$editorOut = Join-Path $root "dist\editor"
Invoke-Step "Editor" { & $dotnet publish "$root\src\ChangeIcons.Editor\ChangeIcons.Editor.csproj" -c Release -v q -nologo -o $editorOut }

# The layout of an SPT install
$staging = Join-Path $root "dist\staging"
Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
$server = New-Item -ItemType Directory -Force (Join-Path $staging "SPT_Runtime\user\mods\ChangeIcons")
$plugin = New-Item -ItemType Directory -Force (Join-Path $staging "BepInEx\plugins\ChangeIcons")
$library = New-Item -ItemType Directory -Force (Join-Path $plugin "icons\library")

Copy-Item "$root\src\ChangeIcons\bin\Release\net10.0\ChangeIcons.dll" $server
Copy-Item "$root\LICENSE" $server
Copy-Item "$root\src\ChangeIcons.Client\bin\Release\ChangeIcons.Client.dll" $plugin
Copy-Item (Join-Path $editorOut "ChangeIcons.Editor.exe") (Join-Path $plugin "ChangeIcons Editor.exe")
Copy-Item "$root\src\ChangeIcons.Client\icons\library\*.png" $library
Copy-Item "$root\LICENSE" $plugin

$zip = Join-Path $root "dist\EditionCustomizer-$Version.zip"
Remove-Item $zip -Force -ErrorAction SilentlyContinue

# Each file added by hand with a "/" path: in Windows PowerShell 5.1 both Compress-Archive and
# ZipFile.CreateFromDirectory write "\" into entry names, which some extractors and mod managers
# get wrong.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::Open($zip, [IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem $staging -Recurse -File | ForEach-Object {
        $entry = $_.FullName.Substring($staging.Length + 1).Replace("\", "/")
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $_.FullName, $entry, [IO.Compression.CompressionLevel]::Optimal)
    }
}
finally {
    $archive.Dispose()
}
Remove-Item $staging -Recurse -Force

Write-Host ""
Write-Host "Release archive: $zip" -ForegroundColor Green
$archive = [IO.Compression.ZipFile]::OpenRead($zip)
try {
    if ($archive.Entries | Where-Object { $_.FullName.Contains("\") }) {
        throw "The zip has backslashes in its paths"
    }

    $archive.Entries | Where-Object { $_.Length -gt 0 -and $_.FullName -notlike "*/icons/library/*" } | ForEach-Object { "  $($_.FullName)  ($([math]::Round($_.Length / 1KB)) KB)" }
    "  BepInEx/plugins/ChangeIcons/icons/library/  ($(@($archive.Entries | Where-Object { $_.FullName -like '*/icons/library/*.png' }).Count) icons)"
}
finally {
    $archive.Dispose()
}
