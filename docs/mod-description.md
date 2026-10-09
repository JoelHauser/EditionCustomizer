# Name Plate

**Make your name stand out.** Colors, animations, glow, clan tags and custom icons for your
nickname, plus a desktop editor to set it all up with a live preview. PMC bots get their own looks
too, so the names you run into aren't all plain gray.

![PMC names in Name Plate looks](names.png)

## What you can do

**Your name**
- One color, a gradient, or a different color for every letter
- Animations: scroll, pulse, wave, sparkle, rainbow, glitch and bounce
- Outline, glow, drop shadow and italic
- A clan tag in front of your name, like **[EZ] YourName**

**Your icon**
- Any of the game's ~1,100 icons, 107 extra icons (12 animated), or your own PNG
- Make brand-new icons, or recolor the existing ones (Standard, Edge of Darkness, Unheard…)
- Icon effects: pulse, glow, flip and float
- Hide the prestige badge, or swap it for your own picture

**Share it**
- Save your own presets
- Copy your look as a short code that friends can paste into their editor

## Where it shows up

**Your look**
- Main menu name and icon
- The settings dropdown where you pick your icon
- Everywhere else the game shows your member icon
- Your scav's name, if you want

**PMC bot looks**
- Death screen: the bot that killed you
- Kill list after the raid
- Flea market seller names
- Messages: "your item was bought by…"
- Dogtags you take, on the item and in the inspect window

Every bot's look comes from its name, so you'll recognize the same PMC from raid to raid. About a
third wear a clan tag, drawn from nearly 500 like **[EZ]**, **[L2P]**, **[1V9]** or **[GGEZ]**.
Traders, system messages and real players always keep the game's own look.

## How to use it

![The Name Plate editor](editor-icons.png)

1. Unzip into your SPT folder (the one with `EscapeFromTarkov.exe`).
2. Run **Name Plate.exe** from that folder.
3. **Icons:** pick an icon, or press **New**. Choose colors, animation, effects, tag and picture.
4. **Profile:** tick the icons your character has and pick the one to show. The SPT server must be closed for this step.
5. Press **Save** and start the game.

You can tweak your look with the game running; reopen the screen to see it. On the **Bots** page
you choose how many bots get a look and what they can use, or turn bot looks off.

## Requirements

- SPT 4.1.x
- The editor needs the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0); Windows offers it if it's missing.

## Good to know

- **Fika:** built to run alongside it; real players never get bot looks. Other players see your look only if they also run Name Plate.
- Works with Bot Callsigns Reloaded, Realistic PMC Names and MoxoPixel's Menu Overhaul.
- **Updating from Edition Customizer?** It's the same mod, renamed. Move your `icons.json` from
  `BepInEx/plugins/ChangeIcons` to `BepInEx/plugins/NamePlate`, then delete the old ChangeIcons folders and `ChangeIcons Editor.exe`.

Based on Evgencheg's Tarkov-Change-Icons. Source: https://github.com/JoelHauser/EditionCustomizer
