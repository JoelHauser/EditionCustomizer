using System;
using System.Collections.Generic;
using System.Linq;
using NamePlate.Shared;
using EFT;
using EFT.UI;
using UnityEngine;

namespace NamePlate.Client;

/// <summary>
/// PMC bot looks at display time. A look is worked out from the bot's name alone (see
/// BotLookGenerator), so nothing is stored on the bot or in the profile, the same name looks the
/// same every raid, and name mods (Bot Callsigns Reloaded, Realistic PMC Names) only change
/// which names there are.
/// </summary>
public static class BotLooks
{
    public class Look
    {
        /// <summary>The name's colors, without a tag: for dogtags and messages.</summary>
        public NameStyle Style;

        /// <summary>The same with the bot's clan tag and name effect, if it has them: for name labels.</summary>
        public NameStyle Tagged;

        public Sprite Icon;

        /// <summary>How the icon moves: its frames if it's animated, and an icon effect; null for still.</summary>
        public IconTable.IconMotion IconMotion;
    }

    private static BotRules _rules;

    public static void Configure(BotConfig config)
    {
        // No "bots" section (a fresh install, or icons.json never saved): on, with the defaults
        if (config == null)
        {
            _rules = BotDefaults.Rules(LibraryIcons());
            Plugin.Log.LogInfo($"PMC bot looks on with the defaults: {_rules.Icons.Length} icons, {_rules.Palettes.Length} presets");
            return;
        }

        if (!config.Enabled)
        {
            _rules = null;
            Plugin.Log.LogInfo("PMC bot looks are off in icons.json");
            return;
        }

        var modes = config.Modes ?? new List<string> { "solid", "gradient", "letters" };

        // Everything starts on: icons and motions that are new since the file was saved join in
        var pool = BotDefaults.Icons(LibraryIcons());
        var icons = config.KnownIcons == null
            ? pool
            : (config.Icons ?? new List<string>()).Concat(pool.Except(config.KnownIcons)).Distinct().OrderBy(i => i, StringComparer.Ordinal).ToArray();
        var motions = config.KnownMotions == null
            ? Shared.Motions.All
            : (config.Motions ?? new List<string>(Shared.Motions.All)).Concat(Shared.Motions.All.Except(config.KnownMotions)).Distinct().ToArray();

        _rules = new BotRules
        {
            Share = Mathf.Clamp(config.Share, 0, 100),
            Icons = icons,
            Solid = modes.Contains("solid"),
            Gradient = modes.Contains("gradient"),
            Letters = modes.Contains("letters"),
            Palettes = (config.Palettes ?? new List<List<string>>()).Where(p => p is { Count: > 0 }).Select(p => p.ToArray()).ToArray(),
            RandomColors = config.RandomColors,
            AnimateChance = Mathf.Clamp(config.AnimateChance, 0, 100),
            MaxSpeed = config.MaxSpeed,
            Motions = motions,
            TagChance = Mathf.Clamp(config.TagChance, 0, 100),
            TagColorChance = Mathf.Clamp(config.TagColorChance, 0, 100),
            Tags = config.Tags?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToArray(),
            FactionColors = config.FactionColors,
            EffectChance = Mathf.Clamp(config.EffectChance, 0, 100),
            Effects = config.Effects?.Select(e => e.Trim().ToLowerInvariant()).ToArray(),
            IconEffectChance = Mathf.Clamp(config.IconEffectChance, 0, 100),
            IconEffects = config.IconEffects?.Select(e => e.Trim().ToLowerInvariant()).ToArray(),
        };
        Plugin.Log.LogInfo($"PMC bot looks on: {_rules.Share}% of PMCs, {_rules.Icons.Length} icons, {_rules.Palettes.Length} presets");
    }

    // icons/library/*.png as the plugin refers to them
    private static IEnumerable<string> LibraryIcons()
    {
        var folder = System.IO.Path.Combine(Plugin.Folder, "icons", "library");
        return System.IO.Directory.Exists(folder)
            ? System.IO.Directory.GetFiles(folder, "*.png").Select(f => "icons/library/" + System.IO.Path.GetFileName(f))
            : Enumerable.Empty<string>();
    }

