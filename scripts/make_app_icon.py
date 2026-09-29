"""Draws the editor's app icon: src/ChangeIcons.Editor/Assets/app.ico

    python scripts/make_app_icon.py

Gold rank chevrons (the library's "chevrons", also the editor's header logo) on a dark rounded
tile, with a thin gradient line under them for the name colors. Every size is drawn from 1024 px
and scaled down, so the small ones stay clean.
"""
import os
from PIL import Image, ImageDraw

from make_icons import chevrons, render

HERE = os.path.dirname(os.path.abspath(__file__))
DEST = os.path.join(HERE, "..", "src", "ChangeIcons.Editor", "Assets", "app.ico")
BIG = 1024
SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]


def tile():
    img = Image.new("RGBA", (BIG, BIG), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Dark tile with a soft top-to-bottom shade and a gold rim
    for y in range(BIG):
        shade = int(34 - 16 * y / BIG)
        d.line([(0, y), (BIG, y)], fill=(shade, shade + 3, shade + 5, 255))
    mask = Image.new("L", (BIG, BIG), 0)
    ImageDraw.Draw(mask).rounded_rectangle((24, 24, BIG - 24, BIG - 24), 200, fill=255)
    out = Image.new("RGBA", (BIG, BIG), (0, 0, 0, 0))
    out.paste(img, mask=mask)
    ImageDraw.Draw(out).rounded_rectangle((24, 24, BIG - 24, BIG - 24), 200, outline=(201, 183, 127, 255), width=22)

    # The chevrons, in the header's gold
    mark = render(chevrons(), "#C9B77F").resize((600, 600), Image.LANCZOS)
    out.alpha_composite(mark, ((BIG - 600) // 2, 150))

    # A gradient line: the colors a name can take
    stops = [(255, 77, 77), (255, 159, 28), (255, 225, 77), (91, 227, 107), (61, 185, 255), (139, 92, 255)]
    x0, x1, y0, y1 = 250, BIG - 250, 790, 836
    line = Image.new("RGBA", (x1 - x0, y1 - y0))
    lp = line.load()
    for x in range(x1 - x0):
        t = x / (x1 - x0 - 1) * (len(stops) - 1)
        i = min(int(t), len(stops) - 2)
        f = t - i
        c = tuple(int(stops[i][k] + (stops[i + 1][k] - stops[i][k]) * f) for k in range(3))
        for y in range(y1 - y0):
            lp[x, y] = c + (255,)
    lmask = Image.new("L", line.size, 0)
    ImageDraw.Draw(lmask).rounded_rectangle((0, 0, line.width - 1, line.height - 1), 23, fill=255)
    out.paste(line, (x0, y0), lmask)
    return out


def main():
    big = tile()
    images = [big.resize((s, s), Image.LANCZOS) for s in SIZES]
    images[-1].save(DEST, format="ICO", sizes=[(s, s) for s in SIZES], append_images=images[:-1])
    big.resize((256, 256), Image.LANCZOS).save(os.path.join(HERE, "app-icon.png"))

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
