"""Draws the icon library in src/ChangeIcons.Client/icons/library.

    python scripts/make_icons.py

Each icon is drawn at 256 px as a tone map (255 = main color, lower = darker shade of it,
0 = empty), then colored, shaded top to bottom, given a dark outline so it reads at the ~20 px
the game shows member icons at, and scaled down to 64 px.
"""
import math
import os
from PIL import Image, ImageDraw, ImageFilter, ImageChops

S = 256
OUT = 64
HERE = os.path.dirname(os.path.abspath(__file__))
DEST = os.path.join(HERE, "..", "src", "ChangeIcons.Client", "icons", "library")

FULL, SHADE, DEEP = 255, 170, 110


def canvas():
    img = Image.new("L", (S, S), 0)
    return img, ImageDraw.Draw(img)


def poly_star(cx, cy, r_out, r_in, points, rot=-90):
    pts = []
    for i in range(points * 2):
        r = r_out if i % 2 == 0 else r_in
        a = math.radians(rot + i * 180 / points)
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


def rotate_pts(pts, cx, cy, deg):
    a = math.radians(deg)
    return [(cx + (x - cx) * math.cos(a) - (y - cy) * math.sin(a), cy + (x - cx) * math.sin(a) + (y - cy) * math.cos(a)) for x, y in pts]


def rect_pts(x0, y0, x1, y1):
    return [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]


# ---------------------------------------------------------------- shapes


def skull():
    img, d = canvas()
    d.ellipse((40, 26, 216, 190), fill=FULL)
    d.rounded_rectangle((78, 150, 178, 226), 18, fill=FULL)
    d.ellipse((68, 92, 120, 146), fill=0)
    d.ellipse((136, 92, 188, 146), fill=0)
    d.polygon([(128, 146), (114, 174), (142, 174)], fill=0)
    for x in (104, 128, 152):
        d.rectangle((x - 5, 196, x + 5, 226), fill=0)
    return img


def crosshair():
    img, d = canvas()
    d.ellipse((34, 34, 222, 222), fill=FULL)
    d.ellipse((62, 62, 194, 194), fill=0)
    for box in ((116, 14, 140, 98), (116, 158, 140, 242), (14, 116, 98, 140), (158, 116, 242, 140)):
        d.rectangle(box, fill=FULL)
    d.ellipse((112, 112, 144, 144), fill=FULL)
    return img


def star():
    img, d = canvas()
    d.polygon(poly_star(128, 138, 118, 50, 5), fill=FULL)
    # Right half a shade darker, for a folded look
    right = Image.new("L", (S, S), 0)
    ImageDraw.Draw(right).polygon(poly_star(128, 138, 118, 50, 5), fill=255)
    ImageDraw.Draw(right).rectangle((0, 0, 128, S), fill=0)
    img.paste(SHADE, mask=right)
    return img


def crown():
    img, d = canvas()
    d.polygon([(28, 190), (40, 70), (88, 130), (128, 46), (168, 130), (216, 70), (228, 190)], fill=FULL)
    d.rectangle((28, 196, 228, 222), fill=SHADE)
    for x, y in ((40, 64), (128, 40), (216, 64)):
        d.ellipse((x - 16, y - 16, x + 16, y + 16), fill=FULL)
    d.ellipse((114, 150, 142, 178), fill=DEEP)
    return img


def shield():
    img, d = canvas()
    d.polygon([(128, 18), (224, 52), (216, 140), (128, 240), (40, 140), (32, 52)], fill=FULL)
    d.polygon([(128, 18), (224, 52), (216, 140), (128, 240)], fill=SHADE)
    d.polygon([(76, 110), (128, 150), (180, 110), (180, 140), (128, 182), (76, 140)], fill=0)
    return img


def flame():
    img, d = canvas()
    # Outer flame: a wide base rising to one main tongue with a smaller one either side
    outer = [(128, 8), (150, 58), (176, 90), (184, 58), (212, 110), (226, 160), (214, 204), (180, 236),
             (128, 248), (76, 236), (42, 204), (30, 160), (44, 110), (70, 72), (80, 104), (100, 74)]
    d.polygon(outer, fill=FULL)
    inner = [(128, 96), (146, 136), (164, 124), (174, 170), (162, 210), (128, 228), (94, 210), (82, 172), (100, 140), (112, 150)]
    d.polygon(inner, fill=SHADE)
    return img


def bolt():
    img, d = canvas()
    d.polygon([(150, 12), (52, 146), (118, 146), (96, 244), (204, 104), (138, 104), (172, 12)], fill=FULL)
    d.polygon([(150, 12), (172, 12), (138, 104), (204, 104), (96, 244), (118, 146)], fill=SHADE)
    return img


