# ChangeIcons

> **This fork (EditionCustomizer)** builds on [Evgencheg/Tarkov-Change-Icons](https://github.com/Evgencheg/Tarkov-Change-Icons)
> and adds custom icons, multi-color nicknames and a desktop editor. See
> [Custom icons](#custom-icons-this-branch) below. The original server command is unchanged
> apart from accepting the new custom flags.
>
> **Status:** everything builds, and the editor was tested against a copy of an SPT 4.1
> install: reading the game's icons, saving `icons.json`, and writing a profile. The client
> plugin has **not been run in game yet**. The first things to check there are the multi-color
> nicknames (`NameColorizer`) and the live reload of `icons.json`.

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
- **ChangeIcons Editor.exe** (same folder) edits all of that with a live preview. It reads every
  icon straight out of the game's `resources.assets` into `game-icons/` (about 1,100, plus the
  member icon table with the game's own colors), comes with a library of 31 extra icons
  (`icons/library`, drawn by `scripts/make_icons.py`), imports and recolors images, and sets which
  icons a character has and which one is shown. It only writes a profile while the SPT server is
  closed, and backs it up to `backups/` first.

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

Unpack the [release archive](https://github.com/Evgencheg/Tarkov-Change-Icons/releases/latest) into your SPT game folder.

You should get `SPT_Runtime/user/mods/ChangeIcons/ChangeIcons.dll`.

## Uninstallation

Delete `SPT_Runtime/user/mods/ChangeIcons`.

Your icons stay on the account. To reset them, send `spt membercategory default` before deleting the mod.

## Building

```
dotnet build src/ChangeIcons/ChangeIcons.csproj -c Release
```

Needs the .NET 10 SDK.

## Credits

- Evgencheg - author
- Made with help from an AI coding assistant

[MIT](LICENSE)
