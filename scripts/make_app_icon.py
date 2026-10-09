"""Draws the editor's app icon: src/NamePlate.Editor/Assets/app.ico (and the header's logo.png)

    python scripts/make_app_icon.py

The library's cat, drawn the same way as the library icons (tone map, color, dark outline) but
kept at full size instead of shrunk to 64 px, then scaled down for each icon size.
"""
import os
from PIL import Image, ImageChops, ImageFilter

from make_icons import S, cat, hex_rgb

HERE = os.path.dirname(os.path.abspath(__file__))
DEST = os.path.join(HERE, "..", "src", "NamePlate.Editor", "Assets", "app.ico")
SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]
COLOR = "#F0A04B"  # the library cat's color


UP = 4  # drawn at 4x and scaled down, so the big sizes have smooth edges


def smooth_up(mask_or_tones, levels):
    """Scales a hard-edged tone map up UP times with soft curves, then snaps each pixel back to
    the nearest of its tones, so the shapes come out as smooth outlines rather than stair steps."""
    big = mask_or_tones.resize((S * UP, S * UP), Image.BICUBIC)
    table = [min(levels, key=lambda t: abs(t - v)) for v in range(256)]
    return big.point(table)


def render_full(tones, color):
    """make_icons.render (tone map, color, dark outline) at UP times the size, without its shrink to 64 px."""
    rgb = hex_rgb(color)
    levels = [tone for tone, count in enumerate(tones.histogram()) if count]
    alpha = tones.point(lambda v: 255 if v > 0 else 0)
    outline = smooth_up(alpha.filter(ImageFilter.MaxFilter(21)), [0, 255])
    tones = smooth_up(tones, levels)
    size = S * UP
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px = img.load()
    tp = tones.load()
    for y in range(size):
        light = 1.18 - 0.36 * y / size
        for x in range(size):
            v = tp[x, y]
            if v:
                k = v / 255 * light
                px[x, y] = tuple(min(255, int(c * k)) for c in rgb) + (255,)
    ring = ImageChops.subtract(outline, tones.point(lambda v: 255 if v > 0 else 0))
    base = Image.new("RGBA", (size, size), (10, 12, 13, 0))
    base.putalpha(ring)
    base.alpha_composite(img)
    padded = Image.new("RGBA", (size + 24 * UP, size + 24 * UP), (0, 0, 0, 0))
    padded.alpha_composite(base, (12 * UP, 12 * UP))
    return padded


def main():
    big = render_full(cat(), COLOR)
    images = [big.resize((s, s), Image.LANCZOS) for s in SIZES]
    images[-1].save(DEST, format="ICO", sizes=[(s, s) for s in SIZES], append_images=images[:-1])
    images[-1].save(os.path.join(HERE, "app-icon.png"))
    # The editor's header logo
    big.resize((64, 64), Image.LANCZOS).save(os.path.join(os.path.dirname(DEST), "logo.png"))

    # Sheet of every size, for checking by eye
    sheet = Image.new("RGBA", (sum(SIZES) + 12 * len(SIZES) + 12, 256 + 24), (40, 40, 40, 255))
    x = 12
    for s, im in zip(SIZES, images):
        sheet.alpha_composite(im, (x, 12))
        x += s + 12
    sheet.save(os.path.join(HERE, "app-icon-sizes.png"))
    print("->", os.path.normpath(DEST))


if __name__ == "__main__":
    main()