def heart():
    img, d = canvas()
    d.ellipse((24, 40, 136, 150), fill=FULL)
    d.ellipse((120, 40, 232, 150), fill=FULL)
    d.polygon([(32, 118), (224, 118), (128, 232)], fill=FULL)
    d.ellipse((56, 64, 92, 100), fill=SHADE)
    return img


def gem():
    img, d = canvas()
    top = [(64, 40), (192, 40), (236, 96), (20, 96)]
    d.polygon(top, fill=FULL)
    d.polygon([(20, 96), (236, 96), (128, 232)], fill=SHADE)
    d.polygon([(64, 40), (100, 96), (128, 40)], fill=SHADE)
    d.polygon([(192, 40), (156, 96), (128, 40)], fill=SHADE)
    d.polygon([(100, 96), (156, 96), (128, 232)], fill=FULL)
    return img


def moon():
    img, d = canvas()
    d.ellipse((24, 24, 232, 232), fill=FULL)
    d.ellipse((84, 4, 262, 190), fill=0)
    return img


def sun():
    img, d = canvas()
    for i in range(12):
        a = i * 30
        pts = rotate_pts([(128, 6), (144, 56), (112, 56)], 128, 128, a)
        d.polygon(pts, fill=SHADE if i % 2 else FULL)
    d.ellipse((58, 58, 198, 198), fill=FULL)
    return img


def snowflake():
    img, d = canvas()
    for a in range(0, 180, 60):
        arm = rotate_pts(rect_pts(118, 14, 138, 242), 128, 128, a)
        d.polygon(arm, fill=FULL)
        for end in (1, -1):
            for dist in (62, 92):
                for side in (1, -1):
                    cx, cy = 128, 128 - end * dist
                    branch = rotate_pts(rect_pts(cx - 8, cy - 34, cx + 8, cy), cx, cy, side * 45 + (0 if end == 1 else 180))
                    d.polygon(rotate_pts(branch, 128, 128, a), fill=FULL)
    d.ellipse((104, 104, 152, 152), fill=SHADE)
    return img


def radiation():
    img, d = canvas()
    d.ellipse((16, 16, 240, 240), fill=SHADE)
    d.ellipse((32, 32, 224, 224), fill=0)
    for a in (-90, 30, 150):
        d.pieslice((40, 40, 216, 216), a - 30, a + 30, fill=FULL)
    d.ellipse((92, 92, 164, 164), fill=0)
    d.ellipse((106, 106, 150, 150), fill=FULL)
    return img


def dogtag():
    img, d = canvas()
    d.rounded_rectangle((52, 30, 204, 230), 42, fill=FULL)
    d.ellipse((110, 46, 146, 82), fill=0)
    for y in (116, 146, 176):
        d.rounded_rectangle((82, y, 174, y + 14), 6, fill=DEEP)
    return img


def bullet():
    img, d = canvas()
    d.rectangle((86, 110, 170, 226), fill=FULL)
    d.rectangle((78, 214, 178, 238), fill=SHADE)
    d.rectangle((86, 110, 104, 226), fill=SHADE)
    d.pieslice((86, 20, 170, 200), 180, 360, fill=SHADE)
    d.polygon([(86, 110), (170, 110), (128, 18)], fill=SHADE)
    d.rectangle((86, 104, 170, 116), fill=DEEP)
    return img


def key():
    img, d = canvas()
    d.ellipse((20, 20, 124, 124), fill=FULL)
    d.ellipse((48, 48, 96, 96), fill=0)
    shaft = rotate_pts(rect_pts(100, 62, 250, 84), 72, 72, 45)
    d.polygon(shaft, fill=FULL)
    for t in (150, 190):
        tooth = rotate_pts(rect_pts(t, 84, t + 18, 120), 72, 72, 45)
        d.polygon(tooth, fill=SHADE)
    return img


def hexagon():
    img, d = canvas()
    hexa = [(128 + 116 * math.cos(math.radians(a)), 128 + 116 * math.sin(math.radians(a))) for a in range(-90, 270, 60)]
    inner = [(128 + 64 * math.cos(math.radians(a)), 128 + 64 * math.sin(math.radians(a))) for a in range(-90, 270, 60)]
    d.polygon(hexa, fill=FULL)
    d.polygon(hexa[1:4] + [(128, 128)], fill=SHADE)
    d.polygon(inner, fill=0)
    return img


def warning():
    img, d = canvas()
    d.polygon([(128, 16), (244, 226), (12, 226)], fill=FULL)
    d.rounded_rectangle((114, 80, 142, 168), 10, fill=DEEP)
    d.ellipse((112, 180, 144, 212), fill=DEEP)
    return img


