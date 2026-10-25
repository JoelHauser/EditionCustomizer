using EFT.UI;
using TMPro;
using UnityEngine;

namespace NamePlate.Client;

/// <summary>
/// Sits on a nickname label and recolors its letters after TextMeshPro builds them, so it works
/// whoever sets the text. It also puts a clan tag before the name, moves letters (bounce, glitch)
/// and gives the label an outline, glow or shadow and a font style, and undoes all of that when the
/// label is cleared: labels are reused for other people.
/// </summary>
public class NameColorizer : MonoBehaviour
{
    // A clan tag's color: quieter than the name after it
    private static readonly Color TagColor = new(0.66f, 0.70f, 0.73f, 1f);

    private static bool _reportedUnsupported;

    private TMP_Text _text;

    // Null for a style set directly (a bot's), which a reload of icons.json leaves alone
    private EMemberCategory? _category;
    private NameStyle _style;
    private bool _subscribed;

    // When set, the label is only colored while it shows this text. Another mod can write
    // something else into a name label (the deploy screen puts the map's name there); that must
    // not come out in the player's colors. With a tag, this is the tagged text.
    private string _expected;

    // The name as it was given, before any tag: what a reload binds to again
    private string _givenExpected;

    // When set, only these characters (by index in the source text) are colored: a name inside a
    // sentence (a flea buyer in a sale message), or the name after a tag
    private int _rangeStart = -1;
    private int _rangeLength;

    // Characters before the range are a tag of ours, in the tag color
    private bool _colorPrefix;

    // The text this put on the label with a tag, and the name it replaced, to put back
    private string _tagged;
    private string _untagged;

    // Outline/glow/shadow: our own copy of the label's material, and the one it replaced
    private Material _ownMaterial;
    private Material _gameMaterial;
    private FontStyles? _gameFontStyle;
    private bool _allowEffects;

    // Letter positions as TextMeshPro last built them, for moving letters without drift
    private Vector3[][] _base;

    /// <summary>Your name in your look for a member category; null puts the label back to plain.</summary>
    public static void Attach(TMP_Text label, EMemberCategory? category, string expected = null)
    {
        if (label == null)
        {
            return;
        }

        var style = category == null ? null : IconTable.StyleFor(category.Value);
        var colorizer = label.GetComponent<NameColorizer>();
        if (style == null)
        {
            colorizer?.Clear();
            return;
        }

        colorizer ??= label.gameObject.AddComponent<NameColorizer>();
        colorizer.Bind(label, style, category, expected, -1, 0, true);
    }

