using EFT.UI;
using TMPro;
using UnityEngine;

namespace ChangeIcons.Client;

/// <summary>
/// Sits on a nickname label and recolors its letters after TextMeshPro builds them, so it works
/// whoever sets the text. Removing it (or a category without a style) gives the label back its
/// plain color.
/// </summary>
public class NameColorizer : MonoBehaviour
{
    private TMP_Text _text;
    // Null for a style set directly (a bot's), which a reload of icons.json leaves alone
    private EMemberCategory? _category;
    private NameStyle _style;
    private bool _subscribed;

    public static void Attach(TMP_Text label, EMemberCategory? category)
    {
        var colorizer = label.GetComponent<NameColorizer>();
        var style = category == null ? null : IconTable.StyleFor(category.Value);
        if (style == null)
        {
            if (colorizer != null)
            {
                colorizer.Clear();
            }

            return;
        }

        colorizer ??= label.gameObject.AddComponent<NameColorizer>();
        colorizer._text = label;
        colorizer._category = category;
        colorizer._style = style;
        colorizer.Subscribe();
        colorizer.Recolor();
    }

    /// <summary>
    /// Colors a label with a given style rather than a category's (a PMC bot's look). Null puts
    /// the label back to plain.
    /// </summary>
    public static void AttachStyle(TMP_Text label, NameStyle style)
    {
        if (label == null)
        {
            return;
        }

        var colorizer = label.GetComponent<NameColorizer>();
        if (style == null)
        {
            if (colorizer != null)
            {
                colorizer.Clear();
            }

            return;
        }

        colorizer ??= label.gameObject.AddComponent<NameColorizer>();
        colorizer._text = label;
        colorizer._category = null;
        colorizer._style = style;
        colorizer.Subscribe();
        colorizer.Recolor();
    }

    /// <summary>icons.json reloaded: pick up the new style, or go back to plain.</summary>
    public static void RefreshAll()
    {
        foreach (var colorizer in FindObjectsOfType<NameColorizer>(true))
        {
            if (colorizer._category is not { } category)
            {
                continue;
            }

            colorizer._style = IconTable.StyleFor(category);
            if (colorizer._style == null)
            {
                colorizer.Clear();
            }
            else
            {
                colorizer.Recolor();
            }
        }
    }

    private void Clear()
    {
        _style = null;
        Unsubscribe();
        if (_text != null)
        {
            _text.ForceMeshUpdate();
        }
    }

    private void Subscribe()
    {
        if (!_subscribed)
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
            _subscribed = true;
        }
    }

    private void Unsubscribe()
    {
        if (_subscribed)
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            _subscribed = false;
        }
    }

    private void OnEnable()
    {
        if (_style != null)
        {
            Subscribe();
        }
    }

    private void OnDisable() => Unsubscribe();

    private void OnDestroy() => Unsubscribe();

    // Fired from inside TextMeshPro's mesh build, which has just written the plain color back
    private void OnTextChanged(Object changed)
    {
        if (changed == _text)
        {
            Recolor();
        }
    }

    private void LateUpdate()
    {
        if (_style is { Speed: > 0 })
        {
            Recolor();
        }
    }

    private void Recolor()
    {
        if (_style == null || _text == null || !_text.isActiveAndEnabled)
        {
            return;
        }

        var info = _text.textInfo;
        if (info == null || info.characterCount == 0)
        {
            return;
        }

        // The name's horizontal extent, for the gradient
        var minX = float.MaxValue;
        var maxX = float.MinValue;
        for (var i = 0; i < info.characterCount; i++)
        {
            var ch = info.characterInfo[i];
            if (ch.isVisible)
            {
                minX = Mathf.Min(minX, ch.bottomLeft.x);
                maxX = Mathf.Max(maxX, ch.topRight.x);
            }
        }

        var width = Mathf.Max(maxX - minX, 0.001f);
        var time = Time.unscaledTime;
        var letter = 0;

        for (var i = 0; i < info.characterCount; i++)
        {
            var ch = info.characterInfo[i];
            if (!ch.isVisible)
            {
                continue;
            }

            var colors = info.meshInfo[ch.materialReferenceIndex].colors32;
            var vertices = info.meshInfo[ch.materialReferenceIndex].vertices;
            var v = ch.vertexIndex;
            if (colors == null || vertices == null || v + 3 >= colors.Length)
            {
                continue;
            }

            for (var k = 0; k < 4; k++)
            {
                var color = _style.Letters
                    ? _style.Letter(letter, time)
                    : _style.Gradient((vertices[v + k].x - minX) / width, time);

                // Keep the label's own alpha, so fades still work
                Color32 c = color;
                c.a = colors[v + k].a;
                colors[v + k] = c;
            }

            letter++;
        }

        _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}