def chevrons():
    img, d = canvas()
    for i, y in enumerate((30, 100, 170)):
        d.polygon([(28, y + 20), (128, y - 30 + 20), (228, y + 20), (228, y + 64), (128, y + 14 + 20), (28, y + 64)], fill=FULL if i != 1 else SHADE)
    return img


def swords():
    img, d = canvas()
    for a, tone in ((45, FULL), (-45, SHADE)):
        blade = rotate_pts([(118, 20), (128, 4), (138, 20), (138, 176), (118, 176)], 128, 128, a)
        d.polygon(blade, fill=tone)
        guard = rotate_pts(rect_pts(94, 176, 162, 192), 128, 128, a)
        d.polygon(guard, fill=DEEP)
        grip = rotate_pts(rect_pts(120, 192, 136, 244), 128, 128, a)
        d.polygon(grip, fill=tone)
    return img


def paw():
    img, d = canvas()
    d.ellipse((64, 118, 192, 232), fill=FULL)
    for x0, y0 in ((22, 84), (70, 30), (134, 30), (182, 84)):
        d.ellipse((x0, y0, x0 + 54, y0 + 70), fill=FULL)
    d.ellipse((96, 160, 160, 214), fill=SHADE)
    return img


def eye():
    img, d = canvas()
    top = [(12 + i * 232 / 40, 128 - 92 * math.sin(math.pi * i / 40)) for i in range(41)]
    bottom = [(244 - i * 232 / 40, 128 + 92 * math.sin(math.pi * i / 40)) for i in range(41)]
    d.polygon(top + bottom, fill=FULL)
    d.ellipse((76, 76, 180, 180), fill=SHADE)
    d.ellipse((106, 106, 150, 150), fill=0)
    return img


def trophy():
    img, d = canvas()
    d.pieslice((56, -40, 200, 150), 0, 180, fill=FULL)
    d.rectangle((56, 18, 200, 56), fill=FULL)
    for x in (18, 190):
        d.ellipse((x, 36, x + 48, 104), fill=SHADE)
        d.ellipse((x + 12, 50, x + 36, 90), fill=0)
    d.rectangle((116, 146, 140, 196), fill=SHADE)
    d.rounded_rectangle((72, 196, 184, 232), 8, fill=FULL)
    return img


def spade():
    img, d = canvas()
    d.ellipse((24, 96, 136, 200), fill=FULL)
    d.ellipse((120, 96, 232, 200), fill=FULL)
    d.polygon([(32, 136), (224, 136), (128, 18)], fill=FULL)
    d.polygon([(128, 150), (170, 240), (86, 240)], fill=SHADE)
    return img


def target():
    img, d = canvas()
    for i, r in enumerate((116, 88, 60, 32)):
        d.ellipse((128 - r, 128 - r, 128 + r, 128 + r), fill=FULL if i % 2 == 0 else 0)
    return img


def cross():
    img, d = canvas()
    d.rounded_rectangle((92, 20, 164, 236), 14, fill=FULL)
    d.rounded_rectangle((20, 92, 236, 164), 14, fill=FULL)
    return img


def cube():
    img, d = canvas()
    top = [(128, 16), (232, 72), (128, 128), (24, 72)]
    d.polygon(top, fill=FULL)
    d.polygon([(24, 72), (128, 128), (128, 242), (24, 186)], fill=SHADE)
    d.polygon([(232, 72), (128, 128), (128, 242), (232, 186)], fill=DEEP)
    return img


def clover():
    img, d = canvas()
    for cx, cy in ((128, 70), (70, 128), (186, 128), (128, 186)):
        d.ellipse((cx - 56, cy - 56, cx + 56, cy + 56), fill=FULL)
    d.ellipse((104, 104, 152, 152), fill=SHADE)
    return img


def anchor():
    img, d = canvas()
    d.ellipse((96, 12, 160, 76), fill=FULL)
    d.ellipse((114, 30, 142, 58), fill=0)
    d.rectangle((116, 70, 140, 222), fill=FULL)
    d.rectangle((70, 90, 186, 110), fill=FULL)
    d.arc((30, 70, 226, 236), 20, 160, fill=FULL, width=24)
    d.polygon([(22, 150), (62, 144), (40, 184)], fill=FULL)
    d.polygon([(234, 150), (194, 144), (216, 184)], fill=FULL)
    return img


