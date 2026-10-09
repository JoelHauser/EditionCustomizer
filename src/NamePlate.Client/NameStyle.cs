using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NamePlate.Shared;
using UnityEngine;

namespace NamePlate.Client;

/// <summary>Looks on the letters themselves rather than their colors.</summary>
public class NameEffects
{
    public Color? Outline;
    public Color? Glow;
    public bool Shadow;
    public bool Bold;
    public bool Italic;
    public bool SmallCaps;

    /// <summary>Outline, glow and shadow need the label's material changed; the rest don't.</summary>
    public bool NeedsMaterial => Outline != null || Glow != null || Shadow;

    public bool Any => NeedsMaterial || Bold || Italic || SmallCaps;
}

/// <summary>
/// The colors of a nickname, and how they move. The math is the shared NamePaint, the same code the
/// editor's preview runs.
/// </summary>
public class NameStyle
{
    public Color[] Colors;
    public NamePaint Paint;

    /// <summary>Outline, glow, shadow, bold ... or null for none.</summary>
    public NameEffects Effects;

    /// <summary>A clan tag shown before the name, without brackets; null for none.</summary>
    public string Tag;

    private readonly double[] _rgb = new double[3];

    public bool Moving => Paint.Moving;

    public bool MovesLetters => Paint.MovesLetters;

    /// <summary>Any number of colors; one gives a plain color unless it waves or sparkles.</summary>
    public static NameStyle Create(Color[] colors, bool letters, float speed, string motion = null, bool reverse = false) =>
        new()
        {
            Colors = colors,
            Paint = new NamePaint(colors.Select(c => new double[] { c.r, c.g, c.b }).ToArray(), letters, motion, speed, reverse),
        };

    /// <summary>
    /// An icons.json entry's name style, or null when the name is just the row's one color. One
    /// color still makes a style if it moves, has effects or a tag.
    /// </summary>
    public static NameStyle From(IconEntry entry, Color rowColor)
    {
        var colors = new List<Color>();
        foreach (var text in entry.Colors ?? new List<string>())
        {
            if (ColorUtility.TryParseHtmlString(text, out var color))
            {
                colors.Add(color);
            }
            else
            {
                Plugin.Log.LogWarning($"Icon {entry.Category}: \"{text}\" isn't a color, use #RRGGBB");
            }
        }

        var effects = EffectsOf(entry);
        var tag = Shared.BotTags.Clean(entry.Tag);
        var moving = entry.Animate > 0 && !string.IsNullOrEmpty(entry.Motion);

        if (colors.Count < 2)
        {
            if (effects == null && tag == null && !moving)
            {
                return null;
            }

            colors = new List<Color> { colors.Count == 1 ? colors[0] : rowColor };
        }

        var style = Create(colors.ToArray(), string.Equals(entry.ColorMode, "letters", StringComparison.OrdinalIgnoreCase),
            Mathf.Max(0, entry.Animate), entry.Motion, entry.Reverse);
        style.Effects = effects;
        style.Tag = tag;
        return style;
    }

    private static NameEffects EffectsOf(IconEntry entry)
    {
        var effects = new NameEffects
        {
            Outline = ParseColor(entry, entry.Outline),
            Glow = ParseColor(entry, entry.Glow),
            Shadow = entry.Shadow,
            Bold = entry.Bold,
            Italic = entry.Italic,
            SmallCaps = entry.SmallCaps,
        };
        return effects.Any ? effects : null;
    }

    private static Color? ParseColor(IconEntry entry, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        if (ColorUtility.TryParseHtmlString(text, out var color))
        {
            return color;
        }

        Plugin.Log.LogWarning($"Icon {entry.Category}: \"{text}\" isn't a color, use #RRGGBB");
        return null;
    }

    /// <summary>The color at x (0..1 across the name) on a visible letter, at a time in seconds.</summary>
    public Color At(float x, int letter, float time)
    {
        Paint.At(x, letter, time, _rgb);
        return new Color((float)_rgb[0], (float)_rgb[1], (float)_rgb[2], 1f);
    }

    /// <summary>
    /// The same colors as TextMeshPro rich text, for places that take a string: one color tag per
    /// letter, still (menus there don't animate).
    /// </summary>
    public string RichText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var count = text.Count(ch => !char.IsWhiteSpace(ch));
        var builder = new StringBuilder();
        var visible = 0;
        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                builder.Append(ch);
                continue;
            }

            var color = At(count <= 1 ? 0 : (visible + 0.5f) / count, visible, 0);
            builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(color)).Append('>').Append(ch).Append("</color>");
            visible++;
        }

        return builder.ToString();
    }
}
