using System;
using UnityEngine;
using UnityEngine.UI;

namespace NamePlate.Client;

/// <summary>
/// Sits on an icon image: steps through an animated icon's frames, and runs an icon effect (pulse,
/// glow, flip, float). Icons are reused for other people, so attaching nothing, or the game putting
/// another picture in, puts the image back exactly as it was.
/// </summary>
public class IconAnimator : MonoBehaviour
{
    private const string HaloName = "NamePlate_Halo";
    private static Sprite _haloSprite;

    private Image _image;
    private IconTable.IconMotion _motion;
    private Image _halo;

    // The image's own scale, to grow from and to put back. Its position belongs to whatever lays it
    // out (a row centers icon and name): only "float" moves it, by an offset on top of that
    private bool _captured;
    private Vector3 _baseScale;
    private Vector2 _floatOffset;
    private Vector2? _floatWritten;

    /// <summary>Animates an icon the way a category's icons.json entry says; null stops it.</summary>
    public static void Attach(Image image, IconTable.IconMotion motion)
    {
        if (image == null)
        {
            return;
        }

        var animator = image.GetComponent<IconAnimator>();
        if (motion == null || (motion.Frames == null && motion.Effect == null))
        {
            if (animator != null)
            {
                animator.Stop();
            }

            return;
        }

        animator ??= image.gameObject.AddComponent<IconAnimator>();
        animator.Begin(image, motion);
    }

    /// <summary>For a picture alone (a bot's icon): plays it if it's an animated strip, else stops.</summary>
    public static void AttachPicture(Image image)
    {
        var frames = image != null ? IconTable.FramesOf(image.sprite) : null;
        Attach(image, frames == null ? null : new IconTable.IconMotion { Frames = frames });
    }

    private void Begin(Image image, IconTable.IconMotion motion)
    {
        Restore();
        _image = image;
        var rect = (RectTransform)transform;
        _baseScale = rect.localScale;
        _floatOffset = Vector2.zero;
        _floatWritten = null;
        _captured = true;
        _motion = motion;
        enabled = true;

        if (motion.Effect == "glow")
        {
            ShowHalo(motion.Tint);
        }
        else
        {
            HideHalo();
        }
    }

    private void Stop()
    {
        Restore();
        HideHalo();
        _motion = null;
        enabled = false;
    }

    private void Restore()
    {
        if (!_captured)
        {
            return;
        }

        var rect = (RectTransform)transform;
        rect.localScale = _baseScale;
        SetFloat(rect, Vector2.zero);
        _captured = false;
    }

    private void LateUpdate()
    {
        if (_motion == null || _image == null)
        {
            return;
        }

        var time = Time.unscaledTime;
        if (_motion.Frames != null)
        {
            // The game has put another picture in: this one is done
            if (Array.IndexOf(_motion.Frames, _image.sprite) < 0)
            {
                Stop();
                return;
            }

            var frame = _motion.Frames[(int)(time * _motion.Fps) % _motion.Frames.Length];
            if (_image.sprite != frame)
            {
                _image.sprite = frame;
            }
        }

        var rect = (RectTransform)transform;
        switch (_motion.Effect)
        {
            case "pulse":
                var grow = 1f + 0.12f * Mathf.Sin(time * 2f * Mathf.PI * 1.1f);
                ScaleAboutCenter(rect, grow, grow);
                break;
            // An animated icon already moves: turning it over too (a coin spinning two ways) looks broken
            case "flip" when _motion.Frames == null:
                ScaleAboutCenter(rect, Mathf.Cos(time * Mathf.PI * 0.9f), 1f);
                break;
            case "float":
                SetFloat(rect, new Vector2(0f, rect.rect.height * 0.12f * Mathf.Sin(time * 2f * Mathf.PI * 0.7f)));
                break;
            case "glow":
                if (_halo != null)
                {
                    var c = _motion.Tint;
                    _halo.color = new Color(c.r, c.g, c.b, 0.35f + 0.25f * (0.5f + 0.5f * Mathf.Sin(time * 2f * Mathf.PI * 0.8f)));
                    var halo = (RectTransform)_halo.transform;
                    halo.position = rect.TransformPoint(rect.rect.center);
                    halo.sizeDelta = rect.rect.size * 2f;
                }

                break;
        }
    }

    // Scales the icon about its middle. Scale works about the pivot, and some icons (the main menu's)
    // pivot on their left edge, so a flip would swing the picture across the name beside it: the
    // pivot's drift is taken back with an offset on top of the layout's position.
    private void ScaleAboutCenter(RectTransform rect, float x, float y)
    {
        rect.localScale = new Vector3(_baseScale.x * x, _baseScale.y * y, _baseScale.z);
        var size = rect.rect.size;
        var middle = new Vector2((0.5f - rect.pivot.x) * size.x, (0.5f - rect.pivot.y) * size.y);
        SetFloat(rect, new Vector2(_baseScale.x * (1f - x) * middle.x, _baseScale.y * (1f - y) * middle.y));
    }

    // Moves the icon by an offset from where its layout put it. If the layout has placed it again
    // since (the position isn't the one written here), that new place is the base.
    private void SetFloat(RectTransform rect, Vector2 offset)
    {
        var position = rect.anchoredPosition;
        var basePosition = _floatWritten is { } written && (position - written).sqrMagnitude < 0.0001f ? position - _floatOffset : position;
        if (offset == Vector2.zero && _floatOffset == Vector2.zero)
        {
            return;
        }

        rect.anchoredPosition = basePosition + offset;
        _floatOffset = offset;
        _floatWritten = offset == Vector2.zero ? null : basePosition + offset;
    }

    // A soft disc behind the icon: a sibling drawn before it, kept out of any layout
    private void ShowHalo(Color tint)
    {
        if (_halo == null)
        {
            var parent = transform.parent;
            if (parent == null)
            {
                return;
            }

            var existing = parent.Find(HaloName);
            var go = existing != null ? existing.gameObject : new GameObject(HaloName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            _halo = go.GetComponent<Image>();
            _halo.raycastTarget = false;
            _halo.sprite = HaloSprite();
        }

        // Drawn first, so behind the icon
        if (_halo.transform.GetSiblingIndex() > transform.GetSiblingIndex())
        {
            _halo.transform.SetSiblingIndex(transform.GetSiblingIndex());
        }
        _halo.color = new Color(tint.r, tint.g, tint.b, 0.5f);
        _halo.gameObject.SetActive(true);
    }

    private void HideHalo()
    {
        if (_halo != null)
        {
            _halo.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (_motion != null)
        {
            Restore();
            HideHalo();
        }
    }

    private void OnEnable()
    {
        if (_motion != null && _image != null)
        {
            Begin(_image, _motion);
        }
    }

    private static Sprite HaloSprite()
    {
        if (_haloSprite != null)
        {
            return _haloSprite;
        }

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "NamePlate halo", filterMode = FilterMode.Bilinear };
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = (x + 0.5f) / size * 2f - 1f;
                var dy = (y + 0.5f) / size * 2f - 1f;
                var falloff = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, falloff * falloff));
            }
        }

        texture.Apply();
        texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
        _haloSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        _haloSprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return _haloSprite;
    }
}