    public static bool IsPmcBot(WildSpawnType role) => role is WildSpawnType.pmcBEAR or WildSpawnType.pmcUSEC;

    /// <summary>The side as the look generator names it, or null when it isn't a PMC side.</summary>
    public static string SideOf(EPlayerSide side) =>
        side == EPlayerSide.Bear ? BotLookGenerator.Bear : side == EPlayerSide.Usec ? BotLookGenerator.Usec : null;

    /// <summary>
    /// The look for a PMC bot's name, or null for the game's own. The side, when known, picks faction
    /// colors. A real person (Fika: a friend, someone met in a raid) never gets a bot look: the
    /// look they share if they run the mod, else none.
    /// </summary>
    public static Look For(string name, string side = null, string profileId = null)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        if (SharedLooks.For(profileId, name) is { } shared)
        {
            return shared;
        }

        if (_rules == null || RealPlayers.IsReal(profileId, name))
        {
            return null;
        }

        try
        {
            var look = BotLookGenerator.For(name, _rules, side);
            if (look == null)
            {
                return null;
            }

            var colors = look.Colors.Select(c => ColorUtility.TryParseHtmlString(c, out var color) ? color : Color.white).ToArray();
            var style = NameStyle.Create(colors, look.Mode == "letters", (float)look.Speed, look.Motion, look.Reverse);
            var effects = EffectsOf(look);
            var icon = IconFor(look.Icon);
            var frames = IconTable.FramesOf(icon);
            var tagged = look.Tag == null && effects == null ? style : new NameStyle { Colors = style.Colors, Paint = style.Paint, Effects = effects, Tag = look.Tag };
            if (tagged != style)
            {
                tagged.SetTagColor(look.TagColor);
            }

            return new Look
            {
                Style = style,
                Tagged = tagged,
                Icon = icon,
                IconMotion = frames == null && look.IconEffect == null ? null
                    : new IconTable.IconMotion { Frames = frames, Effect = look.IconEffect, Tint = colors[0] },
            };
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"No look for bot \"{name}\": {e.Message}");
            return null;
        }
    }

    // A bot's one name effect, as the plugin draws effects for you
    private static NameEffects EffectsOf(BotLook look)
    {
        if (look.Effect == null)
        {
            return null;
        }

        var color = look.EffectColor != null && ColorUtility.TryParseHtmlString(look.EffectColor, out var c) ? c : Color.black;
        return look.Effect switch
        {
            NameEffectKinds.Outline => new NameEffects { Outline = color },
            NameEffectKinds.Glow => new NameEffects { Glow = color },
            NameEffectKinds.Shadow => new NameEffects { Shadow = true },
            NameEffectKinds.Bold => new NameEffects { Bold = true },
            NameEffectKinds.Italic => new NameEffects { Italic = true },
            NameEffectKinds.SmallCaps => new NameEffects { SmallCaps = true },
            _ => null,
        };
    }

    /// <summary>A bot's icon on an image: its picture, playing if animated, with its icon effect.</summary>
    public static void ShowIcon(UnityEngine.UI.Image image, Look look)
    {
        if (image == null || look?.Icon == null)
        {
            return;
        }

        image.sprite = look.Icon;
        IconAnimator.Attach(image, look.IconMotion);
    }

    private static Sprite IconFor(string entry)
    {
        if (string.IsNullOrEmpty(entry))
        {
            return null;
        }

        if (entry.StartsWith(BotLookGenerator.MemberPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Enum.TryParse(entry.Substring(BotLookGenerator.MemberPrefix.Length), true, out EMemberCategory category)
                ? IconTable.GameSprite(category)
                : null;
        }

        return IconTable.FileSprite(entry);
    }

    /// <summary>
    /// Puts a bot's look on a name panel (the death screen's killer): its icon on the panel's
    /// icon, its colors on the name.
    /// </summary>
    public static void ApplyTo(PlayerNamePanel panel, Look look, string name)
    {
        if (panel == null || look == null)
        {
            return;
        }

        NameColorizer.AttachStyle(panel._name, look.Tagged, name);
        if (panel._icon != null)
        {
            ShowIcon(panel._icon._icon, look);
        }
    }
}