def wings():
    img, d = canvas()
    for side in (1, -1):
        # Three feathers per wing, longest on top, sweeping out and up from the middle
        for i, (length, y, tone) in enumerate(((112, 84, FULL), (92, 124, SHADE), (70, 162, FULL))):
            x0 = 128 + side * 14
            pts = [(x0, y + 26), (x0, y), (x0 + side * length, y - 40 + i * 10), (x0 + side * (length - 26), y + 8)]
            d.polygon(pts, fill=tone)
    d.polygon([(128, 60), (146, 110), (128, 210), (110, 110)], fill=DEEP)
    return img


def infinity():
    img, d = canvas()
    for cx in (80, 176):
        d.ellipse((cx - 64, 64, cx + 64, 192), fill=FULL)
        d.ellipse((cx - 34, 94, cx + 34, 162), fill=0)
    d.polygon([(118, 104), (138, 104), (140, 152), (116, 152)], fill=SHADE)
    return img


ICONS = [
    ("skull", skull, "#E8E4D8"),
    ("crosshair", crosshair, "#FF4A3D"),
    ("star", star, "#FFC93C"),
    ("crown", crown, "#B06CFF"),
    ("shield", shield, "#4C8DFF"),
    ("flame", flame, "#FF7A1A"),
    ("bolt", bolt, "#FFE04A"),
    ("heart", heart, "#FF3D6E"),
    ("gem", gem, "#3DE0FF"),
    ("moon", moon, "#C9D4FF"),
    ("sun", sun, "#FFB21E"),
    ("snowflake", snowflake, "#9FE8FF"),
    ("radiation", radiation, "#D8FF3A"),
    ("dogtag", dogtag, "#B9C0C6"),
    ("bullet", bullet, "#D9A441"),
    ("key", key, "#E0C060"),
    ("hexagon", hexagon, "#35E0A0"),
    ("warning", warning, "#FFB000"),
    ("chevrons", chevrons, "#C5B48A"),
    ("swords", swords, "#D0D6DC"),
    ("paw", paw, "#C08050"),
    ("eye", eye, "#7CFFB2"),
    ("trophy", trophy, "#F2C744"),
    ("spade", spade, "#8A94A6"),
    ("target", target, "#FF5A36"),
    ("medic", cross, "#E23B3B"),
    ("cube", cube, "#5AA8FF"),
    ("clover", clover, "#4FD65A"),
    ("anchor", anchor, "#5A7FA8"),
    ("wings", wings, "#F0F0F0"),
    ("infinity", infinity, "#FF6BD5"),
]


def hex_rgb(text):
    text = text.lstrip("#")
    return tuple(int(text[i:i + 2], 16) for i in (0, 2, 4))


def render(tones, color):
    rgb = hex_rgb(color)
    alpha = tones.point(lambda v: 255 if v > 0 else 0)

    # Color by tone, lighter at the top and darker at the bottom
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    px = img.load()
    tp = tones.load()
    for y in range(S):
        light = 1.18 - 0.36 * y / S
        for x in range(S):
            v = tp[x, y]
            if v:
                k = v / 255 * light
                px[x, y] = tuple(min(255, int(c * k)) for c in rgb) + (255,)

    # Dark outline around the whole shape, holes included
    outline = alpha.filter(ImageFilter.MaxFilter(21))
    ring = ImageChops.subtract(outline, alpha)
    base = Image.new("RGBA", (S, S), (10, 12, 13, 0))
    base.putalpha(ring)
    base.alpha_composite(img)

    # Pad so the outline isn't clipped, then scale down
    padded = Image.new("RGBA", (S + 24, S + 24), (0, 0, 0, 0))
    padded.alpha_composite(base, (12, 12))
    return padded.resize((OUT, OUT), Image.LANCZOS)


def main():
    os.makedirs(DEST, exist_ok=True)
    rendered = []
    for name, draw, color in ICONS:
        icon = render(draw(), color)
        icon.save(os.path.join(DEST, f"{name}.png"))
        rendered.append((name, icon))

    # Contact sheet, also at in-game size, for checking by eye
    cols = 8
    rows = math.ceil(len(rendered) / cols)
    sheet = Image.new("RGBA", (cols * 104 + 8, rows * 104 + 8), (18, 20, 21, 255))
    for i, (name, icon) in enumerate(rendered):
        x, y = 8 + (i % cols) * 104, 8 + (i // cols) * 104
        sheet.alpha_composite(icon.resize((72, 72), Image.LANCZOS), (x, y))
        sheet.alpha_composite(icon.resize((20, 20), Image.LANCZOS), (x + 76, y + 50))
    sheet.save(os.path.join(HERE, "icons-sheet.png"))
    print(f"{len(rendered)} icons -> {os.path.normpath(DEST)}")


if __name__ == "__main__":
    main()
