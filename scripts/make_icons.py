"""Draws the icon library in src/NamePlate.Client/icons/library.

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
DEST = os.path.join(HERE, "..", "src", "NamePlate.Client", "icons", "library")

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


def grenade():
    # Frag grenade: ribbed body, fuse, spoon and pin ring
    img, d = canvas()
    d.ellipse((52, 76, 204, 240), fill=FULL)
    for x in (102, 154):
        d.line((x, 84, x, 232), fill=SHADE, width=10)
    for y in (130, 182):
        d.line((56, y, 200, y), fill=SHADE, width=10)
    d.rectangle((100, 44, 156, 84), fill=SHADE)
    d.polygon([(150, 46), (206, 30), (214, 44), (162, 74)], fill=DEEP)
    d.ellipse((58, 18, 110, 70), outline=SHADE, width=12)
    return img


def backpack():
    img, d = canvas()
    d.arc((92, 8, 164, 72), 180, 360, fill=SHADE, width=14)
    d.rounded_rectangle((48, 40, 208, 240), 40, fill=FULL)
    d.rounded_rectangle((48, 40, 208, 116), 40, fill=SHADE)
    d.rectangle((48, 92, 208, 116), fill=SHADE)
    d.rounded_rectangle((80, 146, 176, 222), 16, fill=SHADE)
    d.rectangle((116, 104, 140, 132), fill=DEEP)
    return img


def knife():
    # A combat knife, drawn level and turned to point up and right
    img, d = canvas()
    d.polygon([(100, 108), (214, 108), (246, 126), (214, 150), (100, 150)], fill=FULL)
    d.polygon([(100, 108), (214, 108), (246, 126), (100, 126)], fill=SHADE)
    d.rounded_rectangle((86, 88, 102, 170), 4, fill=DEEP)
    d.rounded_rectangle((12, 110, 88, 148), 12, fill=SHADE)
    for x in (32, 52, 72):
        d.line((x, 112, x, 146), fill=DEEP, width=5)
    return img.rotate(40, resample=Image.BICUBIC, center=(128, 128))


def bitcoin():
    # A physical bitcoin: rim, face and the B with its strokes
    img, d = canvas()
    d.ellipse((12, 12, 244, 244), fill=FULL)
    d.ellipse((34, 34, 222, 222), fill=SHADE)
    b = Image.new("L", (S, S), 0)
    bd = ImageDraw.Draw(b)
    bd.rectangle((86, 70, 120, 186), fill=255)
    bd.ellipse((92, 70, 166, 132), fill=255)
    bd.ellipse((92, 122, 176, 186), fill=255)
    bd.ellipse((116, 90, 144, 112), fill=0)
    bd.ellipse((116, 142, 152, 166), fill=0)
    for x in (100, 128):
        bd.rectangle((x, 50, x + 12, 72), fill=255)
        bd.rectangle((x, 184, x + 12, 206), fill=255)
    img.paste(FULL, mask=b)
    return img


def padlock():
    img, d = canvas()
    d.arc((66, 18, 190, 150), 180, 360, fill=SHADE, width=26)
    d.rectangle((66, 82, 92, 120), fill=SHADE)
    d.rectangle((164, 82, 190, 120), fill=SHADE)
    d.rounded_rectangle((40, 108, 216, 240), 22, fill=FULL)
    d.ellipse((110, 142, 146, 178), fill=0)
    d.rectangle((120, 168, 136, 210), fill=0)
    return img


def compass():
    img, d = canvas()
    d.ellipse((12, 12, 244, 244), fill=FULL)
    d.ellipse((36, 36, 220, 220), fill=60)
    d.polygon([(128, 46), (152, 128), (104, 128)], fill=FULL)
    d.polygon([(128, 210), (152, 128), (104, 128)], fill=SHADE)
    d.ellipse((118, 118, 138, 138), fill=DEEP)
    return img


def flag():
    img, d = canvas()
    d.rounded_rectangle((36, 16, 58, 244), 8, fill=SHADE)
    top = [(58 + i * 4.5, 34 + 14 * math.sin(i / 32 * 2 * math.pi)) for i in range(33)]
    bottom = [(58 + (32 - i) * 4.5, 140 + 14 * math.sin((32 - i) / 32 * 2 * math.pi)) for i in range(33)]
    d.polygon(top + bottom, fill=FULL)
    return img


def biohazard():
    # Three hooked rings round a centre circle
    img, d = canvas()
    for a in (-90, 30, 150):
        r = math.radians(a)
        cx, cy = 128 + 58 * math.cos(r), 132 + 58 * math.sin(r)
        d.ellipse((cx - 58, cy - 58, cx + 58, cy + 58), fill=FULL)
    for a in (-90, 30, 150):
        r = math.radians(a)
        cx, cy = 128 + 80 * math.cos(r), 132 + 80 * math.sin(r)
        d.ellipse((cx - 40, cy - 40, cx + 40, cy + 40), fill=0)
    d.ellipse((128 - 74, 132 - 74, 128 + 74, 132 + 74), outline=SHADE, width=14)
    d.ellipse((128 - 22, 132 - 22, 128 + 22, 132 + 22), fill=0)
    return img


def ghost():
    img, d = canvas()
    d.ellipse((44, 18, 212, 186), fill=FULL)
    d.rectangle((44, 102, 212, 214), fill=FULL)
    for x in (72, 128, 184):
        d.ellipse((x - 28, 196, x + 28, 252), fill=FULL)
    for x in (100, 156):
        d.ellipse((x - 28, 214, x + 28, 270), fill=0)
    d.ellipse((84, 80, 116, 124), fill=DEEP)
    d.ellipse((140, 80, 172, 124), fill=DEEP)
    return img


def bomb():
    img, d = canvas()
    d.ellipse((22, 70, 194, 242), fill=FULL)
    d.ellipse((50, 100, 92, 142), fill=SHADE)
    cap = rotate_pts(rect_pts(132, 54, 176, 92), 154, 73, 40)
    d.polygon(cap, fill=SHADE)
    d.line((170, 60, 196, 36), fill=SHADE, width=10)
    d.polygon(poly_star(210, 28, 26, 9, 4, rot=-45), fill=FULL)
    return img


def capsule():
    # A painkiller capsule, two-tone, on a slant
    img, d = canvas()
    d.rounded_rectangle((20, 92, 236, 164), 36, fill=FULL)
    d.rounded_rectangle((128, 92, 236, 164), 36, fill=SHADE)
    d.rectangle((128, 92, 160, 164), fill=SHADE)
    d.rounded_rectangle((44, 106, 110, 118), 6, fill=255)
    return img.rotate(35, resample=Image.BICUBIC, center=(128, 128))


def syringe():
    # A stim injector: barrel with marks, plunger, needle; on a slant
    img, d = canvas()
    d.rectangle((70, 100, 186, 156), fill=FULL)
    for x in (100, 126, 152):
        d.line((x, 100, x, 118), fill=DEEP, width=6)
    d.rectangle((186, 114, 222, 142), fill=SHADE)
    d.rounded_rectangle((218, 92, 240, 164), 6, fill=SHADE)
    d.rectangle((52, 112, 70, 144), fill=SHADE)
    d.line((14, 128, 52, 128), fill=SHADE, width=7)
    return img.rotate(40, resample=Image.BICUBIC, center=(128, 128))


def hourglass():
    img, d = canvas()
    d.rounded_rectangle((40, 14, 216, 40), 8, fill=SHADE)
    d.rounded_rectangle((40, 216, 216, 242), 8, fill=SHADE)
    d.polygon([(60, 40), (196, 40), (140, 128), (196, 216), (60, 216), (116, 128)], fill=FULL)
    d.polygon([(84, 216), (172, 216), (128, 176)], fill=DEEP)
    d.polygon([(96, 64), (160, 64), (128, 104)], fill=DEEP)
    return img


def dice():
    img, d = canvas()
    d.rounded_rectangle((36, 36, 220, 220), 34, fill=FULL)
    for x, y in ((80, 80), (176, 80), (128, 128), (80, 176), (176, 176)):
        d.ellipse((x - 17, y - 17, x + 17, y + 17), fill=0)
    return img.rotate(12, resample=Image.BICUBIC, center=(128, 128))


def mountain():
    img, d = canvas()
    d.polygon([(150, 52), (246, 214), (60, 214)], fill=SHADE)
    d.polygon([(92, 30), (196, 214), (10, 214)], fill=FULL)
    d.polygon([(92, 30), (122, 84), (106, 76), (92, 92), (78, 76), (62, 84)], fill=255)
    return img


def lightbulb():
    img, d = canvas()
    d.ellipse((44, 12, 212, 180), fill=FULL)
    d.polygon([(76, 140), (180, 140), (164, 190), (92, 190)], fill=FULL)
    for y in (192, 212):
        d.rounded_rectangle((90, y, 166, y + 14), 6, fill=SHADE)
    d.rounded_rectangle((106, 230, 150, 244), 6, fill=DEEP)
    d.arc((84, 46, 136, 98), 180, 270, fill=255, width=10)
    return img


def rocket():
    # Nose, body with a window, fins and a flame; drawn upright, turned to fly up and right
    img, d = canvas()
    d.polygon([(56, 150), (96, 120), (96, 196), (56, 206)], fill=SHADE)
    d.polygon([(200, 150), (160, 120), (160, 196), (200, 206)], fill=SHADE)
    d.polygon([(108, 196), (148, 196), (128, 248)], fill=SHADE)
    d.rounded_rectangle((96, 60, 160, 204), 18, fill=FULL)
    d.polygon([(96, 82), (128, 6), (160, 82)], fill=FULL)
    d.ellipse((108, 88, 148, 128), fill=DEEP)
    d.rectangle((128, 60, 160, 204), fill=SHADE)
    d.ellipse((108, 88, 148, 128), fill=DEEP)
    return img.rotate(-40, resample=Image.BICUBIC, center=(128, 128))


def music():
    # Two beamed notes
    img, d = canvas()
    d.polygon([(90, 52), (214, 22), (214, 64), (90, 94)], fill=FULL)
    d.rectangle((90, 52, 110, 196), fill=FULL)
    d.rectangle((194, 22, 214, 170), fill=FULL)
    for cx, cy in ((72, 200), (176, 174)):
        head = rotate_pts([(cx + 40 * math.cos(t / 12 * math.pi), cy + 28 * math.sin(t / 12 * math.pi)) for t in range(24)], cx, cy, -20)
        d.polygon(head, fill=FULL)
    d.polygon([(90, 52), (214, 22), (214, 40), (90, 70)], fill=SHADE)
    return img


def planet():
    # A ringed planet: the ring's back half behind the ball, its front half over it
    img, d = canvas()
    d.ellipse((6, 98, 250, 158), outline=SHADE, width=16)
    d.ellipse((58, 58, 198, 198), fill=FULL)
    d.chord((58, 58, 198, 198), 20, 160, fill=SHADE)
    front = Image.new("L", (S, S), 0)
    ImageDraw.Draw(front).arc((6, 98, 250, 158), 0, 180, fill=255, width=16)
    img.paste(SHADE, mask=front)
    return img.rotate(-18, resample=Image.BICUBIC, center=(128, 128))


def cloud():
    img, d = canvas()
    d.ellipse((24, 104, 112, 192), fill=FULL)
    d.ellipse((70, 52, 178, 160), fill=FULL)
    d.ellipse((136, 88, 232, 184), fill=FULL)
    d.rounded_rectangle((52, 130, 208, 196), 32, fill=FULL)
    d.rounded_rectangle((52, 172, 208, 196), 12, fill=SHADE)
    return img


def cat():
    img, d = canvas()
    d.polygon([(40, 112), (52, 22), (118, 74)], fill=FULL)
    d.polygon([(216, 112), (204, 22), (138, 74)], fill=FULL)
    d.polygon([(56, 92), (62, 46), (96, 74)], fill=SHADE)
    d.polygon([(200, 92), (194, 46), (160, 74)], fill=SHADE)
    d.ellipse((30, 58, 226, 236), fill=FULL)
    d.ellipse((76, 112, 110, 158), fill=DEEP)
    d.ellipse((146, 112, 180, 158), fill=DEEP)
    d.ellipse((88, 122, 98, 140), fill=255)
    d.ellipse((158, 122, 168, 140), fill=255)
    d.polygon([(116, 170), (140, 170), (128, 184)], fill=SHADE)
    return img


def potion():
    # A round flask, cork in, liquid in the lower half with a couple of bubbles
    img, d = canvas()
    d.rounded_rectangle((96, 8, 160, 40), 8, fill=SHADE)
    d.rectangle((106, 36, 150, 110), fill=SHADE)
    d.ellipse((40, 88, 216, 248), fill=SHADE)
    liquid = Image.new("L", (S, S), 0)
    ImageDraw.Draw(liquid).ellipse((40, 88, 216, 248), fill=255)
    ImageDraw.Draw(liquid).rectangle((0, 0, S, 158), fill=0)
    img.paste(FULL, mask=liquid)
    for x, y, r in ((100, 196, 12), (146, 214, 8), (132, 180, 6)):
        d.ellipse((x - r, y - r, x + r, y + r), fill=DEEP)
    return img


def flower():
    img, d = canvas()
    for k in range(5):
        a = math.radians(-90 + 72 * k)
        cx, cy = 128 + 64 * math.cos(a), 130 + 64 * math.sin(a)
        d.ellipse((cx - 52, cy - 52, cx + 52, cy + 52), fill=FULL)
    for k in range(5):
        a = math.radians(-54 + 72 * k)
        d.line((128, 130, 128 + 70 * math.cos(a), 130 + 70 * math.sin(a)), fill=SHADE, width=6)
    d.ellipse((90, 92, 166, 168), fill=SHADE)
    return img


def mushroom():
    img, d = canvas()
    d.rounded_rectangle((92, 120, 164, 238), 24, fill=SHADE)
    d.pieslice((12, 22, 244, 226), 180, 360, fill=FULL)
    d.rounded_rectangle((12, 112, 244, 130), 8, fill=FULL)
    for x, y, r in ((72, 82, 18), (128, 52, 16), (184, 84, 20), (120, 100, 10)):
        d.ellipse((x - r, y - r, x + r, y + r), fill=0)
    return img


def cherry():
    img, d = canvas()
    d.line((84, 136, 150, 44), fill=SHADE, width=10)
    d.line((172, 148, 150, 44), fill=SHADE, width=10)
    leaf = rotate_pts([(150 + 50 * math.cos(t / 12 * math.pi), 40 + 18 * math.sin(t / 12 * math.pi)) for t in range(24)], 150, 40, -25)
    d.polygon([(x + 34, y - 4) for x, y in leaf], fill=SHADE)
    d.ellipse((28, 124, 128, 224), fill=FULL)
    d.ellipse((124, 136, 224, 236), fill=FULL)
    return img


def butterfly():
    img, d = canvas()
    for side in (-1, 1):
        up = rotate_pts([(128 + side * (60 + 52 * math.cos(t / 12 * math.pi)), 90 + 58 * math.sin(t / 12 * math.pi)) for t in range(24)], 128, 128, side * -15)
        d.polygon(up, fill=FULL)
        low = rotate_pts([(128 + side * (50 + 38 * math.cos(t / 12 * math.pi)), 176 + 42 * math.sin(t / 12 * math.pi)) for t in range(24)], 128, 128, side * 20)
        d.polygon(low, fill=SHADE)
    d.rounded_rectangle((116, 56, 140, 216), 12, fill=DEEP)
    d.line((122, 60, 96, 16), fill=DEEP, width=7)
    d.line((134, 60, 160, 16), fill=DEEP, width=7)
    return img


def fish():
    img, d = canvas()
    d.polygon([(176, 128), (246, 66), (232, 128), (246, 190)], fill=SHADE)
    d.polygon([(88, 84), (128, 40), (160, 92)], fill=SHADE)
    d.ellipse((18, 72, 196, 184), fill=FULL)
    d.arc((68, 92, 116, 164), 300, 60, fill=SHADE, width=8)
    d.ellipse((50, 108, 72, 130), fill=0)
    return img


def feather():
    img, d = canvas()
    d.ellipse((92, 10, 164, 214), fill=FULL)
    d.polygon([(128, 10), (164, 112), (128, 214)], fill=SHADE)
    d.line((128, 30, 128, 250), fill=DEEP, width=8)
    for y in (78, 128):
        d.line((92, y, 120, y + 22), fill=0, width=6)
        d.line((164, y + 14, 136, y + 34), fill=0, width=6)
    return img.rotate(-35, resample=Image.BICUBIC, center=(128, 128))


def tree():
    img, d = canvas()
    d.rectangle((112, 196, 144, 246), fill=DEEP)
    for top, half, bottom in ((10, 62, 92), (54, 88, 150), (100, 112, 206)):
        d.polygon([(128, top), (128 + half, bottom), (128 - half, bottom)], fill=FULL)
    for top, half, bottom in ((10, 62, 92), (54, 88, 150), (100, 112, 206)):
        d.polygon([(128, top), (128 + half, bottom), (128, bottom)], fill=SHADE)
    return img


def drop():
    img, d = canvas()
    d.ellipse((50, 98, 206, 250), fill=FULL)
    d.polygon([(54, 160), (128, 6), (202, 160)], fill=FULL)
    right = Image.new("L", (S, S), 0)
    ImageDraw.Draw(right).ellipse((50, 98, 206, 250), fill=255)
    ImageDraw.Draw(right).polygon([(54, 160), (128, 6), (202, 160)], fill=255)
    ImageDraw.Draw(right).rectangle((0, 0, 128, S), fill=0)
    img.paste(SHADE, mask=right)
    d.arc((80, 150, 140, 210), 100, 180, fill=255, width=10)
    return img


def umbrella():
    img, d = canvas()
    d.pieslice((14, 34, 242, 262), 180, 360, fill=FULL)
    for x0, x1 in ((14, 71), (128, 185)):
        d.polygon([(128, 34), (x0, 148), (x1, 148)], fill=SHADE)
    for x in (42, 99, 157, 214):
        d.ellipse((x - 29, 134, x + 29, 170), fill=0)
    d.rectangle((120, 140, 136, 214), fill=SHADE)
    d.arc((92, 190, 136, 238), 0, 180, fill=SHADE, width=16)
    return img


def bell():
    img, d = canvas()
    d.ellipse((114, 8, 142, 36), fill=SHADE)
    d.ellipse((62, 26, 194, 150), fill=FULL)
    d.polygon([(62, 88), (194, 88), (218, 194), (38, 194)], fill=FULL)
    d.rounded_rectangle((24, 184, 232, 210), 12, fill=SHADE)
    d.ellipse((106, 202, 150, 246), fill=SHADE)
    return img


def wrench():
    img, d = canvas()
    d.rounded_rectangle((70, 110, 196, 146), 12, fill=FULL)
    d.ellipse((10, 76, 112, 180), fill=FULL)
    d.rectangle((0, 112, 54, 144), fill=0)
    d.ellipse((182, 94, 246, 162), fill=FULL)
    d.ellipse((200, 112, 228, 144), fill=0)
    d.rounded_rectangle((96, 120, 170, 136), 6, fill=SHADE)
    return img.rotate(40, resample=Image.BICUBIC, center=(128, 128))


def magnet():
    img, d = canvas()
    d.arc((34, 70, 222, 246), 0, 180, fill=FULL, width=52)
    d.rectangle((34, 24, 86, 160), fill=FULL)
    d.rectangle((170, 24, 222, 160), fill=FULL)
    d.rectangle((34, 24, 86, 70), fill=SHADE)
    d.rectangle((170, 24, 222, 70), fill=SHADE)
    return img


def gamepad():
    img, d = canvas()
    d.ellipse((12, 110, 104, 226), fill=FULL)
    d.ellipse((152, 110, 244, 226), fill=FULL)
    d.rounded_rectangle((22, 70, 234, 184), 54, fill=FULL)
    d.rectangle((60, 112, 100, 128), fill=DEEP)
    d.rectangle((72, 100, 88, 140), fill=DEEP)
    for x, y in ((186, 100), (186, 140), (166, 120), (206, 120)):
        d.ellipse((x - 10, y - 10, x + 10, y + 10), fill=DEEP)
    d.rounded_rectangle((112, 112, 144, 124), 5, fill=SHADE)
    return img


def coffee():
    img, d = canvas()
    for x in (84, 124):
        d.line([(x, 70), (x - 10, 50), (x + 6, 32), (x - 4, 12)], fill=SHADE, width=9, joint="curve")
    d.arc((150, 112, 236, 200), 270, 90, fill=FULL, width=22)
    d.rounded_rectangle((30, 90, 184, 236), 26, fill=FULL)
    d.ellipse((30, 78, 184, 112), fill=SHADE)
    d.ellipse((44, 84, 170, 104), fill=DEEP)
    return img


def pizza():
    img, d = canvas()
    d.polygon([(30, 52), (226, 52), (128, 244)], fill=FULL)
    d.rounded_rectangle((18, 26, 238, 66), 18, fill=SHADE)
    for x, y in ((92, 96), (156, 104), (126, 160)):
        d.ellipse((x - 20, y - 20, x + 20, y + 20), fill=DEEP)
    return img


def ufo():
    img, d = canvas()
    d.ellipse((76, 46, 180, 132), fill=SHADE)
    d.ellipse((12, 100, 244, 176), fill=FULL)
    d.ellipse((12, 132, 244, 176), fill=SHADE)
    for x in (54, 102, 154, 202):
        d.ellipse((x - 10, 128, x + 10, 148), fill=DEEP)
    d.polygon([(96, 176), (160, 176), (186, 244), (70, 244)], fill=60)
    return img


def alien():
    img, d = canvas()
    d.ellipse((34, 14, 222, 184), fill=FULL)
    d.polygon([(46, 128), (210, 128), (150, 228), (106, 228)], fill=FULL)
    d.ellipse((106, 200, 150, 240), fill=FULL)
    for side in (-1, 1):
        eye = rotate_pts([(128 + side * 52 + 34 * math.cos(t / 12 * math.pi), 122 + 18 * math.sin(t / 12 * math.pi)) for t in range(24)],
                         128 + side * 52, 122, side * 30)
        d.polygon(eye, fill=DEEP)
    return img


def smiley():
    img, d = canvas()
    d.ellipse((14, 14, 242, 242), fill=FULL)
    d.ellipse((80, 70, 110, 116), fill=DEEP)
    d.ellipse((146, 70, 176, 116), fill=DEEP)
    d.arc((60, 70, 196, 198), 20, 160, fill=DEEP, width=16)
    return img


def bone():
    img, d = canvas()
    d.rectangle((62, 112, 194, 144), fill=FULL)
    for x, y in ((56, 102), (56, 154), (200, 102), (200, 154)):
        d.ellipse((x - 30, y - 30, x + 30, y + 30), fill=FULL)
    d.rectangle((62, 132, 194, 144), fill=SHADE)
    return img.rotate(35, resample=Image.BICUBIC, center=(128, 128))


def pawn():
    img, d = canvas()
    d.rounded_rectangle((50, 208, 206, 240), 10, fill=SHADE)
    d.polygon([(82, 210), (174, 210), (152, 118), (104, 118)], fill=FULL)
    d.rounded_rectangle((86, 104, 170, 128), 10, fill=SHADE)
    d.ellipse((84, 16, 172, 104), fill=FULL)
    return img


def sparkle():
    img, d = canvas()
    d.polygon(poly_star(110, 140, 112, 26, 4), fill=FULL)
    d.polygon(poly_star(206, 50, 44, 11, 4), fill=SHADE)
    d.polygon(poly_star(212, 200, 26, 7, 4), fill=SHADE)
    return img


def storm():
    # A dark cloud with a bolt under it
    img, d = canvas()
    d.polygon([(136, 128), (100, 196), (128, 196), (110, 250), (172, 170), (142, 170), (162, 128)], fill=FULL)
    d.ellipse((20, 70, 100, 150), fill=SHADE)
    d.ellipse((62, 26, 166, 130), fill=SHADE)
    d.ellipse((128, 58, 222, 152), fill=SHADE)
    d.rounded_rectangle((44, 96, 210, 152), 28, fill=SHADE)
    return img


def broken_heart():
    img, d = canvas()
    d.ellipse((24, 40, 136, 150), fill=FULL)
    d.ellipse((120, 40, 232, 150), fill=FULL)
    d.polygon([(32, 118), (224, 118), (128, 232)], fill=FULL)
    d.polygon([(124, 52), (104, 104), (140, 132), (110, 182), (128, 232), (150, 180), (118, 132), (150, 100), (132, 52)], fill=0)
    d.polygon([(132, 52), (150, 100), (118, 132), (150, 180), (128, 232), (224, 118), (232, 90), (200, 40)], fill=SHADE)
    return img


def diamond():
    img, d = canvas()
    d.polygon([(128, 12), (220, 128), (128, 244), (36, 128)], fill=FULL)
    d.polygon([(128, 12), (220, 128), (128, 244)], fill=SHADE)
    return img


def club():
    img, d = canvas()
    for x, y in ((128, 70), (74, 140), (182, 140)):
        d.ellipse((x - 54, y - 54, x + 54, y + 54), fill=FULL)
    d.polygon([(128, 120), (100, 236), (156, 236)], fill=FULL)
    d.ellipse((128, 86, 236, 194), fill=SHADE)
    d.ellipse((74, 16, 182, 124), fill=FULL)
    return img


def comet():
    # A star with a tail streaming down-left
    img, d = canvas()
    d.polygon([(132, 60), (16, 240), (196, 124)], fill=DEEP)
    d.polygon([(146, 80), (44, 222), (182, 112)], fill=SHADE)
    d.polygon(poly_star(170, 86, 72, 30, 5), fill=FULL)
    return img


def plane():
    # A paper plane
    img, d = canvas()
    d.polygon([(14, 116), (242, 22), (150, 232), (112, 150)], fill=FULL)
    d.polygon([(112, 150), (242, 22), (150, 232)], fill=SHADE)
    d.polygon([(112, 150), (242, 22), (124, 210)], fill=DEEP)
    return img


def leaf():
    img, d = canvas()
    outline = []
    for t in range(0, 31):
        a = t / 30
        outline.append((40 + a * 176, 216 - a * 176 - 70 * math.sin(a * math.pi)))
    for t in range(30, -1, -1):
        a = t / 30
        outline.append((40 + a * 176, 216 - a * 176 + 70 * math.sin(a * math.pi)))
    d.polygon(outline, fill=FULL)
    d.polygon(outline[:31] + [(216, 40), (40, 216)], fill=SHADE)
    d.line((24, 232, 210, 46), fill=DEEP, width=8)
    return img


def cactus():
    img, d = canvas()
    d.rounded_rectangle((98, 20, 158, 220), 30, fill=FULL)
    d.rounded_rectangle((40, 80, 84, 160), 22, fill=FULL)
    d.rounded_rectangle((60, 136, 110, 162), 12, fill=FULL)
    d.rounded_rectangle((172, 60, 216, 140), 22, fill=FULL)
    d.rounded_rectangle((146, 116, 196, 142), 12, fill=FULL)
    d.rectangle((128, 20, 158, 220), fill=SHADE)
    d.rounded_rectangle((70, 210, 186, 246), 8, fill=DEEP)
    return img


def donut():
    img, d = canvas()
    d.ellipse((14, 14, 242, 242), fill=SHADE)
    d.ellipse((28, 22, 228, 214), fill=FULL)
    d.ellipse((92, 92, 164, 164), fill=0)
    for x, y, a in ((70, 70, 30), (170, 64, -40), (60, 150, 70), (176, 160, 10), (120, 46, 80), (196, 112, -70)):
        d.polygon(rotate_pts(rect_pts(x - 12, y - 4, x + 12, y + 4), x, y, a), fill=DEEP)
    return img


def icecream():
    img, d = canvas()
    d.polygon([(70, 120), (186, 120), (128, 250)], fill=SHADE)
    for i in range(3):
        d.line((90 + i * 30, 120, 128 + i * 12, 220), fill=DEEP, width=5)
    d.ellipse((52, 20, 204, 150), fill=FULL)
    d.ellipse((110, 6, 146, 42), fill=DEEP)
    return img


def house():
    img, d = canvas()
    d.polygon([(128, 18), (244, 122), (12, 122)], fill=SHADE)
    d.rectangle((44, 112, 212, 238), fill=FULL)
    d.rectangle((108, 160, 148, 238), fill=DEEP)
    d.rectangle((160, 140, 196, 176), fill=DEEP)
    d.rectangle((168, 40, 196, 92), fill=SHADE)
    return img


def axe():
    # A wedge of a blade on a handle, the edge curving out
    img, d = canvas()
    d.rounded_rectangle((116, 26, 138, 248), 9, fill=SHADE)
    edge = [(214 + 22 * math.sin(math.pi * t / 12), 22 + 136 * t / 12) for t in range(13)]
    d.polygon([(136, 44), (196, 22)] + edge + [(196, 158), (136, 128)], fill=FULL)
    d.polygon([(196, 22)] + edge + [(196, 158)], fill=SHADE)
    d.rounded_rectangle((94, 48, 120, 104), 6, fill=DEEP)
    return img.rotate(-22, resample=Image.BICUBIC, center=(128, 128))


def hammer():
    img, d = canvas()
    d.rounded_rectangle((116, 70, 140, 248), 8, fill=SHADE)
    d.rounded_rectangle((44, 30, 212, 92), 12, fill=FULL)
    d.rectangle((44, 30, 76, 92), fill=DEEP)
    return img.rotate(30, resample=Image.BICUBIC, center=(128, 128))


def arrow():
    img, d = canvas()
    d.polygon([(128, 14), (232, 128), (164, 128), (164, 242), (92, 242), (92, 128), (24, 128)], fill=FULL)
    d.polygon([(128, 14), (232, 128), (164, 128), (164, 242), (128, 242)], fill=SHADE)
    return img


def camera():
    img, d = canvas()
    d.rounded_rectangle((14, 64, 242, 226), 22, fill=FULL)
    d.rounded_rectangle((84, 36, 172, 80), 10, fill=FULL)
    d.ellipse((76, 92, 180, 196), fill=DEEP)
    d.ellipse((98, 114, 158, 174), fill=SHADE)
    d.rectangle((190, 84, 222, 104), fill=SHADE)
    return img


def mail():
    img, d = canvas()
    d.rounded_rectangle((14, 52, 242, 204), 14, fill=FULL)
    d.polygon([(14, 60), (128, 140), (242, 60), (242, 82), (128, 162), (14, 82)], fill=SHADE)
    return img


def battery():
    img, d = canvas()
    d.rounded_rectangle((20, 68, 216, 188), 18, fill=SHADE)
    d.rounded_rectangle((216, 104, 240, 152), 6, fill=SHADE)
    d.rounded_rectangle((38, 86, 198, 170), 10, fill=60)
    for i in range(3):
        d.rounded_rectangle((46 + i * 50, 94, 86 + i * 50, 162), 6, fill=FULL)
    return img


def pumpkin():
    img, d = canvas()
    d.rounded_rectangle((118, 14, 142, 60), 8, fill=DEEP)
    for x0, x1, tone in ((14, 140, SHADE), (116, 242, SHADE), (60, 196, FULL)):
        d.ellipse((x0, 46, x1, 238), fill=tone)
    d.polygon([(78, 120), (110, 120), (94, 96)], fill=0)
    d.polygon([(146, 120), (178, 120), (162, 96)], fill=0)
    d.polygon([(74, 168), (182, 168), (164, 196), (146, 182), (128, 200), (110, 182), (92, 196)], fill=0)
    return img


def bunny():
    img, d = canvas()
    d.ellipse((62, 4, 112, 140), fill=FULL)
    d.ellipse((144, 4, 194, 140), fill=FULL)
    d.ellipse((76, 22, 98, 118), fill=SHADE)
    d.ellipse((158, 22, 180, 118), fill=SHADE)
    d.ellipse((34, 92, 222, 250), fill=FULL)
    d.ellipse((84, 150, 108, 178), fill=DEEP)
    d.ellipse((148, 150, 172, 178), fill=DEEP)
    d.polygon([(118, 192), (138, 192), (128, 204)], fill=SHADE)
    return img


def fox():
    img, d = canvas()
    d.polygon([(30, 30), (110, 80), (60, 120)], fill=FULL)
    d.polygon([(226, 30), (146, 80), (196, 120)], fill=FULL)
    d.polygon([(28, 70), (128, 236), (228, 70), (128, 60)], fill=FULL)
    d.polygon([(70, 150), (128, 236), (186, 150), (128, 176)], fill=SHADE)
    d.ellipse((86, 112, 108, 136), fill=DEEP)
    d.ellipse((148, 112, 170, 136), fill=DEEP)
    d.ellipse((116, 216, 140, 238), fill=DEEP)
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
    ("grenade", grenade, "#7D9A5A"),
    ("backpack", backpack, "#B58B5A"),
    ("knife", knife, "#C9D1D8"),
    ("bitcoin", bitcoin, "#F7A531"),
    ("padlock", padlock, "#E2B84A"),
    ("compass", compass, "#E05A47"),
    ("flag", flag, "#4D9BFF"),
    ("biohazard", biohazard, "#C8F03A"),
    ("ghost", ghost, "#E9EEF5"),
    ("bomb", bomb, "#8F99A6"),
    ("capsule", capsule, "#FF5C7A"),
    ("syringe", syringe, "#56C8F0"),
    ("hourglass", hourglass, "#D9B36C"),
    ("dice", dice, "#F2F2F2"),
    ("mountain", mountain, "#7FA7D9"),
    ("lightbulb", lightbulb, "#FFE15A"),
    ("rocket", rocket, "#EDEFF2"),
    ("music", music, "#B98CFF"),
    ("planet", planet, "#F2A65A"),
    ("cloud", cloud, "#CFE4FF"),
    ("cat", cat, "#F0A04B"),
    ("potion", potion, "#B35CFF"),
    ("flower", flower, "#FF7BB5"),
    ("mushroom", mushroom, "#E5483B"),
    ("cherry", cherry, "#E0213F"),
    ("butterfly", butterfly, "#59B8FF"),
    ("fish", fish, "#FF9A3C"),
    ("feather", feather, "#E8E2D0"),
    ("tree", tree, "#3FAE5B"),
    ("drop", drop, "#3FA9F5"),
    ("umbrella", umbrella, "#A66CFF"),
    ("bell", bell, "#F5C542"),
    ("wrench", wrench, "#A9B4BF"),
    ("magnet", magnet, "#E5484D"),
    ("gamepad", gamepad, "#8C7BFF"),
    ("coffee", coffee, "#B07A4F"),
    ("pizza", pizza, "#F6C451"),
    ("ufo", ufo, "#8DE36B"),
    ("alien", alien, "#7CE38B"),
    ("smiley", smiley, "#FFD43B"),
    ("bone", bone, "#EDE6D6"),
    ("pawn", pawn, "#D9D9D9"),
    ("sparkle", sparkle, "#FFE9A8"),
    ("storm", storm, "#8FA3B8"),
    ("broken-heart", broken_heart, "#FF3D6E"),
    ("diamond", diamond, "#FF4D6A"),
    ("club", club, "#5C6B80"),
    ("comet", comet, "#7FD8FF"),
    ("plane", plane, "#F2F4F7"),
    ("leaf", leaf, "#7BC86C"),
    ("cactus", cactus, "#4FA35A"),
    ("donut", donut, "#F48FB1"),
    ("icecream", icecream, "#FFB6D0"),
    ("house", house, "#E0A060"),
    ("axe", axe, "#B8C2CC"),
    ("hammer", hammer, "#A9B4BF"),
    ("arrow", arrow, "#4DD0E1"),
    ("camera", camera, "#8C96A3"),
    ("mail", mail, "#F2D27A"),
    ("battery", battery, "#6CCB5F"),
    ("pumpkin", pumpkin, "#FF8A1E"),
    ("bunny", bunny, "#EADFD3"),
    ("fox", fox, "#F07A2C"),
]


# ---------------------------------------------------------------- animated icons
# A strip of square frames side by side; the plugin plays any icon that's a whole number of squares wide.


def scaled(tones, sx, sy, anchor_bottom=False):
    """The tone map squeezed or stretched about its centre (or its bottom edge)."""
    w, h = max(1, round(S * sx)), max(1, round(S * sy))
    img = Image.new("L", (S, S), 0)
    img.paste(tones.resize((w, h), Image.LANCZOS), ((S - w) // 2, S - h if anchor_bottom else (S - h) // 2))
    return img


def coin_frames(n):
    # The bitcoin turning about its upright axis, with a B on both faces (never a mirrored one) and an
    # edge you see as it turns, so side-on it's a thick coin rather than a flicker of a line
    face = bitcoin()
    radius, thick = 116, 30
    frames = []
    for i in range(n):
        a = 2 * math.pi * i / n
        w = abs(math.cos(a))
        edge = thick * math.sin(a) * (1 if math.cos(a) >= 0 else -1)
        img, d = canvas()
        steps = max(1, int(abs(edge)))
        for k in range(steps + 1):
            x = 128 - edge / 2 + edge * k / steps
            d.ellipse((x - radius * w, 12, x + radius * w, 244), fill=DEEP)
        front = scaled(face, max(w, 0.01), 1)
        shift = int(round(-edge / 2))
        moved = Image.new("L", (S, S), 0)
        moved.paste(front, (shift, 0))
        img.paste(moved, mask=moved.point(lambda v: 255 if v else 0))
        frames.append(img)
    return frames


def heart_frames(n):
    # Two quick beats, then rest
    beat = [1.0, 0.86, 0.96, 0.84, 0.82, 0.82, 0.82, 0.82]
    return [scaled(heart(), k, k) for k in beat[:n]]


def flame_frames(n):
    # Taller and shorter, leaning a little either way
    frames = []
    for i in range(n):
        a = 2 * math.pi * i / n
        tones = scaled(flame(), 0.92 + 0.05 * math.sin(2 * a), 0.86 + 0.12 * math.sin(a), anchor_bottom=True)
        frames.append(tones.rotate(4 * math.sin(a + 1), resample=Image.BICUBIC, center=(128, 230)))
    return frames


def star_frames(n):
    # A fifth of a turn per loop (so it joins up), twinkling as it goes
    return [scaled(star(), 0.86 + 0.1 * abs(math.sin(math.pi * i / n)), 0.86 + 0.1 * abs(math.sin(math.pi * i / n)))
            .rotate(-72 * i / n, resample=Image.BICUBIC) for i in range(n)]


def cog():
    # A generic eight-tooth cog
    img, d = canvas()
    cx = cy = 128
    d.ellipse((cx - 90, cy - 90, cx + 90, cy + 90), fill=FULL)
    for i in range(8):
        tooth = [(cx - 20, cy - 120), (cx + 20, cy - 120), (cx + 26, cy - 80), (cx - 26, cy - 80)]
        d.polygon(rotate_pts(tooth, cx, cy, i * 45), fill=FULL)
    d.ellipse((cx - 44, cy - 44, cx + 44, cy + 44), fill=SHADE)
    d.ellipse((cx - 22, cy - 22, cx + 22, cy + 22), fill=0)
    return img


def cog_frames(n):
    # An eighth of a turn per loop: the teeth line up again, so it joins
    return [cog().rotate(-45 * i / n, resample=Image.BICUBIC) for i in range(n)]


def radar_frames(n):
    # Rings spreading out from the middle, fading as they go
    frames = []
    for i in range(n):
        img, d = canvas()
        for k in range(3):
            phase = (i / n + k / 3) % 1
            r = 24 + phase * 96
            tone = int(FULL - phase * (FULL - 60))
            d.ellipse((128 - r, 128 - r, 128 + r, 128 + r), outline=tone, width=14)
        d.ellipse((108, 108, 148, 148), fill=FULL)
        frames.append(img)
    return frames


def eye_frames(n):
    # Open, a quick blink, open
    shut = {n - 3: 0.45, n - 2: 0.08, n - 1: 0.45}
    return [scaled(eye(), 1, shut.get(i, 1)) for i in range(n)]


def bolt_frames(n):
    # Flickers: bright, dim, bright again, then holds
    levels = [1, 0.55, 1, 0.7, 1, 1, 1, 1, 1, 1][:n]
    return [bolt().point(lambda v, k=k: int(v * k) if v else 0) for k in levels]


def hourglass_frames(n):
    # Rests, then turns over: half a turn per loop looks the same, so it joins
    frames = []
    for i in range(n):
        turn = max(0, (i - n // 2) / (n - n // 2))
        frames.append(scaled(hourglass(), 0.86, 0.86).rotate(-180 * turn, resample=Image.BICUBIC))
    return frames


def sparkle_frames(n):
    # Three stars, each swelling and shrinking at its own moment
    frames = []
    for i in range(n):
        img, d = canvas()
        for (x, y, r, tone), off in (((110, 140, 112, FULL), 0), ((206, 50, 44, SHADE), 1 / 3), ((212, 200, 30, SHADE), 2 / 3)):
            k = 0.45 + 0.55 * (0.5 + 0.5 * math.cos(2 * math.pi * (i / n + off)))
            d.polygon(poly_star(x, y, r * k, r * k * 0.24, 4), fill=tone)
        frames.append(img)
    return frames


def ghost_frames(n):
    # Floats up and down, swaying a little
    frames = []
    for i in range(n):
        a = 2 * math.pi * i / n
        g = scaled(ghost(), 0.82, 0.82)
        g = g.transform(g.size, Image.AFFINE, (1, 0, 0, 0, 1, -14 * math.sin(a)), resample=Image.BICUBIC)
        frames.append(g.rotate(5 * math.sin(a + 1), resample=Image.BICUBIC))
    return frames


def rocket_with_flame(length):
    img, d = canvas()
    d.polygon([(56, 150), (96, 120), (96, 196), (56, 206)], fill=SHADE)
    d.polygon([(200, 150), (160, 120), (160, 196), (200, 206)], fill=SHADE)
    d.polygon([(108, 196), (148, 196), (128, 196 + length)], fill=SHADE)
    d.polygon([(118, 196), (138, 196), (128, 196 + length * 0.6)], fill=FULL)
    d.rounded_rectangle((96, 60, 160, 204), 18, fill=FULL)
    d.polygon([(96, 82), (128, 6), (160, 82)], fill=FULL)
    d.rectangle((128, 60, 160, 204), fill=SHADE)
    d.ellipse((108, 88, 148, 128), fill=DEEP)
    return img.rotate(-40, resample=Image.BICUBIC, center=(128, 128))


def rocket_frames(n):
    # The flame flickers and the rocket hovers a little along its line of flight
    frames = []
    for i in range(n):
        a = 2 * math.pi * i / n
        r = scaled(rocket_with_flame(40 + 18 * math.sin(2 * a)), 0.86, 0.86)
        k = 8 * math.sin(a)
        frames.append(r.transform(r.size, Image.AFFINE, (1, 0, -k * 0.64, 0, 1, k * 0.77), resample=Image.BICUBIC))
    return frames


ANIMATED = [
    ("animated-coin", coin_frames, 16, "#F7A531"),
    ("animated-heart", heart_frames, 8, "#FF3D6E"),
    ("animated-flame", flame_frames, 8, "#FF7A1A"),
    ("animated-star", star_frames, 10, "#FFC93C"),
    ("animated-cog", cog_frames, 8, "#9AA8B6"),
    ("animated-radar", radar_frames, 12, "#4FD65A"),
    ("animated-eye", eye_frames, 12, "#7CFFB2"),
    ("animated-bolt", bolt_frames, 8, "#FFE04A"),
    ("animated-hourglass", hourglass_frames, 12, "#D9B36C"),
    ("animated-sparkle", sparkle_frames, 12, "#FFE9A8"),
    ("animated-ghost", ghost_frames, 12, "#E9EEF5"),
    ("animated-rocket", rocket_frames, 8, "#EDEFF2"),
]


def render_strip(frames, color):
    strip = Image.new("RGBA", (OUT * len(frames), OUT), (0, 0, 0, 0))
    for i, tones in enumerate(frames):
        strip.alpha_composite(render(tones, color), (i * OUT, 0))
    return strip


# ---------------------------------------------------------------- icons in more than one color


def render_layers(layers):
    """Like render(), for an icon made of several colors: (tone map, color) pairs, drawn in order,
    each shaded the same way, with one outline around them all."""
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    alpha = Image.new("L", (S, S), 0)
    for tones, color in layers:
        rgb = hex_rgb(color)
        layer = Image.new("RGBA", (S, S), (0, 0, 0, 0))
        px, tp = layer.load(), tones.load()
        for y in range(S):
            light = 1.18 - 0.36 * y / S
            for x in range(S):
                v = tp[x, y]
                if v:
                    k = v / 255 * light
                    px[x, y] = tuple(min(255, int(c * k)) for c in rgb) + (255,)
        img.alpha_composite(layer)
        alpha = ImageChops.lighter(alpha, tones.point(lambda v: 255 if v > 0 else 0))

    outline = alpha.filter(ImageFilter.MaxFilter(21))
    ring = ImageChops.subtract(outline, alpha)
    base = Image.new("RGBA", (S, S), (10, 12, 13, 0))
    base.putalpha(ring)
    base.alpha_composite(img)
    padded = Image.new("RGBA", (S + 24, S + 24), (0, 0, 0, 0))
    padded.alpha_composite(base, (12, 12))
    return padded.resize((OUT, OUT), Image.LANCZOS)


def prestige_medal():
    """A nod to Modern Warfare 2's tenth prestige emblem: a gold medal, a chrome skull, and gold
    bands either side with three dots each. Drawn fresh in this library's style."""
    gold, d = canvas()
    d.ellipse((8, 8, 248, 248), fill=FULL)
    d.ellipse((24, 24, 232, 232), fill=SHADE)
    d.ellipse((32, 32, 224, 224), fill=55)
    bands = Image.new("L", (S, S), 0)
    bd = ImageDraw.Draw(bands)
    bd.ellipse((32, 32, 224, 224), fill=255)
    bd.ellipse((58, 26, 198, 230), fill=0)
    gold.paste(FULL, mask=bands)

    # The skull fills the face: chrome, bright at the top, darker toward the jaw
    silver, d = canvas()
    d.ellipse((68, 34, 188, 152), fill=FULL)
    d.ellipse((72, 104, 114, 150), fill=205)
    d.ellipse((142, 104, 184, 150), fill=205)
    d.rounded_rectangle((100, 136, 156, 208), 16, fill=185)
    d.ellipse((82, 90, 122, 128), fill=55)
    d.ellipse((134, 90, 174, 128), fill=55)
    d.polygon([(82, 92), (122, 104), (122, 88)], fill=FULL)
    d.polygon([(174, 92), (134, 104), (134, 88)], fill=FULL)
    d.ellipse((92, 104, 104, 116), fill=150)
    d.ellipse((144, 104, 156, 116), fill=150)
    d.polygon([(128, 128), (117, 150), (139, 150)], fill=55)
    d.rectangle((102, 170, 154, 175), fill=80)
    for x in (113, 128, 143):
        d.rectangle((x - 3, 175, x + 3, 206), fill=80)

    dots, d = canvas()
    for x in (45, 211):
        for y in (98, 128, 158):
            d.ellipse((x - 8, y - 8, x + 8, y + 8), fill=FULL)

    return [(gold, "#E3B53C"), (silver, "#E2E6EA"), (dots, "#FFFFFF")]


MULTI = [
    ("prestige-medal", prestige_medal),
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

    for name, draw in MULTI:
        icon = render_layers(draw())
        icon.save(os.path.join(DEST, f"{name}.png"))
        rendered.append((name, icon))

    for name, frames, count, color in ANIMATED:
        strip = render_strip(frames(count), color)
        strip.save(os.path.join(DEST, f"{name}.png"))
        rendered.append((name, strip.crop((0, 0, OUT, OUT))))

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
