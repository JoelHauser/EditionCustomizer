# ChangeIcons

> **This fork (EditionCustomizer)** builds on [Evgencheg/Tarkov-Change-Icons](https://github.com/Evgencheg/Tarkov-Change-Icons)
> and adds custom icons, multi-color nicknames and a desktop editor. See
> [Custom icons](#custom-icons-this-branch) below. The original server command is unchanged
> apart from accepting the new custom flags.
>
> **Status:** everything builds, and the editor was tested against a copy of an SPT 4.1
> install: reading the game's icons, saving `icons.json`, and writing a profile. The client
> plugin has **not been run in game yet**. The first things to check there are the multi-color
> nicknames (`NameColorizer`), the live reload of `icons.json`, and PMC bot looks on the death
> screen and kill list.

Adds icons to your account: Sherpa, Emissary, Developer, Unheard or Edge of Darkness.

I saw many people on the SPT Discord server asking how to get these icons, so I created a mod that allows you to do so with a single chat message.

Server mod for SPT (SP-Tushonka) 4.1.

## Features

Message **SPT** in your friends list:
- `spt membercategory` - what you have now
- `spt membercategory list` - what you can add
- `spt membercategory 1026` or `spt membercategory unheard+uniqueid` - set your icons
- `spt membercategory default` - back to what your game edition has

Fully restart the game after a change. Choose which icon is shown in the profile settings.

Trader, Group, System and other service flags can't be set, they break the profile.

## Custom icons (this branch)

Three more pieces make the icons themselves customizable: new icons, your own pictures, and
nicknames in as many colors as you like.

- **ChangeIcons.Client** (BepInEx plugin, `BepInEx/plugins/ChangeIcons`) reads `icons.json` and
  changes the game's icon table: recolor or replace the existing icons, or add new ones on free
  flags (2048, 4096, ...). Nicknames can be one color, a gradient across the name, or a color per
  letter, still or moving. It reloads `icons.json` while the game runs; reopen a screen to see it.
  A changed game icon (Standard, Edge of Darkness, ...) is your look only: traders, system
  messages, chat bots, flea sellers and other players keep the game's own.
- **ChangeIcons Editor.exe** (in the SPT folder, next to `EscapeFromTarkov.exe`) edits all of that with a live preview. It reads every
  icon straight out of the game's `resources.assets` into `game-icons/` (about 1,100, plus the
  member icon table with the game's own colors), comes with a library of 31 extra icons
  (`icons/library`, drawn by `scripts/make_icons.py`), imports and recolors images, and sets which
  icons a character has and which one is shown. It only writes a profile while the SPT server is
  closed, and backs it up to `backups/` first.

**PMC bots** can get looks too: an icon from the library or the member icons, and a name in one
color, a gradient or a color per letter, from the presets or made-up colors, optionally moving.
A bot's look is worked out from its name (`src/Shared/BotLooks.cs`, compiled into both the plugin
and the editor), so the same name looks the same every raid and nothing is stored on bots or
profiles. It shows on the death screen ("killed by": icon and colors), in the kill list after a
raid (colors), on flea market sellers (icon and colors), whose names come from the same PMC
name lists -- so a seller looks like the PMC of the same name -- and on the dogtags you take
(name colors on the tile and in the inspect window). It is on out of the box (every library and member icon, every preset); the
editor's BOTS page narrows it down or turns it off. Because only the final name matters,
it works with [Bot Callsigns Reloaded](https://sp-mod.com/mod/1873/bot-callsigns-reloaded) and
[Realistic PMC Names](https://sp-mod.com/mod/3064/realistic-pmc-names); the editor previews real
names from whichever of them is installed.

With MoxoPixel's Menu Overhaul installed, its main-screen name gets your
icon and colors too (it hides the game's own name row and draws its own). That happens only when the
icon you show is one you've changed; otherwise its accent color stays.

The server command takes the new flags too: `spt membercategory unheard+uniqueid+2048`.

Limit: the settings dropdown draws its small icon from a fixed sprite sheet, so there a new icon
shows as its name only, and a replaced icon keeps the game's original small picture.

```
dotnet build src/ChangeIcons.Client/ChangeIcons.Client.csproj -c Release -p:SPTPath="C:\SPT" -p:DeployToSPT=true
dotnet publish src/ChangeIcons.Editor/ChangeIcons.Editor.csproj -c Release -o dist/editor
python scripts/make_icons.py
```

The client compiles against the install's Assembly-CSharp, so the install must have been launched
once. The editor reads the game's files with [AssetsTools.NET](https://github.com/nesrak1/AssetsTools.NET);
`src/ChangeIcons.Editor/Assets/classdata.tpk` is its class database, from
[UABEA](https://github.com/nesrak1/UABEA) (MIT).

## Installation

Unpack `EditionCustomizer-<version>.zip` into your SPT folder (the one with
`EscapeFromTarkov.exe`). You should get:

```
ChangeIcons Editor.exe                                       the editor
SPT_Runtime/user/mods/ChangeIcons/ChangeIcons.dll            server mod (the chat command)
BepInEx/plugins/ChangeIcons/ChangeIcons.Client.dll           client plugin (icons, colors, bots)
BepInEx/plugins/ChangeIcons/icons/library/*.png              31 extra icons
```

Then run **ChangeIcons Editor.exe** from your SPT folder. The first time it reads the game's icons
(a second or two), then everything is set up in the editor; press SAVE to write your
`icons.json`. The zip doesn't include an `icons.json`, so updating never overwrites yours.

The editor needs the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0);
if it's missing, Windows offers the download when you start the editor. The mods themselves
don't need it.

To make a release zip: `.\scripts\package.ps1 -SPTPath C:\SPT -Version 1.0.0` (writes
`dist\EditionCustomizer-1.0.0.zip`). It needs an SPT install that has been launched once, because
the client plugin compiles against the game's patched `Assembly-CSharp`, so it can't run on
GitHub Actions.

## Uninstallation

Delete `SPT_Runtime/user/mods/ChangeIcons`, `BepInEx/plugins/ChangeIcons` and `ChangeIcons Editor.exe`.

Your icons stay on the account. To reset them, send `spt membercategory default` (or untick them
in the editor's PROFILE page) before deleting the mod.

## Building

```
dotnet build src/ChangeIcons/ChangeIcons.csproj -c Release
```

Needs the .NET 10 SDK.

## Credits

- Evgencheg - author
- Made with help from an AI coding assistant

[MIT](LICENSE)
