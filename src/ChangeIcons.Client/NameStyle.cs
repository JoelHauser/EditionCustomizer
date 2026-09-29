using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ChangeIcons.Client;

/// <summary>
/// Several colors in a nickname. The editor's preview uses the same formulas; keep them in step.
/// </summary>
public class NameStyle
{
    public Color[] Colors;
    public bool Letters;
    public float Speed;

    /// <summary>Any number of colors; one gives a plain color (used for bots).</summary>
    public static NameStyle Create(Color[] colors, bool letters, float speed) =>
        new() { Colors = colors, Letters = letters, Speed = colors.Length > 1 ? Mathf.Max(0, speed) : 0 };

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
            : new NameStyle
            {
                Colors = colors.ToArray(),
                Letters = string.Equals(entry.ColorMode, "letters", StringComparison.OrdinalIgnoreCase),
                Speed = Mathf.Max(0, entry.Animate),
            };
    }

    /// <summary>
    /// Gradient color at <paramref name="x"/> (0 = left edge of the name, 1 = right edge).
    /// Moving, the colors wrap around so the last blends back into the first.
    /// </summary>
    public Color Gradient(float x, float time)
    {
        if (Speed <= 0)
        {
            return Sample(Mathf.Clamp01(x) * (Colors.Length - 1), wrap: false);
        }

        var t = Mathf.Repeat(x + time * Speed, 1f);
        return Sample(t * Colors.Length, wrap: true);
    }

    public Color Letter(int index, float time)
    {
        var shift = Speed <= 0 ? 0 : Mathf.FloorToInt(time * Speed * 4f);
        return Colors[((index + shift) % Colors.Length + Colors.Length) % Colors.Length];
    }

    private Color Sample(float position, bool wrap)
    {
        var i = Mathf.FloorToInt(position);
        var f = position - i;
        var a = Colors[Mathf.Clamp(i, 0, Colors.Length - 1)];
        var next = i + 1;
        var b = wrap ? Colors[next % Colors.Length] : Colors[Mathf.Clamp(next, 0, Colors.Length - 1)];
        return Color.Lerp(a, b, f);
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

        var builder = new StringBuilder();
        var visible = 0;
        var count = 0;
        foreach (var ch in text)
        {
            if (!char.IsWhiteSpace(ch))
            {
                count++;
            }
        }

        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                builder.Append(ch);
                continue;
            }

            var color = Letters ? Letter(visible, 0) : Gradient(count <= 1 ? 0 : (visible + 0.5f) / count, 0);
            builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(color)).Append('>').Append(ch).Append("</color>");
            visible++;
        }

        return builder.ToString();
    }
}
