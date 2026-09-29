using System;
using System.Collections.Generic;
using System.Linq;
using ChangeIcons.Shared;
using EFT;
using EFT.UI;
using UnityEngine;

namespace ChangeIcons.Client;

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
        public NameStyle Style;
        public Sprite Icon;
    }

    private static BotRules _rules;
    private static Dictionary<string, string> _names = new(StringComparer.OrdinalIgnoreCase);

    public static void Configure(BotConfig config)
    {
        if (config is not { Enabled: true })
        {
            _rules = null;
            _names = new(StringComparer.OrdinalIgnoreCase);
            return;
        }

        var modes = config.Modes ?? new List<string> { "solid", "gradient", "letters" };
        _rules = new BotRules
        {
            Share = Mathf.Clamp(config.Share, 0, 100),
            Icons = (config.Icons ?? new List<string>()).ToArray(),
            Solid = modes.Contains("solid"),
            Gradient = modes.Contains("gradient"),
            Letters = modes.Contains("letters"),
            Palettes = (config.Palettes ?? new List<List<string>>()).Where(p => p is { Count: > 0 }).Select(p => p.ToArray()).ToArray(),
            RandomColors = config.RandomColors,
            AnimateChance = Mathf.Clamp(config.AnimateChance, 0, 100),
            MaxSpeed = config.MaxSpeed,
        };
        _names = new Dictionary<string, string>(config.Names ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
        Plugin.Log.LogInfo($"PMC bot looks on: {_rules.Share}% of PMCs, {_rules.Icons.Length} icons, {_rules.Palettes.Length} presets, {_names.Count} named bots");
    }

    public static bool IsPmcBot(WildSpawnType role) => role is WildSpawnType.pmcBEAR or WildSpawnType.pmcUSEC;

    /// <summary>The look for a PMC bot's name, or null for the game's own.</summary>
    public static Look For(string name)
    {
        if (_rules == null || string.IsNullOrEmpty(name))
        {
            return null;
        }

        try
        {
            if (_names.TryGetValue(name.Trim(), out var key))
            {
                return Named(key);
            }

            var look = BotLookGenerator.For(name, _rules);
            if (look == null)
            {
                return null;
            }

            var colors = look.Colors.Select(c => ColorUtility.TryParseHtmlString(c, out var color) ? color : Color.white).ToArray();
            return new Look
            {
                Style = NameStyle.Create(colors, look.Mode == "letters", (float)look.Speed),
                Icon = IconFor(look.Icon),
            };
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"No look for bot \"{name}\": {e.Message}");
            return null;
        }
    }

    // A named bot wears one of your icons: its picture and its nickname colors
    private static Look Named(string key)
    {
        var category = int.TryParse(key, out var number)
            ? (EMemberCategory)number
            : Enum.TryParse(key, true, out EMemberCategory parsed) ? parsed : EMemberCategory.Default;
        var row = EFTHardSettings.Instance.ChatSpecialIconSettings.GetDataByMemberCategory(category);
        if (row == null)
        {
            return null;
        }

        return new Look
        {
            Style = IconTable.StyleFor(row.Category) ?? NameStyle.Create(new[] { row.IconColor }, false, 0),
            Icon = row.IconSprite,
        };
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
    public static void ApplyTo(PlayerNamePanel panel, Look look)
    {
        if (panel == null || look == null)
        {
            return;
        }

        NameColorizer.AttachStyle(panel._name, look.Style);
        if (look.Icon != null && panel._icon != null && panel._icon._icon != null)
        {
            panel._icon._icon.sprite = look.Icon;
        }
    }
}