    /// <summary>
    /// Colors a label with a given style rather than a category's (a PMC bot's look). Null puts
    /// the label back to plain. A range colors only part of the text, with no tag or effects.
    /// </summary>
    public static void AttachStyle(TMP_Text label, NameStyle style, string expected = null, int rangeStart = -1, int rangeLength = 0)
    {
        if (label == null)
        {
            return;
        }

        var colorizer = label.GetComponent<NameColorizer>();
        if (style == null)
        {
            colorizer?.Clear();
            return;
        }

        colorizer ??= label.gameObject.AddComponent<NameColorizer>();
        colorizer.Bind(label, style, null, expected, rangeStart, rangeLength, rangeStart < 0);
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

            var style = IconTable.StyleFor(category);
            if (style == null)
            {
                colorizer.Clear();
            }
            else
            {
                colorizer.Bind(colorizer._text, style, category, colorizer._givenExpected, -1, 0, true);
            }
        }
    }

    private void Bind(TMP_Text label, NameStyle style, EMemberCategory? category, string expected, int rangeStart, int rangeLength, bool effects)
    {
        _text = label;

        // Our old tag is still on the label (a reload, or the same name shown again): take it off first
        if (_tagged != null && label.text == _tagged)
        {
            label.text = _untagged;
        }

        _tagged = _untagged = null;
        _category = category;
        _style = style;
        _givenExpected = expected;
        _expected = expected;
        _rangeStart = rangeStart;
        _rangeLength = rangeLength;
        _colorPrefix = false;
        _allowEffects = effects;

        var text = label.text;
        if (style.Tag != null && rangeStart < 0 && !string.IsNullOrEmpty(text) && (expected == null || text == expected))
        {
            var prefix = "[" + style.Tag + "] ";
            if (!text.StartsWith(prefix))
            {
                _untagged = text;
                _tagged = prefix + text;
                label.text = _tagged;
                _expected = _tagged;
            }
            else
            {
                _expected = text;
            }

            _rangeStart = prefix.Length;
            _rangeLength = _expected.Length - prefix.Length;
            _colorPrefix = true;

            // A tag in the name's colors is colored as the name's first letters
            if (style.TagInName)
            {
                _rangeStart = 0;
                _rangeLength = _expected.Length;
                _colorPrefix = false;
            }
        }

        ApplyEffects(_allowEffects ? style.Effects : null);
        Subscribe();
        _text.ForceMeshUpdate();
    }

    private void Clear()
    {
        // The category goes too: the label may show someone else now, and a reload of
        // icons.json (RefreshAll) would otherwise put that category's colors back on it
        _style = null;
        _category = null;
        _base = null;
        Unsubscribe();
        ApplyEffects(null);
        if (_text != null)
        {
            if (_tagged != null && _text.text == _tagged)
            {
                _text.text = _untagged;
            }

            _text.ForceMeshUpdate();
        }

        _tagged = _untagged = null;
    }

    // ---------------------------------------------------------------- effects

    private void ApplyEffects(NameEffects fx)
    {
        if (_text == null)
        {
            return;
        }

        // Bold, italic, small caps: on top of the label's own style, which is put back after
        if (fx != null && (fx.Bold || fx.Italic || fx.SmallCaps))
        {
            _gameFontStyle ??= _text.fontStyle;
            var style = _gameFontStyle.Value;
            if (fx.Bold)
            {
                style |= FontStyles.Bold;
            }

            if (fx.Italic)
            {
                style |= FontStyles.Italic;
            }

            if (fx.SmallCaps)
            {
                style |= FontStyles.SmallCaps;
            }

            _text.fontStyle = style;
        }
        else if (_gameFontStyle != null)
        {
            _text.fontStyle = _gameFontStyle.Value;
            _gameFontStyle = null;
        }

        if (fx is { NeedsMaterial: true })
        {
            // Our own copy of the label's material: font materials are shared by every label
            if (_ownMaterial == null || _text.fontSharedMaterial != _ownMaterial)
            {
                if (_ownMaterial != null)
                {
                    Destroy(_ownMaterial);
                }

                _gameMaterial = _text.fontSharedMaterial;
                _ownMaterial = new Material(_gameMaterial) { name = _gameMaterial.name + " (Name Plate)" };
                _text.fontSharedMaterial = _ownMaterial;
            }

            Configure(_ownMaterial, _gameMaterial, fx);
            _text.UpdateMeshPadding();
        }
        else if (_ownMaterial != null)
        {
            if (_text.fontSharedMaterial == _ownMaterial)
            {
                _text.fontSharedMaterial = _gameMaterial;
            }

            Destroy(_ownMaterial);
            _ownMaterial = null;
            _gameMaterial = null;
            _text.UpdateMeshPadding();
        }
    }

    // Glow is a soft underlay with no offset, which every TextMeshPro text shader can draw (the
    // glow proper is missing from the mobile one); so glow and shadow share the underlay
    private static void Configure(Material material, Material source, NameEffects fx)
    {
        material.CopyPropertiesFromMaterial(source);
        material.shaderKeywords = source.shaderKeywords;

        var missing = false;
        if (fx.Outline is { } outline)
        {
            if (material.HasProperty(ShaderUtilities.ID_OutlineColor))
            {
                material.EnableKeyword(ShaderUtilities.Keyword_Outline);
                material.SetColor(ShaderUtilities.ID_OutlineColor, outline);
                material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            }
            else
            {
                missing = true;
            }
        }

        if (fx.Glow != null || fx.Shadow)
        {
            if (material.HasProperty(ShaderUtilities.ID_UnderlayColor))
            {
                material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                if (fx.Glow is { } glow)
                {
                    material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(glow.r, glow.g, glow.b, 0.85f));
                    material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
                    material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
                    material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.55f);
                    material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.75f);
                }
                else
                {
                    material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.8f));
                    material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.6f);
                    material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
                    material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.1f);
                    material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.3f);
                }
            }
            else
            {
                missing = true;
            }
        }

        if (missing && !_reportedUnsupported)
        {
            _reportedUnsupported = true;
            Plugin.Log.LogWarning($"Name effects: the shader \"{source.shader.name}\" has no outline or underlay; those effects are skipped");
        }
    }

    // ---------------------------------------------------------------- coloring

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

    private void OnDestroy()
    {
        Unsubscribe();
        if (_ownMaterial != null)
        {
            if (_text != null && _text.fontSharedMaterial == _ownMaterial)
            {
                _text.fontSharedMaterial = _gameMaterial;
            }

            Destroy(_ownMaterial);
        }
    }

    // Fired from inside TextMeshPro's mesh build, which has just written the plain colors and
    // positions back: remember those positions, then recolor
    private void OnTextChanged(Object changed)
    {
        if (changed != _text)
        {
            return;
        }

        CacheBase();
        Recolor();
    }

    private void LateUpdate()
    {
        if (_style is not { Moving: true } || _text == null)
        {
            return;
        }

        // Not while a rebuild is pending: textInfo still describes the old text until TextMeshPro
        // rebuilds later this frame, and a reused row (the flea list) can have just been given
        // another name. The rebuild fires TEXT_CHANGED, which recolors anyway.
        if (_text.havePropertiesChanged)
        {
            return;
        }

        // Nor while scrolled out of view
        if (_text.canvasRenderer != null && _text.canvasRenderer.cull)
        {
            return;
        }

        Recolor();
    }

    private void CacheBase()
    {
        if (_style is not { MovesLetters: true })
        {
            _base = null;
            return;
        }

        var meshes = _text.textInfo?.meshInfo;
        if (meshes == null)
        {
            _base = null;
            return;
        }

        _base = new Vector3[meshes.Length][];
        for (var i = 0; i < meshes.Length; i++)
        {
            _base[i] = meshes[i].vertices == null ? null : (Vector3[])meshes[i].vertices.Clone();
        }
    }

    private void Recolor()
    {
        if (_style == null || _text == null || !_text.isActiveAndEnabled)
        {
            return;
        }

        // Showing something other than the name it was colored for: leave it the plain color
        // TextMeshPro has just built
        if (_expected != null && _text.text != _expected)
        {
            return;
        }

        var info = _text.textInfo;
        if (info == null || info.characterCount == 0 || info.characterInfo == null || info.characterInfo.Length < info.characterCount)
        {
            return;
        }

        var move = _style.MovesLetters && _base != null && _base.Length == info.meshInfo.Length;

        // The name's horizontal extent, for the gradient (from where the letters were built)
        var minX = float.MaxValue;
        var maxX = float.MinValue;
        for (var i = 0; i < info.characterCount; i++)
        {
            var ch = info.characterInfo[i];
            if (ch.isVisible && InRange(ch.index))
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

            var mesh = ch.materialReferenceIndex;
            var colors = info.meshInfo[mesh].colors32;
            var vertices = info.meshInfo[mesh].vertices;
            var v = ch.vertexIndex;
            if (colors == null || vertices == null || v + 3 >= colors.Length)
            {
                continue;
            }

            if (!InRange(ch.index))
            {
                // A tag of ours before the name: its own color, a quiet gray unless set
                if (_colorPrefix && ch.index < _rangeStart)
                {
                    for (var k = 0; k < 4; k++)
                    {
                        Color32 c = _style.TagColor ?? TagColor;
                        c.a = colors[v + k].a;
                        colors[v + k] = c;
                    }
                }

                continue;
            }

            var origin = move && _base[mesh] != null && v + 3 < _base[mesh].Length ? _base[mesh] : null;
            var offset = Vector3.zero;
            if (origin != null)
            {
                _style.Paint.Offset(letter, time, out var dx, out var dy);
                offset = new Vector3((float)dx * ch.pointSize, (float)dy * ch.pointSize, 0f);
            }

            for (var k = 0; k < 4; k++)
            {
                var x = origin != null ? origin[v + k].x : vertices[v + k].x;
                var color = _style.At((x - minX) / width, letter, time);

                // Keep the label's own alpha, so fades still work
                Color32 c = color;
                c.a = colors[v + k].a;
                colors[v + k] = c;

                if (origin != null)
                {
                    vertices[v + k] = origin[v + k] + offset;
                }
            }

            letter++;
        }

        _text.UpdateVertexData(move ? TMP_VertexDataUpdateFlags.Colors32 | TMP_VertexDataUpdateFlags.Vertices : TMP_VertexDataUpdateFlags.Colors32);
    }

    private bool InRange(int index) => _rangeStart < 0 || (index >= _rangeStart && index < _rangeStart + _rangeLength);
}
