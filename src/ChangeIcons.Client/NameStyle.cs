using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChangeIcons.Shared;
using UnityEngine;

namespace ChangeIcons.Client;

/// <summary>
/// The colors of a nickname, and how they move. The math is the shared NamePaint, the same code the
/// editor's preview runs.
/// </summary>
public class NameStyle
{
    public Color[] Colors;
    public NamePaint Paint;

    private readonly double[] _rgb = new double[3];

    public bool Moving => Paint.Moving;

    /// <summary>Any number of colors; one gives a plain color unless it waves or sparkles.</summary>
    public static NameStyle Create(Color[] colors, bool letters, float speed, string motion = null, bool reverse = false) =>
        new()
        {
            Colors = colors,
            Paint = new NamePaint(colors.Select(c => new double[] { c.r, c.g, c.b }).ToArray(), letters, motion, speed, reverse),
        };

    /// <summary>An icons.json entry's multi-color name, or null for a plain one.</summary>
    public static NameStyle From(IconEntry entry)
    {
        if (entry.Colors == null || entry.Colors.Count < 2)
        {
            return null;
        }

        var colors = new List<Color>();
        foreach (var text in entry.Colors)
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

        return colors.Count < 2
            ? null
            : Create(colors.ToArray(), string.Equals(entry.ColorMode, "letters", StringComparison.OrdinalIgnoreCase),
                Mathf.Max(0, entry.Animate), entry.Motion, entry.Reverse);
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
