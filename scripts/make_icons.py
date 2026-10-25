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


# ---------------------------------------------------------------- batch 5: animals, food, games, symbols, outdoors


def regular(cx, cy, r, n, rot=-90):
    return [(cx + r * math.cos(math.radians(rot + i * 360 / n)), cy + r * math.sin(math.radians(rot + i * 360 / n))) for i in range(n)]


def trail(d, pts, r0, r1, tone):
    """A thick stroke that tapers from r0 to r1 along pts (a tail, a tentacle, a trunk)."""
    steps = 60
    for i in range(steps + 1):
        t = i / steps
        k = t * (len(pts) - 1)
        j = min(int(k), len(pts) - 2)
        f = k - j
        x = pts[j][0] + (pts[j + 1][0] - pts[j][0]) * f
        y = pts[j][1] + (pts[j + 1][1] - pts[j][1]) * f
        r = r0 + (r1 - r0) * t
        d.ellipse((x - r, y - r, x + r, y + r), fill=tone)


def clip(tones, mask):
    """Tones kept only where mask is drawn."""
    return ImageChops.darker(tones, mask.point(lambda v: 255 if v else 0))


def dog():
    img, d = canvas()
    d.ellipse((22, 50, 92, 186), fill=SHADE)
    d.ellipse((164, 50, 234, 186), fill=SHADE)
    d.ellipse((56, 28, 200, 184), fill=FULL)
    d.ellipse((82, 128, 174, 234), fill=FULL)
    d.ellipse((90, 92, 112, 116), fill=DEEP)
    d.ellipse((144, 92, 166, 116), fill=DEEP)
    d.ellipse((106, 138, 150, 170), fill=DEEP)
    d.line((128, 168, 128, 196), fill=DEEP, width=8)
    d.arc((96, 172, 160, 220), 20, 160, fill=DEEP, width=8)
    return img


def owl():
    img, d = canvas()
    d.polygon([(50, 34), (96, 64), (62, 100)], fill=FULL)
    d.polygon([(206, 34), (160, 64), (194, 100)], fill=FULL)
    d.ellipse((44, 50, 212, 246), fill=FULL)
    d.ellipse((80, 152, 176, 240), fill=SHADE)
    d.ellipse((60, 70, 130, 140), fill=SHADE)
    d.ellipse((126, 70, 196, 140), fill=SHADE)
    d.ellipse((80, 90, 110, 120), fill=DEEP)
    d.ellipse((146, 90, 176, 120), fill=DEEP)
    d.polygon([(114, 128), (142, 128), (128, 156)], fill=DEEP)
    return img


def bear():
    img, d = canvas()
    d.ellipse((30, 22, 102, 94), fill=FULL)
    d.ellipse((154, 22, 226, 94), fill=FULL)
    d.ellipse((50, 42, 82, 74), fill=SHADE)
    d.ellipse((174, 42, 206, 74), fill=SHADE)
    d.ellipse((38, 46, 218, 226), fill=FULL)
    d.ellipse((84, 130, 172, 212), fill=SHADE)
    d.ellipse((106, 138, 150, 166), fill=DEEP)
    d.ellipse((84, 98, 106, 120), fill=DEEP)
    d.ellipse((150, 98, 172, 120), fill=DEEP)
    d.line((128, 164, 128, 186), fill=DEEP, width=8)
    return img


def wolf():
    img, d = canvas()
    d.polygon([(38, 14), (108, 72), (54, 120)], fill=FULL)
    d.polygon([(218, 14), (148, 72), (202, 120)], fill=FULL)
    d.polygon([(56, 40), (90, 72), (62, 98)], fill=SHADE)
    d.polygon([(200, 40), (166, 72), (194, 98)], fill=SHADE)
    d.polygon([(28, 100), (76, 62), (180, 62), (228, 100), (210, 164), (128, 246), (46, 164)], fill=FULL)
    d.polygon([(84, 150), (128, 246), (172, 150), (128, 168)], fill=SHADE)
    d.polygon([(72, 112), (114, 122), (80, 134)], fill=DEEP)
    d.polygon([(184, 112), (142, 122), (176, 134)], fill=DEEP)
    d.ellipse((112, 210, 144, 240), fill=DEEP)
    return img


def penguin():
    body, d = canvas()
    d.ellipse((58, 18, 198, 236), fill=FULL)
    d.ellipse((26, 104, 82, 206), fill=SHADE)
    d.ellipse((174, 104, 230, 206), fill=SHADE)
    belly, d = canvas()
    d.ellipse((84, 84, 172, 228), fill=FULL)
    d.ellipse((82, 48, 128, 106), fill=FULL)
    d.ellipse((128, 48, 174, 106), fill=FULL)
    eyes, d = canvas()
    d.ellipse((98, 66, 114, 84), fill=FULL)
    d.ellipse((142, 66, 158, 84), fill=FULL)
    beak, d = canvas()
    d.polygon([(110, 92), (146, 92), (128, 114)], fill=FULL)
    d.ellipse((76, 220, 124, 246), fill=SHADE)
    d.ellipse((132, 220, 180, 246), fill=SHADE)
    return [(body, "#56637A"), (belly, "#F2F4F7"), (eyes, "#2A2F38"), (beak, "#FF9A2E")]


def frog():
    img, d = canvas()
    d.ellipse((34, 34, 112, 112), fill=FULL)
    d.ellipse((144, 34, 222, 112), fill=FULL)
    d.ellipse((18, 72, 238, 222), fill=FULL)
    d.ellipse((54, 54, 92, 92), fill=DEEP)
    d.ellipse((164, 54, 202, 92), fill=DEEP)
    d.arc((58, 100, 198, 194), 25, 155, fill=DEEP, width=10)
    d.ellipse((40, 150, 74, 176), fill=SHADE)
    d.ellipse((182, 150, 216, 176), fill=SHADE)
    return img


def duck():
    body, d = canvas()
    d.ellipse((20, 118, 226, 236), fill=FULL)
    d.ellipse((96, 26, 196, 126), fill=FULL)
    d.polygon([(30, 150), (6, 106), (64, 136)], fill=FULL)
    d.chord((52, 132, 156, 212), 0, 180, fill=SHADE)
    d.ellipse((146, 56, 166, 76), fill=DEEP)
    beak, d = canvas()
    d.polygon([(184, 72), (246, 86), (240, 100), (186, 106)], fill=FULL)
    return [(body, "#FFD43B"), (beak, "#FF8A1E")]


def snake():
    img, d = canvas()
    pts = [(26, 210), (70, 228), (118, 200), (120, 150), (92, 112), (100, 66), (150, 44)]
    trail(d, pts, 9, 24, FULL)
    d.ellipse((132, 20, 206, 76), fill=FULL)
    d.ellipse((164, 34, 180, 50), fill=DEEP)
    d.polygon([(204, 50), (236, 46), (246, 34), (240, 52), (248, 64), (234, 56), (204, 58)], fill=DEEP)
    for x, y in ((64, 222), (114, 176), (100, 92)):
        d.ellipse((x - 7, y - 7, x + 7, y + 7), fill=SHADE)
    return img


def spider():
    img, d = canvas()
    for side in (-1, 1):
        for k, (kx, ky, fx, fy) in enumerate(((70, 70, 112, 34), (84, 112, 122, 98), (84, 150, 120, 176), (70, 186, 106, 236))):
            d.line([(128, 140), (128 + side * kx, ky), (128 + side * fx, fy)], fill=SHADE, width=12, joint="curve")
    d.ellipse((84, 112, 172, 216), fill=FULL)
    d.ellipse((102, 64, 154, 120), fill=FULL)
    d.ellipse((110, 82, 124, 96), fill=DEEP)
    d.ellipse((132, 82, 146, 96), fill=DEEP)
    d.polygon([(128, 136), (148, 164), (128, 196), (108, 164)], fill=DEEP)
    return img


def octopus():
    img, d = canvas()
    # Back pair a shade darker, front pair curling outward
    for x, side in ((104, -1), (152, 1)):
        trail(d, [(x, 140), (x + side * 6, 196), (x + side * 22, 234), (x + side * 40, 238)], 18, 8, SHADE)
    for x, side in ((76, -1), (180, 1)):
        trail(d, [(x, 130), (x + side * 12, 186), (x + side * 36, 222), (x + side * 58, 214)], 21, 9, FULL)
    d.ellipse((48, 14, 208, 170), fill=FULL)
    d.ellipse((84, 82, 116, 116), fill=DEEP)
    d.ellipse((140, 82, 172, 116), fill=DEEP)
    d.ellipse((76, 40, 104, 64), fill=SHADE)
    return img


def turtle():
    img, d = canvas()
    d.ellipse((102, 8, 154, 66), fill=SHADE)
    for box in ((26, 62, 82, 112), (174, 62, 230, 112), (26, 170, 82, 220), (174, 170, 230, 220)):
        d.ellipse(box, fill=SHADE)
    d.polygon([(118, 222), (138, 222), (128, 250)], fill=SHADE)
    d.ellipse((44, 42, 212, 234), fill=FULL)
    hexagon_pts = regular(128, 138, 38, 6, rot=0)
    d.polygon(hexagon_pts, fill=SHADE)
    for p in hexagon_pts:
        dx, dy = p[0] - 128, p[1] - 138
        d.line((p[0], p[1], 128 + dx * 2.4, 138 + dy * 2.4), fill=SHADE, width=8)
    return img


def crab():
    img, d = canvas()
    for side in (-1, 1):
        cx = 128 + side * 86
        d.line((128 + side * 50, 130, cx, 76), fill=FULL, width=18)
        d.ellipse((cx - 38, 30, cx + 38, 106), fill=FULL)
        d.pieslice((cx - 38, 30, cx + 38, 106), 250 if side < 0 else 250, 300, fill=0)
        for k in range(3):
            d.line([(128 + side * 60, 170 + k * 14), (128 + side * 104, 182 + k * 20), (128 + side * 116, 214 + k * 12)],
                   fill=SHADE, width=10, joint="curve")
    d.ellipse((54, 112, 202, 214), fill=FULL)
    for x in (104, 152):
        d.line((x, 120, x, 92), fill=FULL, width=8)
        d.ellipse((x - 11, 78, x + 11, 100), fill=DEEP)
    d.arc((96, 150, 160, 192), 20, 160, fill=DEEP, width=7)
    return img


def whale():
    img, d = canvas()
    d.ellipse((10, 86, 200, 218), fill=FULL)
    d.polygon([(176, 150), (214, 120), (244, 92), (236, 132), (220, 152), (236, 172), (244, 210), (214, 184)], fill=FULL)
    d.chord((10, 86, 200, 218), 25, 155, fill=SHADE)
    d.ellipse((52, 140, 70, 158), fill=DEEP)
    for x, y, r in ((84, 60, 10), (104, 36, 12), (128, 56, 9), (100, 14, 8)):
        d.ellipse((x - r, y - r, x + r, y + r), fill=SHADE)
    return img


def bat():
    img, d = canvas()
    left = [(128, 100), (70, 64), (10, 84), (36, 112), (32, 152), (64, 134), (86, 168), (112, 142)]
    d.polygon(left, fill=SHADE)
    d.polygon([(256 - x, y) for x, y in left], fill=SHADE)
    d.ellipse((98, 82, 158, 190), fill=FULL)
    d.polygon([(100, 96), (106, 56), (124, 86)], fill=FULL)
    d.polygon([(156, 96), (150, 56), (132, 86)], fill=FULL)
    d.ellipse((108, 104, 122, 118), fill=DEEP)
    d.ellipse((134, 104, 148, 118), fill=DEEP)
    return img


def bee():
    img, d = canvas()
    d.ellipse((66, 18, 136, 112), fill=SHADE)
    d.ellipse((120, 18, 190, 112), fill=SHADE)
    body = Image.new("L", (S, S), 0)
    ImageDraw.Draw(body).ellipse((26, 88, 212, 222), fill=255)
    img.paste(FULL, mask=body)
    stripes = Image.new("L", (S, S), 0)
    sd = ImageDraw.Draw(stripes)
    for x in (96, 146):
        sd.rectangle((x, 0, x + 24, S), fill=255)
    img.paste(DEEP, mask=clip(stripes, body))
    d.polygon([(206, 146), (246, 155), (206, 166)], fill=DEEP)
    d.ellipse((46, 128, 64, 146), fill=DEEP)
    return img


def ladybug():
    dark, d = canvas()
    d.ellipse((86, 18, 170, 92), fill=FULL)
    for y in (100, 146, 192):
        d.line((20, y, 236, y + 6), fill=FULL, width=10)
    d.line((104, 30, 84, 6), fill=FULL, width=6)
    d.line((152, 30, 172, 6), fill=FULL, width=6)
    shell, d = canvas()
    d.ellipse((38, 58, 218, 242), fill=FULL)
    d.pieslice((38, 58, 218, 242), -90, 90, fill=SHADE)
    spots, d = canvas()
    d.line((128, 60, 128, 240), fill=FULL, width=8)
    for x, y, r in ((84, 120, 18), (172, 120, 18), (78, 182, 16), (178, 182, 16), (112, 214, 12), (144, 214, 12)):
        d.ellipse((x - r, y - r, x + r, y + r), fill=FULL)
    return [(dark, "#4A5260"), (shell, "#E5373E"), (spots, "#3B4250")]


def snail():
    img, d = canvas()
    d.rounded_rectangle((16, 186, 240, 232), 22, fill=SHADE)
    d.ellipse((14, 132, 74, 214), fill=SHADE)
    for x in (30, 58):
        d.line((x + 10, 140, x, 92), fill=SHADE, width=8)
        d.ellipse((x - 10, 82, x + 10, 102), fill=SHADE)
    d.ellipse((74, 42, 234, 202), fill=FULL)
    for r, box in ((10, (98, 66, 210, 178)), (10, (122, 90, 186, 154)), (9, (142, 110, 166, 134))):
        d.ellipse(box, outline=DEEP, width=r)
    return img


def shark():
    img, d = canvas()
    d.polygon([(8, 146), (54, 112), (150, 98), (206, 118), (244, 70), (232, 144), (246, 210), (206, 172), (150, 190), (54, 180)], fill=FULL)
    d.polygon([(104, 104), (150, 26), (164, 102)], fill=FULL)
    d.polygon([(110, 184), (128, 226), (150, 188)], fill=SHADE)
    d.polygon([(8, 146), (54, 180), (150, 190), (206, 172), (150, 160), (54, 158)], fill=SHADE)
    d.ellipse((46, 126, 62, 142), fill=DEEP)
    for x in (84, 96, 108):
        d.line((x, 128, x - 6, 158), fill=DEEP, width=6)
    return img


def burger():
    bun, d = canvas()
    d.chord((18, 26, 238, 186), 180, 360, fill=FULL)
    d.rounded_rectangle((24, 196, 232, 236), 18, fill=SHADE)
    seeds, d = canvas()
    for x, y in ((80, 64), (124, 50), (168, 66), (102, 86), (150, 90), (60, 96), (196, 98)):
        d.ellipse((x - 9, y - 5, x + 9, y + 5), fill=FULL)
    lettuce, d = canvas()
    d.polygon([(14, 116), (40, 108), (64, 124), (90, 108), (116, 124), (140, 108), (166, 124), (192, 108), (216, 124), (242, 112),
               (238, 140), (18, 140)], fill=FULL)
    patty, d = canvas()
    d.rounded_rectangle((20, 138, 236, 186), 22, fill=FULL)
    cheese, d = canvas()
    d.polygon([(26, 134), (230, 134), (230, 146), (176, 146), (160, 170), (144, 146), (26, 146)], fill=FULL)
    return [(bun, "#E8A04B"), (seeds, "#FFF4D6"), (lettuce, "#6CCB5F"), (patty, "#8C5A3C"), (cheese, "#FFD43B")]


def fries():
    # A carton: its back wall behind the fries, fries fanned out in two rows, and the front panel with
    # a top that dips in the middle, so the fries show above it
    def stick(d, angle, top, tone):
        pts = rotate_pts([(115, top), (141, top), (141, 230), (115, 230)], 128, 300, angle)
        d.polygon(pts, fill=tone)

    back, d = canvas()
    d.polygon([(40, 112), (216, 112), (206, 170), (50, 170)], fill=DEEP)
    potato, d = canvas()
    for angle, top in ((-21, 36), (-7, 18), (7, 18), (21, 36)):
        stick(d, angle, top, SHADE)
    for angle, top in ((-14, 46), (0, 30), (14, 46)):
        stick(d, angle, top, FULL)
    front, d = canvas()
    curve = [(220 - 184 * t, 116 + 4 * 46 * t * (1 - t)) for t in (i / 20 for i in range(21))]
    d.polygon([(60, 246), (196, 246)] + curve, fill=FULL)
    shade = Image.new("L", (S, S), 0)
    ImageDraw.Draw(shade).polygon([(162, 0), (S, 0), (S, S), (178, S)], fill=255)
    front.paste(SHADE, mask=clip(shade, front))
    return [(back, "#E5373E"), (potato, "#FFD43B"), (front, "#E5373E")]


def watermelon():
    rind, d = canvas()
    d.pieslice((10, -60, 246, 236), 20, 160, fill=FULL)
    flesh, d = canvas()
    d.pieslice((32, -40, 224, 212), 20, 160, fill=FULL)
    seeds, d = canvas()
    for x, y in ((92, 128), (128, 150), (164, 128), (110, 172), (146, 172)):
        d.ellipse((x - 6, y - 10, x + 6, y + 10), fill=FULL)
    return [(rind, "#4FA35A"), (flesh, "#FF4D6A"), (seeds, "#3B4250")]


def lemon():
    img, d = canvas()
    d.ellipse((20, 20, 236, 236), fill=SHADE)
    d.ellipse((36, 36, 220, 220), fill=FULL)
    for k in range(8):
        a = math.radians(k * 45)
        d.line((128, 128, 128 + 92 * math.cos(a), 128 + 92 * math.sin(a)), fill=SHADE, width=8)
    d.ellipse((112, 112, 144, 144), fill=SHADE)
    return img


def egg():
    white, d = canvas()
    d.polygon([(40, 96), (86, 34), (164, 30), (222, 84), (226, 160), (182, 222), (100, 230), (34, 182)], fill=FULL)
    white = white.filter(ImageFilter.GaussianBlur(10)).point(lambda v: FULL if v > 128 else 0)
    yolk, d = canvas()
    d.ellipse((84, 82, 172, 170), fill=FULL)
    d.ellipse((104, 98, 126, 120), fill=SHADE)
    return [(white, "#F4F2EC"), (yolk, "#FFB21E")]


def cookie():
    img, d = canvas()
    d.ellipse((20, 20, 236, 236), fill=FULL)
    d.ellipse((176, -10, 266, 80), fill=0)
    d.ellipse((150, 4, 196, 50), fill=0)
    for x, y in ((80, 76), (130, 96), (70, 140), (120, 164), (172, 136), (100, 206), (164, 192), (194, 96)):
        d.ellipse((x - 11, y - 9, x + 11, y + 9), fill=DEEP)
    return img


def cupcake():
    cup, d = canvas()
    d.polygon([(52, 140), (204, 140), (184, 240), (72, 240)], fill=FULL)
    for x in (86, 112, 138, 164):
        d.line((x + 6, 144, x + 2, 236), fill=SHADE, width=8)
    frosting, d = canvas()
    d.ellipse((38, 100, 218, 160), fill=FULL)
    d.ellipse((58, 64, 198, 130), fill=FULL)
    d.ellipse((84, 34, 172, 96), fill=FULL)
    d.ellipse((70, 116, 186, 152), fill=SHADE)
    cherry, d = canvas()
    d.ellipse((108, 2, 148, 42), fill=FULL)
    return [(cup, "#59B8FF"), (frosting, "#FF9BD2"), (cherry, "#E0213F")]


def lollipop():
    stick, d = canvas()
    d.rectangle((120, 150, 136, 250), fill=FULL)
    candy, d = canvas()
    d.ellipse((34, 10, 222, 198), fill=FULL)
    for i in range(3):
        r = 30 + i * 28
        d.arc((128 - r, 104 - r, 128 + r, 104 + r), 0 + i * 40, 230 + i * 40, fill=SHADE, width=14)
    return [(stick, "#F2F4F7"), (candy, "#FF5FCB")]


def beer():
    glass, d = canvas()
    d.rounded_rectangle((46, 60, 178, 242), 14, fill=FULL)
    d.arc((150, 96, 232, 196), 270, 90, fill=SHADE, width=20)
    for x in (82, 114, 146):
        d.line((x, 96, x, 222), fill=SHADE, width=8)
    foam, d = canvas()
    for x, y, r in ((62, 58, 30), (102, 44, 34), (144, 50, 32), (176, 66, 22), (110, 76, 30)):
        d.ellipse((x - r, y - r, x + r, y + r), fill=FULL)
    return [(glass, "#F2A93B"), (foam, "#FFF8EC")]


def cocktail():
    glass, d = canvas()
    d.polygon([(20, 40), (236, 40), (128, 150)], fill=FULL)
    d.polygon([(50, 64), (206, 64), (128, 142)], fill=SHADE)
    d.rectangle((120, 146, 136, 220), fill=FULL)
    d.ellipse((70, 212, 186, 240), fill=FULL)
    olive, d = canvas()
    d.line((150, 30, 104, 98), fill=SHADE, width=6)
    d.ellipse((96, 72, 132, 108), fill=FULL)
    return [(glass, "#7FD8FF"), (olive, "#7BC86C")]


def headphones():
    img, d = canvas()
    d.arc((38, 20, 218, 200), 180, 360, fill=FULL, width=24)
    d.rectangle((38, 106, 62, 140), fill=FULL)
    d.rectangle((194, 106, 218, 140), fill=FULL)
    d.rounded_rectangle((22, 120, 86, 230), 24, fill=FULL)
    d.rounded_rectangle((170, 120, 234, 230), 24, fill=FULL)
    d.rounded_rectangle((44, 138, 70, 212), 10, fill=SHADE)
    d.rounded_rectangle((186, 138, 212, 212), 10, fill=SHADE)
    return img


def microphone():
    img, d = canvas()
    d.arc((62, 60, 194, 190), 0, 180, fill=SHADE, width=14)
    d.rectangle((120, 186, 136, 224), fill=SHADE)
    d.rounded_rectangle((78, 218, 178, 240), 8, fill=SHADE)
    d.rounded_rectangle((88, 12, 168, 156), 40, fill=FULL)
    for y in (50, 76, 102):
        d.line((100, y, 156, y), fill=SHADE, width=8)
    return img


def guitar():
    img, d = canvas()
    d.ellipse((60, 142, 196, 250), fill=FULL)
    d.ellipse((78, 94, 178, 182), fill=FULL)
    d.ellipse((108, 150, 148, 190), fill=DEEP)
    d.rectangle((118, 10, 138, 120), fill=SHADE)
    d.rounded_rectangle((108, 0, 148, 34), 6, fill=SHADE)
    d.rectangle((102, 210, 154, 220), fill=DEEP)
    return scaled(img, 0.84, 0.84).rotate(-35, resample=Image.BICUBIC)


def puzzle():
    img, d = canvas()
    d.rectangle((46, 74, 194, 222), fill=FULL)
    d.ellipse((94, 28, 146, 84), fill=FULL)
    d.ellipse((184, 122, 238, 174), fill=FULL)
    d.ellipse((22, 122, 72, 174), fill=0)
    d.ellipse((96, 196, 144, 246), fill=0)
    d.polygon([(194, 74), (194, 222), (46, 222)], fill=SHADE)
    d.ellipse((184, 122, 238, 174), fill=SHADE)
    d.ellipse((96, 196, 144, 246), fill=0)
    return img


def knight():
    img, d = canvas()
    d.rounded_rectangle((52, 208, 204, 242), 10, fill=FULL)
    d.rounded_rectangle((72, 186, 184, 212), 8, fill=SHADE)
    d.polygon([(82, 188), (96, 142), (70, 128), (48, 104), (62, 76), (110, 40), (118, 12), (138, 36), (172, 60), (192, 112), (184, 188)], fill=FULL)
    d.line([(138, 38), (172, 62), (190, 112), (184, 186)], fill=SHADE, width=16)
    d.ellipse((108, 66, 126, 84), fill=DEEP)
    d.line((52, 104, 82, 116), fill=DEEP, width=7)
    return img


def rook():
    img, d = canvas()
    d.rounded_rectangle((52, 208, 204, 242), 10, fill=FULL)
    d.polygon([(82, 210), (98, 96), (158, 96), (174, 210)], fill=FULL)
    d.rectangle((68, 34, 188, 104), fill=FULL)
    for x0 in (95, 141):
        d.rectangle((x0, 34, x0 + 20, 62), fill=0)
    d.rectangle((68, 94, 188, 110), fill=SHADE)
    d.rounded_rectangle((72, 186, 184, 212), 8, fill=SHADE)
    return img


def soccer():
    white, d = canvas()
    d.ellipse((18, 18, 238, 238), fill=FULL)
    dark, d = canvas()
    d.polygon(regular(128, 128, 44, 5), fill=FULL)
    for k in range(5):
        # Edge patches with a corner toward the middle, mostly over the rim
        a = -90 + 36 + 72 * k
        d.polygon(regular(128 + 112 * math.cos(math.radians(a)), 128 + 112 * math.sin(math.radians(a)), 42, 5, rot=a + 180), fill=FULL)
    return [(white, "#F2F4F7"), (clip(dark, white), "#4A5260")]


def basketball():
    img, d = canvas()
    d.ellipse((18, 18, 238, 238), fill=FULL)
    d.line((128, 18, 128, 238), fill=DEEP, width=8)
    d.line((18, 128, 238, 128), fill=DEEP, width=8)
    d.arc((-56, 34, 104, 222), 300, 60, fill=DEEP, width=8)
    d.arc((152, 34, 312, 222), 120, 240, fill=DEEP, width=8)
    ball = Image.new("L", (S, S), 0)
    ImageDraw.Draw(ball).ellipse((18, 18, 238, 238), fill=255)
    return clip(img, ball)


def eightball():
    ball, d = canvas()
    d.ellipse((18, 18, 238, 238), fill=FULL)
    d.ellipse((150, 40, 200, 80), fill=SHADE)
    face, d = canvas()
    d.ellipse((76, 56, 180, 160), fill=FULL)
    eight, d = canvas()
    d.ellipse((110, 70, 146, 104), outline=FULL, width=9)
    d.ellipse((106, 100, 150, 146), outline=FULL, width=9)
    return [(ball, "#4A5260"), (face, "#F2F4F7"), (eight, "#3B4250")]


def sunglasses():
    img, d = canvas()
    d.rectangle((18, 90, 238, 106), fill=FULL)
    d.rounded_rectangle((18, 92, 118, 176), 36, fill=FULL)
    d.rounded_rectangle((138, 92, 238, 176), 36, fill=FULL)
    d.rounded_rectangle((30, 104, 106, 164), 28, fill=SHADE)
    d.rounded_rectangle((150, 104, 226, 164), 28, fill=SHADE)
    d.line((50, 120, 72, 146), fill=FULL, width=9)
    d.line((170, 120, 192, 146), fill=FULL, width=9)
    return img


def tophat():
    img, d = canvas()
    d.ellipse((12, 186, 244, 240), fill=FULL)
    d.rounded_rectangle((60, 26, 196, 214), 12, fill=FULL)
    d.rectangle((60, 160, 196, 192), fill=DEEP)
    d.ellipse((60, 16, 196, 44), fill=SHADE)
    d.rectangle((150, 26, 196, 160), fill=SHADE)
    return img


def mustache():
    img, d = canvas()
    left = [(128, 110), (104, 94), (70, 96), (44, 114), (30, 140), (14, 132), (10, 150), (24, 172), (56, 178), (90, 164), (114, 150), (128, 142)]
    d.polygon(left, fill=FULL)
    d.polygon([(256 - x, y) for x, y in left], fill=SHADE)
    return img


def robot():
    img, d = canvas()
    d.line((128, 22, 128, 56), fill=SHADE, width=10)
    d.ellipse((112, 6, 144, 38), fill=FULL)
    d.rounded_rectangle((16, 108, 46, 172), 10, fill=SHADE)
    d.rounded_rectangle((210, 108, 240, 172), 10, fill=SHADE)
    d.rounded_rectangle((40, 50, 216, 224), 30, fill=FULL)
    d.ellipse((70, 90, 118, 138), fill=DEEP)
    d.ellipse((138, 90, 186, 138), fill=DEEP)
    d.ellipse((86, 106, 102, 122), fill=FULL)
    d.ellipse((154, 106, 170, 122), fill=FULL)
    d.rounded_rectangle((76, 162, 180, 194), 8, fill=DEEP)
    for x in (102, 128, 154):
        d.line((x, 162, x, 194), fill=FULL, width=6)
    return img


def shuriken():
    img, d = canvas()
    pts = poly_star(128, 128, 122, 32, 4, rot=-90)
    d.polygon(pts, fill=FULL)
    for k in range(4):
        d.polygon([(128, 128), pts[2 * k], pts[2 * k + 1]], fill=SHADE)
    d.ellipse((112, 112, 144, 144), fill=0)
    return img


def trident():
    img, d = canvas()
    d.rectangle((118, 100, 138, 250), fill=FULL)
    d.arc((44, 10, 212, 134), 0, 180, fill=FULL, width=20)
    for x in (54, 128, 202):
        d.rectangle((x - 10, 28, x + 10, 76 if x != 128 else 120), fill=FULL)
        d.polygon([(x - 20, 34), (x, 2), (x + 20, 34)], fill=FULL)
    d.rectangle((112, 186, 144, 204), fill=SHADE)
    return img


def chest():
    img, d = canvas()
    d.rounded_rectangle((22, 110, 234, 232), 10, fill=FULL)
    d.chord((22, 34, 234, 186), 180, 360, fill=FULL)
    d.rectangle((22, 104, 234, 122), fill=DEEP)
    for x in (58, 178):
        d.rectangle((x, 40, x + 20, 232), fill=SHADE)
    d.rounded_rectangle((106, 96, 150, 152), 8, fill=SHADE)
    d.ellipse((120, 110, 136, 126), fill=DEEP)
    d.rectangle((125, 122, 131, 140), fill=DEEP)
    return img


def book():
    img, d = canvas()
    d.polygon([(12, 64), (60, 46), (124, 66), (124, 222), (60, 204), (12, 218)], fill=FULL)
    d.polygon([(244, 64), (196, 46), (132, 66), (132, 222), (196, 204), (244, 218)], fill=SHADE)
    d.line((128, 60, 128, 228), fill=DEEP, width=8)
    for y in (96, 124, 152):
        d.line((34, y, 104, y + 10), fill=DEEP, width=6)
        d.line((152, y + 10, 222, y), fill=DEEP, width=6)
    return img


def clock():
    img, d = canvas()
    d.ellipse((18, 18, 238, 238), fill=SHADE)
    d.ellipse((38, 38, 218, 218), fill=FULL)
    for k in range(12):
        a = math.radians(k * 30)
        r0, r1 = (64, 84) if k % 3 else (56, 84)
        d.line((128 + r0 * math.cos(a), 128 + r0 * math.sin(a), 128 + r1 * math.cos(a), 128 + r1 * math.sin(a)), fill=DEEP, width=8 if k % 3 else 12)
    d.line((128, 128, 128, 64), fill=DEEP, width=12)
    d.line((128, 128, 170, 152), fill=DEEP, width=12)
    d.ellipse((116, 116, 140, 140), fill=DEEP)
    return img


def globe():
    img, d = canvas()
    d.ellipse((18, 18, 238, 238), fill=FULL)
    d.ellipse((74, 18, 182, 238), outline=SHADE, width=9)
    d.line((128, 18, 128, 238), fill=SHADE, width=9)
    for y in (74, 128, 182):
        half = math.sqrt(max(0, 110 ** 2 - (y - 128) ** 2))
        d.line((128 - half, y, 128 + half, y), fill=SHADE, width=9)
    return img


def atom():
    img, _ = canvas()
    for a in (0, 60, 120):
        ring = Image.new("L", (S, S), 0)
        ImageDraw.Draw(ring).ellipse((14, 88, 242, 168), outline=FULL, width=14)
        img = ImageChops.lighter(img, ring.rotate(a, resample=Image.BICUBIC))
    d = ImageDraw.Draw(img)
    d.ellipse((102, 102, 154, 154), fill=FULL)
    d.ellipse((114, 110, 130, 126), fill=SHADE)
    return img


def peace():
    img, d = canvas()
    d.ellipse((18, 18, 238, 238), fill=FULL)
    d.ellipse((48, 48, 208, 208), fill=0)
    d.line((128, 30, 128, 226), fill=FULL, width=26)
    d.line((128, 132, 60, 200), fill=FULL, width=26)
    d.line((128, 132, 196, 200), fill=FULL, width=26)
    return img


def yinyang():
    light, d = canvas()
    d.ellipse((18, 18, 238, 238), fill=FULL)
    dark, d = canvas()
    d.pieslice((18, 18, 238, 238), -90, 90, fill=FULL)
    d.ellipse((73, 128, 183, 238), fill=FULL)
    d.ellipse((73, 18, 183, 128), fill=0)
    d.ellipse((112, 57, 144, 89), fill=FULL)
    d.ellipse((112, 167, 144, 199), fill=0)
    return [(light, "#F2F2F2"), (dark, "#56637A")]


def moneybag():
    img, d = canvas()
    d.ellipse((28, 84, 228, 248), fill=FULL)
    d.polygon([(90, 94), (74, 36), (106, 52), (128, 24), (150, 52), (182, 36), (166, 94)], fill=FULL)
    d.rectangle((88, 84, 168, 102), fill=DEEP)
    d.arc((100, 122, 156, 170), 90, 330, fill=DEEP, width=12)
    d.arc((100, 164, 156, 212), -90, 150, fill=DEEP, width=12)
    d.line((128, 112, 128, 224), fill=DEEP, width=8)
    return img


def gift():
    img, d = canvas()
    d.rectangle((32, 108, 224, 238), fill=FULL)
    d.rectangle((20, 78, 236, 114), fill=FULL)
    d.rectangle((150, 78, 236, 238), fill=SHADE)
    d.rectangle((32, 114, 224, 122), fill=DEEP)
    d.rectangle((112, 78, 144, 238), fill=DEEP)
    for box in ((56, 22, 130, 84), (126, 22, 200, 84)):
        d.ellipse(box, fill=SHADE)
    d.ellipse((76, 38, 114, 70), fill=0)
    d.ellipse((142, 38, 180, 70), fill=0)
    return img


def balloon():
    img, d = canvas()
    d.line([(128, 196), (114, 218), (136, 234), (122, 254)], fill=SHADE, width=7)
    d.polygon([(114, 200), (142, 200), (128, 182)], fill=SHADE)
    shape = Image.new("L", (S, S), 0)
    ImageDraw.Draw(shape).ellipse((48, 8, 208, 196), fill=255)
    img.paste(FULL, mask=shape)
    lit = Image.new("L", (S, S), 0)
    ImageDraw.Draw(lit).ellipse((34, -6, 194, 176), fill=255)
    img.paste(SHADE, mask=ImageChops.subtract(shape, lit))
    d.ellipse((84, 40, 108, 82), fill=255)
    return img


def candle():
    wax, d = canvas()
    d.rounded_rectangle((84, 100, 172, 242), 10, fill=FULL)
    d.ellipse((84, 88, 172, 112), fill=SHADE)
    d.ellipse((92, 100, 112, 150), fill=FULL)
    d.rectangle((140, 100, 172, 242), fill=SHADE)
    flame, d = canvas()
    d.ellipse((102, 30, 154, 88), fill=FULL)
    d.polygon([(104, 56), (128, 0), (152, 56)], fill=FULL)
    core, d = canvas()
    d.ellipse((116, 50, 140, 82), fill=FULL)
    wick, d = canvas()
    d.line((128, 76, 128, 98), fill=FULL, width=6)
    return [(wax, "#F2E6D0"), (flame, "#FF8A1E"), (core, "#FFE15A"), (wick, "#3B4250")]


def horseshoe():
    img, d = canvas()
    d.ellipse((28, 18, 228, 218), fill=FULL)
    d.ellipse((76, 66, 180, 170), fill=0)
    d.rectangle((76, 118, 180, 256), fill=0)
    d.rounded_rectangle((24, 170, 82, 238), 10, fill=FULL)
    d.rounded_rectangle((174, 170, 232, 238), 10, fill=FULL)
    for k in range(7):
        a = math.radians(170 + k * 33.3)
        x, y = 128 + 76 * math.cos(a), 118 + 76 * math.sin(a)
        d.ellipse((x - 7, y - 7, x + 7, y + 7), fill=DEEP)
    return img


def rainbow():
    layers = []
    for k, color in enumerate(("#FF4D4D", "#FF9F1C", "#FFE14D", "#5BE36B", "#3DB9FF", "#8B5CFF")):
        band, d = canvas()
        r = 118 - k * 16
        d.ellipse((128 - r, 190 - r, 128 + r, 190 + r), fill=FULL)
        d.ellipse((128 - r + 16, 190 - r + 16, 128 + r - 16, 190 + r - 16), fill=0)
        d.rectangle((0, 190, S, S), fill=0)
        layers.append((band, color))
    cloud, d = canvas()
    for cx in (40, 216):
        for x, y, r in ((cx - 24, 196, 22), (cx, 184, 28), (cx + 24, 198, 20)):
            d.ellipse((x - r, y - r, x + r, y + r), fill=FULL)
        d.rounded_rectangle((cx - 44, 192, cx + 44, 222), 14, fill=FULL)
    layers.append((cloud, "#F2F4F7"))
    return layers


def volcano():
    rock, d = canvas()
    d.polygon([(8, 244), (90, 92), (166, 92), (248, 244)], fill=FULL)
    d.polygon([(128, 92), (166, 92), (248, 244), (150, 244)], fill=SHADE)
    lava, d = canvas()
    d.polygon([(92, 92), (164, 92), (152, 128), (142, 112), (130, 166), (118, 108), (106, 136)], fill=FULL)
    for x, y, r in ((106, 50, 20), (138, 30, 24), (166, 54, 16), (124, 70, 18)):
        d.ellipse((x - r, y - r, x + r, y + r), fill=FULL)
    return [(rock, "#8C6A55"), (lava, "#FF6B35")]


def palm():
    img, d = canvas()
    trail(d, [(112, 248), (124, 196), (138, 142), (140, 96)], 18, 12, SHADE)
    for y in (220, 190, 160, 130):
        d.line((104, y, 150, y - 6), fill=DEEP, width=4)
    for a, length in ((-160, 104), (-120, 96), (-60, 96), (-20, 104), (20, 86), (160, 86)):
        ang = math.radians(a)
        tip = (140 + length * math.cos(ang), 92 + length * math.sin(ang) + 40)
        mid = (140 + length * 0.55 * math.cos(ang), 92 + length * 0.55 * math.sin(ang) - 6)
        d.polygon([(140, 92), (mid[0], mid[1] - 18), tip, (mid[0], mid[1] + 14)], fill=FULL)
    for x, y in ((126, 104), (150, 106)):
        d.ellipse((x - 12, y - 12, x + 12, y + 12), fill=DEEP)
    return img


def snowman():
    img, d = canvas()
    d.ellipse((48, 120, 208, 250), fill=FULL)
    d.ellipse((76, 50, 180, 150), fill=FULL)
    d.rectangle((92, 6, 164, 56), fill=DEEP)
    d.rectangle((74, 48, 182, 62), fill=DEEP)
    d.ellipse((102, 86, 116, 100), fill=DEEP)
    d.ellipse((140, 86, 154, 100), fill=DEEP)
    d.polygon([(124, 106), (124, 122), (170, 118)], fill=SHADE)
    for y in (160, 190, 220):
        d.ellipse((120, y - 8, 136, y + 8), fill=DEEP)
    return img


def sailboat():
    img, d = canvas()
    d.polygon([(22, 184), (234, 184), (198, 232), (58, 232)], fill=SHADE)
    d.rectangle((122, 16, 132, 188), fill=DEEP)
    d.polygon([(138, 22), (138, 172), (224, 172)], fill=FULL)
    d.polygon([(116, 50), (116, 172), (42, 172)], fill=FULL)
    d.polygon([(132, 16), (172, 28), (132, 40)], fill=SHADE)
    return img


def lighthouse():
    img, d = canvas()
    d.polygon([(104, 62), (8, 34), (8, 92)], fill=SHADE)
    d.polygon([(152, 62), (248, 34), (248, 92)], fill=SHADE)
    tower = [(90, 232), (108, 84), (148, 84), (166, 232)]
    d.polygon(tower, fill=FULL)
    bands = Image.new("L", (S, S), 0)
    bd = ImageDraw.Draw(bands)
    for y in (116, 176):
        bd.rectangle((0, y, S, y + 28), fill=255)
    tmask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(tmask).polygon(tower, fill=255)
    img.paste(DEEP, mask=clip(bands, tmask))
    d.rectangle((100, 48, 156, 86), fill=SHADE)
    d.rectangle((112, 56, 144, 80), fill=FULL)
    d.polygon([(92, 50), (128, 12), (164, 50)], fill=FULL)
    d.rectangle((70, 228, 186, 248), fill=SHADE)
    return img


def tent():
    img, d = canvas()
    d.polygon([(10, 228), (128, 34), (246, 228)], fill=FULL)
    d.polygon([(128, 34), (246, 228), (128, 228)], fill=SHADE)
    d.polygon([(98, 228), (128, 118), (158, 228)], fill=DEEP)
    d.line((128, 34, 106, 8), fill=SHADE, width=8)
    d.line((128, 34, 150, 8), fill=SHADE, width=8)
    return img


def campfire_layers(flicker=0.0):
    logs, d = canvas()
    d.line((40, 232, 216, 182), fill=FULL, width=26)
    d.line((40, 182, 216, 232), fill=SHADE, width=26)
    fire, d = canvas()
    h = 1 + 0.08 * math.sin(flicker)
    lean = 10 * math.sin(flicker * 1.7)
    outer = [(128 + lean, 30 + 40 * (1 - h)), (156, 86), (178, 70), (186, 130), (176, 192), (128, 210), (80, 192), (70, 130), (84, 92), (104, 110)]
    d.polygon(outer, fill=FULL)
    core, d = canvas()
    d.polygon([(128 + lean * 0.6, 100 + 30 * (1 - h)), (150, 150), (146, 190), (128, 200), (110, 190), (106, 150)], fill=FULL)
    return [(logs, "#9C6B45"), (fire, "#FF7A1A"), (core, "#FFE14D")]


def campfire():
    return campfire_layers(0.0)


def crystal():
    img, d = canvas()

    def prism(cx, top, bottom, w, tone_left, tone_right):
        d.polygon([(cx, top), (cx - w, top + w * 1.2), (cx - w, bottom), (cx, bottom + 8)], fill=tone_left)
        d.polygon([(cx, top), (cx + w, top + w * 1.2), (cx + w, bottom), (cx, bottom + 8)], fill=tone_right)

    prism(70, 86, 220, 30, FULL, SHADE)
    prism(186, 72, 220, 32, FULL, SHADE)
    prism(128, 10, 228, 42, FULL, SHADE)
    d.rounded_rectangle((30, 222, 226, 246), 10, fill=DEEP)
    return img


# ---------------------------------------------------------------- batch 6: more animals, food, things, symbols, fantasy


def elephant():
    img, d = canvas()
    d.ellipse((4, 46, 106, 176), fill=SHADE)
    d.ellipse((150, 46, 252, 176), fill=SHADE)
    d.ellipse((58, 30, 198, 172), fill=FULL)
    trail(d, [(128, 130), (122, 186), (134, 222), (160, 234)], 28, 14, FULL)
    d.ellipse((88, 88, 108, 108), fill=DEEP)
    d.ellipse((148, 88, 168, 108), fill=DEEP)
    d.arc((92, 140, 112, 190), 90, 270, fill=DEEP, width=7)
    d.arc((144, 140, 164, 190), 270, 90, fill=DEEP, width=7)
    return img


def lion():
    mane, d = canvas()
    d.polygon(poly_star(128, 132, 124, 98, 12), fill=FULL)
    d.ellipse((22, 26, 234, 238), fill=FULL)
    face, d = canvas()
    d.ellipse((56, 46, 98, 88), fill=FULL)
    d.ellipse((158, 46, 200, 88), fill=FULL)
    d.ellipse((60, 60, 196, 212), fill=FULL)
    d.ellipse((96, 140, 160, 200), fill=SHADE)
    features, d = canvas()
    d.ellipse((92, 104, 112, 124), fill=FULL)
    d.ellipse((144, 104, 164, 124), fill=FULL)
    d.polygon([(112, 140), (144, 140), (128, 160)], fill=FULL)
    d.line((128, 158, 128, 174), fill=FULL, width=6)
    d.arc((104, 156, 130, 186), 20, 160, fill=FULL, width=6)
    d.arc((126, 156, 152, 186), 20, 160, fill=FULL, width=6)
    return [(mane, "#D9822B"), (face, "#F5C04F"), (features, "#5A3A22")]


def pig():
    img, d = canvas()
    d.polygon([(42, 40), (98, 62), (60, 108)], fill=SHADE)
    d.polygon([(214, 40), (158, 62), (196, 108)], fill=SHADE)
    d.ellipse((30, 50, 226, 236), fill=FULL)
    d.ellipse((80, 136, 176, 204), fill=SHADE)
    d.ellipse((102, 156, 120, 182), fill=DEEP)
    d.ellipse((136, 156, 154, 182), fill=DEEP)
    d.ellipse((80, 98, 100, 118), fill=DEEP)
    d.ellipse((156, 98, 176, 118), fill=DEEP)
    return img


def cow():
    horns, d = canvas()
    d.polygon([(54, 70), (20, 30), (34, 18), (78, 58)], fill=FULL)
    d.polygon([(202, 70), (236, 30), (222, 18), (178, 58)], fill=FULL)
    head, d = canvas()
    d.ellipse((14, 74, 70, 118), fill=SHADE)
    d.ellipse((186, 74, 242, 118), fill=SHADE)
    d.rounded_rectangle((50, 46, 206, 200), 60, fill=FULL)
    spots, d = canvas()
    d.ellipse((60, 52, 112, 112), fill=FULL)
    d.ellipse((156, 70, 196, 116), fill=FULL)
    d.ellipse((88, 108, 106, 126), fill=FULL)
    d.ellipse((150, 108, 168, 126), fill=FULL)
    muzzle, d = canvas()
    d.ellipse((60, 144, 196, 238), fill=FULL)
    d.ellipse((92, 176, 112, 198), fill=SHADE)
    d.ellipse((144, 176, 164, 198), fill=SHADE)
    return [(horns, "#D9CBB0"), (head, "#F2F2F2"), (spots, "#3B4250"), (muzzle, "#F4A6B8")]


def chick():
    body, d = canvas()
    d.ellipse((40, 52, 216, 236), fill=FULL)
    d.polygon([(118, 58), (112, 20), (128, 42), (140, 14), (142, 58)], fill=FULL)
    d.chord((60, 140, 140, 210), 300, 120, fill=SHADE)
    eyes, d = canvas()
    d.ellipse((94, 108, 112, 126), fill=FULL)
    d.ellipse((150, 108, 168, 126), fill=FULL)
    beak, d = canvas()
    d.polygon([(114, 132), (150, 132), (132, 156)], fill=FULL)
    d.ellipse((84, 228, 120, 248), fill=SHADE)
    d.ellipse((136, 228, 172, 248), fill=SHADE)
    return [(body, "#FFD43B"), (eyes, "#3B3426"), (beak, "#FF8A1E")]


def mouse():
    ears, d = canvas()
    d.ellipse((10, 20, 110, 120), fill=FULL)
    d.ellipse((146, 20, 246, 120), fill=FULL)
    head, d = canvas()
    d.ellipse((44, 66, 212, 230), fill=FULL)
    d.ellipse((76, 150, 180, 226), fill=SHADE)
    pink, d = canvas()
    d.ellipse((30, 40, 90, 100), fill=FULL)
    d.ellipse((166, 40, 226, 100), fill=FULL)
    d.ellipse((112, 160, 144, 186), fill=FULL)
    dark, d = canvas()
    d.ellipse((90, 116, 110, 136), fill=FULL)
    d.ellipse((146, 116, 166, 136), fill=FULL)
    for y, dy in ((172, -8), (186, 6)):
        d.line((96, y, 40, y + dy), fill=FULL, width=4)
        d.line((160, y, 216, y + dy), fill=FULL, width=4)
    return [(ears, "#AEB6C0"), (head, "#AEB6C0"), (pink, "#F4A6B8"), (dark, "#3B4250")]


def monkey():
    fur, d = canvas()
    d.ellipse((8, 88, 76, 156), fill=FULL)
    d.ellipse((180, 88, 248, 156), fill=FULL)
    d.ellipse((44, 24, 212, 236), fill=FULL)
    face, d = canvas()
    d.ellipse((26, 104, 58, 140), fill=FULL)
    d.ellipse((198, 104, 230, 140), fill=FULL)
    d.ellipse((70, 66, 134, 140), fill=FULL)
    d.ellipse((122, 66, 186, 140), fill=FULL)
    d.ellipse((70, 116, 186, 222), fill=FULL)
    dark, d = canvas()
    d.ellipse((92, 92, 112, 112), fill=FULL)
    d.ellipse((144, 92, 164, 112), fill=FULL)
    d.ellipse((112, 140, 122, 152), fill=FULL)
    d.ellipse((134, 140, 144, 152), fill=FULL)
    d.arc((98, 154, 158, 198), 20, 160, fill=FULL, width=7)
    return [(fur, "#9C6B45"), (face, "#E8C39E"), (dark, "#3B2A1E")]


def panda():
    white, d = canvas()
    d.ellipse((30, 40, 226, 232), fill=FULL)
    black, d = canvas()
    d.ellipse((24, 22, 92, 90), fill=FULL)
    d.ellipse((164, 22, 232, 90), fill=FULL)
    for cx, a in ((92, -25), (164, 25)):
        patch = Image.new("L", (S, S), 0)
        ImageDraw.Draw(patch).ellipse((cx - 26, 92, cx + 26, 152), fill=FULL)
        black = ImageChops.lighter(black, patch.rotate(a, resample=Image.BICUBIC, center=(cx, 122)))
    d = ImageDraw.Draw(black)
    d.ellipse((110, 158, 146, 182), fill=FULL)
    d.arc((104, 172, 128, 200), 20, 160, fill=FULL, width=6)
    d.arc((128, 172, 152, 200), 20, 160, fill=FULL, width=6)
    shine, d = canvas()
    d.ellipse((92, 112, 106, 126), fill=FULL)
    d.ellipse((150, 112, 164, 126), fill=FULL)
    return [(white, "#F2F4F7"), (black, "#3B4250"), (shine, "#F2F4F7")]


def unicorn():
    mane, d = canvas()
    for x, y, r in ((60, 70, 34), (44, 114, 32), (46, 160, 30), (60, 202, 26)):
        d.ellipse((x - r, y - r, x + r, y + r), fill=FULL)
    head, d = canvas()
    d.polygon([(84, 60), (100, 26), (118, 62)], fill=FULL)
    d.polygon([(150, 62), (168, 26), (180, 62)], fill=SHADE)
    d.ellipse((70, 44, 206, 196), fill=FULL)
    d.ellipse((100, 150, 220, 238), fill=FULL)
    d.ellipse((164, 196, 178, 210), fill=SHADE)
    d.ellipse((194, 196, 208, 210), fill=SHADE)
    d.arc((112, 104, 144, 128), 200, 340, fill=DEEP, width=7)
    d.arc((158, 104, 190, 128), 200, 340, fill=DEEP, width=7)
    horn, d = canvas()
    d.polygon([(112, 70), (162, 70), (150, 0)], fill=FULL)
    for y in (52, 30):
        d.line((118, y + 10, 158, y - 2), fill=SHADE, width=6)
    return [(mane, "#C77DFF"), (head, "#F4F2F8"), (horn, "#F2C744")]


def dino():
    img, d = canvas()
    trail(d, [(150, 160), (190, 168), (226, 196), (250, 224)], 30, 6, FULL)
    trail(d, [(96, 140), (78, 96), (70, 54)], 22, 18, FULL)
    d.ellipse((50, 20, 116, 72), fill=FULL)
    d.ellipse((66, 110, 196, 210), fill=FULL)
    for x in (84, 160):
        d.rounded_rectangle((x, 180, x + 28, 244), 10, fill=SHADE)
    for x, y in ((112, 112), (138, 108), (164, 116), (188, 130)):
        d.polygon([(x - 12, y + 6), (x, y - 18), (x + 12, y + 6)], fill=SHADE)
    d.ellipse((90, 32, 104, 46), fill=DEEP)
    return img


def parrot():
    red, d = canvas()
    d.ellipse((60, 20, 170, 130), fill=FULL)
    d.ellipse((60, 70, 190, 220), fill=FULL)
    trail(d, [(150, 190), (176, 226), (190, 252)], 18, 8, FULL)
    wing, d = canvas()
    d.chord((96, 100, 216, 230), 280, 100, fill=FULL)
    d.polygon([(150, 200), (200, 210), (176, 250)], fill=SHADE)
    gold, d = canvas()
    d.chord((96, 100, 216, 230), 250, 290, fill=FULL)
    d.ellipse((96, 46, 136, 86), fill=FULL)
    beak, d = canvas()
    d.chord((26, 52, 96, 122), 90, 300, fill=FULL)
    d.ellipse((108, 58, 124, 74), fill=SHADE)
    return [(red, "#E5373E"), (wing, "#3D7BFF"), (gold, "#FFD43B"), (beak, "#3B3426")]


def jellyfish():
    img, d = canvas()
    # All waving the same way, so they never cross
    for i, x in enumerate((70, 99, 128, 157, 186)):
        trail(d, [(x, 120), (x + 12, 160), (x - 8, 200), (x + 10, 244)], 10, 5, SHADE if i % 2 else FULL)
    d.chord((36, 24, 220, 200), 180, 360, fill=FULL)
    d.rectangle((36, 108, 220, 126), fill=FULL)
    for x in range(48, 220, 34):
        d.ellipse((x - 18, 112, x + 18, 140), fill=FULL)
    d.ellipse((88, 60, 110, 82), fill=SHADE)
    return img


def banana():
    img, d = canvas()
    shape = Image.new("L", (S, S), 0)
    sd = ImageDraw.Draw(shape)
    sd.ellipse((10, -40, 250, 220), fill=255)
    sd.ellipse((10, -110, 250, 160), fill=0)
    shape = shape.rotate(-20, resample=Image.BICUBIC).point(lambda v: 255 if v > 128 else 0)
    img.paste(FULL, mask=shape)
    inner = Image.new("L", (S, S), 0)
    ImageDraw.Draw(inner).ellipse((10, -76, 250, 192), fill=255)
    img.paste(SHADE, mask=clip(ImageChops.subtract(shape, inner.rotate(-20, resample=Image.BICUBIC).point(lambda v: 255 if v > 128 else 0)), shape))
    d.ellipse((20, 70, 46, 98), fill=DEEP)
    d.ellipse((208, 188, 236, 214), fill=DEEP)
    return img


def apple():
    fruit, d = canvas()
    d.ellipse((20, 62, 140, 236), fill=FULL)
    d.ellipse((116, 62, 236, 236), fill=FULL)
    d.ellipse((60, 196, 196, 244), fill=FULL)
    d.ellipse((52, 92, 84, 136), fill=SHADE)
    stem, d = canvas()
    d.line((128, 74, 140, 22), fill=FULL, width=12)
    leaf, d = canvas()
    d.polygon([(142, 54), (176, 16), (220, 14), (190, 52)], fill=FULL)
    return [(fruit, "#E5373E"), (stem, "#7A4A2E"), (leaf, "#6CCB5F")]


def strawberry():
    berry, d = canvas()
    d.ellipse((24, 54, 140, 170), fill=FULL)
    d.ellipse((116, 54, 232, 170), fill=FULL)
    d.polygon([(30, 128), (226, 128), (150, 236), (128, 246), (106, 236)], fill=FULL)
    seeds, d = canvas()
    for x, y in ((80, 100), (128, 92), (176, 100), (102, 140), (154, 140), (128, 182), (80, 150), (176, 150), (112, 210), (144, 210)):
        d.ellipse((x - 5, y - 8, x + 5, y + 8), fill=FULL)
    top, d = canvas()
    d.polygon(poly_star(128, 66, 70, 26, 6, rot=-60), fill=FULL)
    d.rectangle((122, 10, 134, 50), fill=FULL)
    return [(berry, "#E5373E"), (seeds, "#FFE9A8"), (top, "#4FAE5A")]


def grapes():
    fruit, d = canvas()
    # Rows of four, three, two and one, the back ones a shade darker
    for y, xs, tone in ((88, (62, 106, 150, 194), SHADE), (130, (84, 128, 172), FULL), (172, (106, 150), SHADE), (212, (128,), FULL)):
        for x in xs:
            d.ellipse((x - 26, y - 26, x + 26, y + 26), fill=tone)
    leaf, d = canvas()
    d.polygon([(140, 52), (176, 14), (226, 20), (196, 56)], fill=FULL)
    d.line((130, 58, 134, 20), fill=SHADE, width=10)
    return [(fruit, "#8B5CFF"), (leaf, "#6CCB5F")]


def carrot():
    root, d = canvas()
    d.polygon([(80, 70), (176, 70), (128, 250)], fill=FULL)
    d.ellipse((80, 52, 176, 92), fill=FULL)
    for y, w in ((110, 26), (150, 20), (190, 14)):
        d.line((128 - w, y, 128 - w + 22, y + 4), fill=SHADE, width=7)
        d.line((128 + w - 18, y + 16, 128 + w + 4, y + 12), fill=SHADE, width=7)
    tops, d = canvas()
    for a in (-30, 0, 30):
        d.polygon(rotate_pts([(118, 60), (128, 0), (138, 60)], 128, 64, a), fill=FULL)
    return turned_layers([(root, "#FF8A1E"), (tops, "#4FAE5A")], -30)


def turned_layers(layers, angle):
    """Several layers turned together about the middle."""
    return [(tones.rotate(angle, resample=Image.BICUBIC), color) for tones, color in layers]


def pineapple():
    fruit, d = canvas()
    d.ellipse((50, 84, 206, 248), fill=FULL)
    grid = Image.new("L", (S, S), 0)
    gd = ImageDraw.Draw(grid)
    for k in range(-6, 7):
        gd.line((k * 32 + 128 - 120, 60, k * 32 + 128 + 120, 260), fill=255, width=7)
        gd.line((k * 32 + 128 + 120, 60, k * 32 + 128 - 120, 260), fill=255, width=7)
    body = Image.new("L", (S, S), 0)
    ImageDraw.Draw(body).ellipse((50, 84, 206, 248), fill=255)
    fruit.paste(SHADE, mask=clip(grid, body))
    crown, d = canvas()
    for a, length in ((-40, 70), (-18, 86), (0, 96), (18, 86), (40, 70)):
        d.polygon(rotate_pts([(116, 100), (128, 100 - length), (140, 100)], 128, 100, a), fill=FULL if a % 36 else SHADE)
    return [(crown, "#4FAE5A"), (fruit, "#F2C744")]


def cheese():
    img, d = canvas()
    d.polygon([(16, 120), (220, 40), (240, 196), (16, 210)], fill=FULL)
    d.polygon([(16, 120), (220, 40), (240, 70), (16, 140)], fill=SHADE)
    for x, y, r in ((70, 168, 16), (130, 140, 12), (180, 172, 20), (110, 192, 10), (206, 120, 10)):
        d.ellipse((x - r, y - r, x + r, y + r), fill=DEEP)
    return img


def taco():
    # Side on: the folded shell is a half disc, the filling heaped along its open top
    meat, d = canvas()
    d.ellipse((30, 86, 226, 140), fill=FULL)
    lettuce, d = canvas()
    d.polygon([(20, 118), (36, 86), (58, 104), (78, 74), (100, 96), (124, 66), (146, 94), (168, 70), (188, 96), (210, 78), (236, 116)], fill=FULL)
    tomato, d = canvas()
    for x, y in ((68, 100), (118, 86), (166, 92), (204, 104)):
        d.ellipse((x - 13, y - 13, x + 13, y + 13), fill=FULL)
    shell, d = canvas()
    d.chord((14, -6, 242, 236), 0, 180, fill=FULL)
    d.chord((14, -6, 242, 236), 120, 180, fill=SHADE)
    for x in (70, 128, 186):
        d.ellipse((x - 7, 160, x + 7, 174), fill=SHADE)
    return [(lettuce, "#6CCB5F"), (meat, "#8C5A3C"), (tomato, "#E5373E"), (shell, "#F2B33D")]


def popcorn():
    corn, d = canvas()
    for x, y, r in ((70, 90, 30), (110, 66, 34), (150, 70, 32), (190, 92, 28), (92, 110, 26), (170, 112, 26), (130, 100, 28)):
        d.ellipse((x - r, y - r, x + r, y + r), fill=FULL)
        d.ellipse((x - r * 0.3, y - r * 0.2, x + r * 0.4, y + r * 0.5), fill=SHADE)
    box, d = canvas()
    d.polygon([(44, 110), (212, 110), (190, 246), (66, 246)], fill=FULL)
    stripes, d = canvas()
    for x in (66, 120, 174):
        d.polygon([(x, 110), (x + 24, 110), (x + 18 - (x - 128) * 0.12, 246), (x - 6 - (x - 128) * 0.12, 246)], fill=FULL)
    return [(corn, "#FFF1C2"), (box, "#F2F4F7"), (clip(stripes, box), "#E5373E")]


def cake():
    sponge, d = canvas()
    d.polygon([(20, 120), (236, 70), (236, 210), (20, 240)], fill=FULL)
    d.polygon([(236, 70), (236, 210), (200, 216), (200, 90)], fill=SHADE)
    cream, d = canvas()
    d.polygon([(20, 170), (236, 136), (236, 150), (20, 186)], fill=FULL)
    frosting, d = canvas()
    d.polygon([(20, 104), (150, 40), (236, 58), (236, 84), (20, 136)], fill=FULL)
    for x, y in ((48, 136), (100, 124), (152, 112), (204, 100)):
        d.ellipse((x - 14, y - 12, x + 14, y + 18), fill=FULL)
    cherry, d = canvas()
    d.ellipse((150, 8, 192, 50), fill=FULL)
    return [(sponge, "#E8C08A"), (cream, "#FFF8EC"), (frosting, "#FF9BD2"), (cherry, "#E0213F")]


def paperplane():
    img, d = canvas()
    d.polygon([(10, 118), (246, 28), (112, 158)], fill=FULL)
    d.polygon([(112, 158), (246, 28), (150, 232)], fill=SHADE)
    d.polygon([(112, 158), (124, 220), (150, 186)], fill=DEEP)
    return img


def kite_layers(tilt=0.0, wave=0.0):
    quads = []
    top, right, bottom, left = (128, 8), (208, 98), (128, 196), (48, 98)
    centre = (128, 98)
    for pts, color in (([top, right, centre], "#FF4D6A"), ([right, bottom, centre], "#3DB9FF"),
                       ([bottom, left, centre], "#FFD43B"), ([left, top, centre], "#5BE36B")):
        layer, d = canvas()
        d.polygon(pts, fill=FULL)
        quads.append((layer, color))
    tail, d = canvas()
    path =[(128 + 18 * math.sin(i * 0.9 + wave), 196 + i * 12) for i in range(6)]
    d.line(path, fill=FULL, width=5)
    for k in (2, 4):
        x, y = path[k]
        d.polygon([(x - 14, y - 8), (x + 14, y + 8), (x + 14, y - 8), (x - 14, y + 8)], fill=FULL)
    return [(t.rotate(tilt, resample=Image.BICUBIC, center=(128, 100)), c) for t, c in quads + [(tail, "#F2F4F7")]]


def kite():
    return kite_layers()


def ring():
    band, d = canvas()
    d.ellipse((36, 88, 220, 252), outline=FULL, width=26)
    d.arc((48, 100, 208, 240), 200, 340, fill=SHADE, width=10)
    stone, d = canvas()
    d.polygon([(88, 40), (168, 40), (196, 70), (128, 132), (60, 70)], fill=FULL)
    d.polygon([(60, 70), (196, 70), (128, 132)], fill=SHADE)
    d.polygon([(108, 40), (128, 70), (148, 40)], fill=SHADE)
    return [(band, "#F2C744"), (stone, "#7FE8FF")]


def palette():
    wood, d = canvas()
    d.ellipse((12, 36, 244, 226), fill=FULL)
    d.ellipse((150, 150, 196, 196), fill=0)
    d.polygon([(170, 226), (244, 160), (256, 256)], fill=0)
    layers = [(wood, "#D9A86C")]
    for (x, y), color in (((70, 92), "#FF4D4D"), ((122, 70), "#FFD43B"), ((176, 86), "#3DB9FF"), ((60, 150), "#5BE36B"), ((104, 184), "#8B5CFF")):
        blob, d = canvas()
        d.ellipse((x - 22, y - 20, x + 22, y + 22), fill=FULL)
        layers.append((blob, color))
    return layers


def pencil():
    body, d = canvas()
    d.rectangle((100, 50, 156, 196), fill=FULL)
    d.rectangle((136, 50, 156, 196), fill=SHADE)
    ferrule, d = canvas()
    d.rectangle((100, 30, 156, 54), fill=FULL)
    eraser, d = canvas()
    d.rounded_rectangle((100, 4, 156, 36), 10, fill=FULL)
    wood, d = canvas()
    d.polygon([(100, 196), (156, 196), (128, 244)], fill=FULL)
    lead, d = canvas()
    d.polygon([(118, 228), (138, 228), (128, 248)], fill=FULL)
    layers = [(body, "#FFC93C"), (ferrule, "#B9C0C6"), (eraser, "#FF8FAB"), (wood, "#E8C08A"), (lead, "#3B4250")]
    return [(scaled(t, 0.92, 0.92).rotate(-45, resample=Image.BICUBIC), c) for t, c in layers]


def scissors():
    img, d = canvas()
    d.polygon([(118, 150), (40, 14), (62, 6), (150, 140)], fill=FULL)
    d.polygon([(138, 150), (216, 14), (194, 6), (106, 140)], fill=SHADE)
    for cx in (78, 178):
        d.ellipse((cx - 44, 150, cx + 44, 238), outline=FULL, width=22)
    d.ellipse((118, 128, 138, 148), fill=DEEP)
    return img


def magnifier():
    img, d = canvas()
    d.line((168, 168, 236, 236), fill=SHADE, width=34)
    d.ellipse((16, 16, 186, 186), fill=FULL)
    d.ellipse((40, 40, 162, 162), fill=SHADE)
    d.arc((56, 56, 146, 146), 200, 260, fill=FULL, width=12)
    return img


def skateboard():
    deck, d = canvas()
    d.polygon([(8, 120), (36, 140), (220, 140), (248, 120), (240, 156), (220, 164), (36, 164), (16, 156)], fill=FULL)
    d.rectangle((36, 140, 220, 148), fill=SHADE)
    wheels, d = canvas()
    for x in (66, 190):
        d.rectangle((x - 16, 164, x + 16, 178), fill=SHADE)
        d.ellipse((x - 24, 172, x + 24, 220), fill=FULL)
        d.ellipse((x - 8, 188, x + 8, 204), fill=SHADE)
    return [(deck, "#FF5FCB"), (wheels, "#F2E6D0")]


def car():
    img, d = canvas()
    d.rounded_rectangle((10, 118, 246, 196), 28, fill=FULL)
    d.polygon([(54, 122), (86, 62), (174, 62), (212, 122)], fill=FULL)
    d.polygon([(74, 118), (96, 76), (124, 76), (124, 118)], fill=SHADE)
    d.polygon([(136, 118), (136, 76), (166, 76), (192, 118)], fill=SHADE)
    for x in (66, 190):
        d.ellipse((x - 34, 160, x + 34, 228), fill=DEEP)
        d.ellipse((x - 14, 180, x + 14, 208), fill=SHADE)
    d.rounded_rectangle((222, 134, 244, 150), 4, fill=SHADE)
    return img


def boxingglove():
    img, d = canvas()
    d.ellipse((40, 14, 220, 190), fill=FULL)
    d.ellipse((14, 92, 92, 176), fill=FULL)
    d.chord((14, 92, 92, 176), 300, 120, fill=SHADE)
    d.rounded_rectangle((70, 170, 196, 246), 12, fill=SHADE)
    for y in (186, 208, 230):
        d.line((104, y, 162, y), fill=DEEP, width=6)
    d.ellipse((70, 40, 106, 90), fill=SHADE)
    return img


def dumbbell():
    img, d = canvas()
    d.rounded_rectangle((60, 116, 196, 140), 8, fill=SHADE)
    for x0, x1 in ((12, 40), (40, 70), (186, 216), (216, 244)):
        tall = 64 if x0 in (40, 186) else 44
        d.rounded_rectangle((x0, 128 - tall, x1, 128 + tall), 8, fill=FULL)
    return img


def retrotv():
    img, d = canvas()
    d.line((128, 66, 78, 10), fill=SHADE, width=8)
    d.line((128, 66, 178, 10), fill=SHADE, width=8)
    d.rounded_rectangle((14, 60, 242, 230), 24, fill=FULL)
    d.rounded_rectangle((34, 82, 182, 210), 26, fill=DEEP)
    d.arc((48, 94, 120, 160), 200, 260, fill=SHADE, width=8)
    d.ellipse((196, 96, 226, 126), fill=SHADE)
    d.ellipse((196, 140, 226, 170), fill=SHADE)
    d.rectangle((50, 230, 80, 246), fill=SHADE)
    d.rectangle((176, 230, 206, 246), fill=SHADE)
    return img


def floppy():
    body, d = canvas()
    d.polygon([(20, 20), (206, 20), (236, 50), (236, 236), (20, 236)], fill=FULL)
    shutter, d = canvas()
    d.rectangle((70, 20, 186, 96), fill=FULL)
    d.rectangle((146, 32, 170, 84), fill=SHADE)
    label, d = canvas()
    d.rounded_rectangle((50, 130, 206, 236), 8, fill=FULL)
    for y in (158, 182, 206):
        d.line((70, y, 186, y), fill=SHADE, width=5)
    return [(body, "#3D7BFF"), (shutter, "#C9D1D8"), (label, "#F2F4F7")]


def cassette():
    img, d = canvas()
    d.rounded_rectangle((10, 46, 246, 210), 16, fill=FULL)
    d.rounded_rectangle((34, 66, 222, 146), 10, fill=SHADE)
    for cx in (86, 170):
        d.ellipse((cx - 22, 84, cx + 22, 128), fill=FULL)
        d.ellipse((cx - 10, 96, cx + 10, 116), fill=DEEP)
    d.rectangle((112, 96, 144, 116), fill=DEEP)
    d.polygon([(62, 210), (80, 166), (176, 166), (194, 210)], fill=SHADE)
    return img


def vinyl_layers(angle=0.0):
    disc, d = canvas()
    d.ellipse((10, 10, 246, 246), fill=FULL)
    for r in (100, 84, 68):
        d.ellipse((128 - r, 128 - r, 128 + r, 128 + r), outline=SHADE, width=4)
    label, d = canvas()
    d.ellipse((86, 86, 170, 170), fill=FULL)
    d.pieslice((86, 86, 170, 170), -30, 30, fill=SHADE)
    d.ellipse((120, 120, 136, 136), fill=0)
    shine, d = canvas()
    d.arc((30, 30, 226, 226), 200, 240, fill=FULL, width=8)
    return [(disc, "#56637A"), (label.rotate(angle, resample=Image.BICUBIC), "#FF4D6A"), (shine, "#8C99AD")]


def vinyl():
    return vinyl_layers()


def queen():
    img, d = canvas()
    d.rounded_rectangle((52, 208, 204, 242), 10, fill=FULL)
    d.rounded_rectangle((72, 186, 184, 212), 8, fill=SHADE)
    d.polygon([(84, 188), (102, 96), (154, 96), (172, 188)], fill=FULL)
    d.polygon([(70, 96), (64, 40), (98, 70), (128, 26), (158, 70), (192, 40), (186, 96)], fill=FULL)
    for x, y in ((64, 34), (128, 18), (192, 34)):
        d.ellipse((x - 12, y - 12, x + 12, y + 12), fill=FULL)
    d.rectangle((70, 92, 186, 106), fill=SHADE)
    return img


def joystick():
    base, d = canvas()
    d.chord((22, 156, 234, 280), 180, 360, fill=FULL)
    d.rounded_rectangle((22, 210, 234, 244), 10, fill=FULL)
    d.rounded_rectangle((22, 222, 234, 244), 10, fill=SHADE)
    stick, d = canvas()
    d.rounded_rectangle((116, 80, 140, 196), 10, fill=FULL)
    red, d = canvas()
    d.ellipse((80, 14, 176, 110), fill=FULL)
    d.ellipse((104, 34, 128, 58), fill=SHADE)
    d.ellipse((176, 176, 210, 200), fill=FULL)
    return [(base, "#56637A"), (stick, "#C9D1D8"), (red, "#E5373E")]


def bowling():
    pin, d = canvas()
    d.ellipse((96, 6, 160, 74), fill=FULL)
    d.rounded_rectangle((108, 60, 148, 110), 14, fill=FULL)
    d.ellipse((70, 96, 186, 250), fill=FULL)
    d.chord((70, 96, 186, 250), 290, 70, fill=SHADE)
    stripes, d = canvas()
    d.rectangle((0, 70, S, 80), fill=FULL)
    d.rectangle((0, 88, S, 96), fill=FULL)
    return [(pin, "#F2F4F7"), (clip(stripes, pin), "#E5373E")]


def check():
    img, d = canvas()
    d.ellipse((14, 14, 242, 242), fill=FULL)
    d.line([(70, 132), (110, 172), (188, 90)], fill=0, width=30, joint="curve")
    for x, y in ((70, 132), (188, 90)):
        d.ellipse((x - 15, y - 15, x + 15, y + 15), fill=0)
    return img


def question():
    img, d = canvas()
    d.ellipse((14, 14, 242, 242), fill=FULL)
    d.arc((82, 50, 174, 138), 180, 45, fill=DEEP, width=24)
    d.line((158, 124, 128, 148), fill=DEEP, width=24)
    d.line((128, 140, 128, 166), fill=DEEP, width=24)
    d.ellipse((112, 180, 144, 212), fill=DEEP)
    return img


def recycle():
    img, d = canvas()
    for k in range(3):
        start = -90 + k * 120 + 14
        d.arc((26, 26, 230, 230), start, start + 84, fill=FULL if k != 1 else SHADE, width=32)
        a = math.radians(start + 84)
        tip = (128 + 102 * math.cos(a + 0.32), 128 + 102 * math.sin(a + 0.32))
        base1 = (128 + 72 * math.cos(a), 128 + 72 * math.sin(a))
        base2 = (128 + 134 * math.cos(a), 128 + 134 * math.sin(a))
        d.polygon([base1, base2, tip], fill=FULL if k != 1 else SHADE)
    return img


def hashtag():
    img, d = canvas()
    for x in (98, 166):
        d.line((x + 10, 20, x - 30, 236), fill=FULL, width=30)
    for y in (88, 160):
        d.line((24, y, 236, y), fill=SHADE if y > 100 else FULL, width=30)
    return img


def heartbeat():
    img = heart()
    d = ImageDraw.Draw(img)
    d.line([(0, 132), (64, 132), (88, 100), (116, 170), (142, 82), (166, 150), (184, 132), (S, 132)], fill=0, width=14, joint="curve")
    return img


def sunflower():
    petals, d = canvas()
    for k in range(14):
        d.polygon(rotate_pts([(116, 36), (128, 4), (140, 36), (128, 76)], 128, 128, k * 360 / 14), fill=FULL if k % 2 else SHADE)
    d.ellipse((42, 42, 214, 214), fill=FULL)
    centre, d = canvas()
    d.ellipse((70, 70, 186, 186), fill=FULL)
    for x in range(86, 180, 18):
        for y in range(86, 180, 18):
            if (x - 128) ** 2 + (y - 128) ** 2 < 46 ** 2:
                d.ellipse((x - 4, y - 4, x + 4, y + 4), fill=SHADE)
    return [(petals, "#FFC93C"), (centre, "#7A4A2E")]


def rose():
    stem, d = canvas()
    d.line((128, 120, 120, 252), fill=FULL, width=12)
    d.polygon([(124, 196), (66, 160), (88, 210)], fill=FULL)
    d.polygon([(124, 176), (190, 150), (168, 200)], fill=SHADE)
    # A cup of petals: two outer ones a shade darker wrapped round a bud, three points along the top
    bloom, d = canvas()
    d.polygon([(54, 40), (88, 62), (128, 26), (168, 62), (202, 40), (198, 120), (128, 166), (58, 120)], fill=FULL)
    d.ellipse((54, 70, 202, 166), fill=FULL)
    d.chord((40, 50, 150, 170), 80, 230, fill=SHADE)
    d.chord((106, 50, 216, 170), 310, 100, fill=SHADE)
    d.arc((96, 56, 160, 110), 200, 340, fill=DEEP, width=8)
    return [(stem, "#4FAE5A"), (bloom, "#E5373E")]


def maple():
    img, d = canvas()
    leaf = [(128, 8), (146, 58), (176, 44), (168, 96), (226, 70), (210, 112), (246, 126), (186, 160), (196, 186),
            (136, 176), (134, 248), (122, 248), (120, 176), (60, 186), (70, 160), (10, 126), (46, 112), (30, 70), (88, 96),
            (80, 44), (110, 58)]
    d.polygon(leaf, fill=FULL)
    d.polygon([p for p in leaf if p[0] >= 128] + [(128, 248)], fill=SHADE)
    for end in ((128, 30), (200, 96), (56, 96)):
        d.line((128, 176, end[0], end[1]), fill=DEEP, width=6)
    return img


def wave_tones(phase=0.0):
    img, d = canvas()
    for k, y in enumerate((70, 128, 186)):
        pts = [(x, y + 16 * math.sin(x / 256 * 4 * math.pi + phase + k * 0.9)) for x in range(-10, 270, 14)]
        trail(d, pts, 15, 15, FULL if k != 1 else SHADE)
    return img


def wave():
    return wave_tones()


def tornado_tones(phase=0.0):
    img, d = canvas()
    for k in range(7):
        y = 26 + k * 32
        w = 116 - k * 14
        x = 128 + 18 * math.sin(phase + k * 0.7) * (k / 6)
        d.ellipse((x - w, y - 18, x + w, y + 18), fill=FULL if k % 2 == 0 else SHADE)
    return img


def tornado():
    return tornado_tones()


def wizardhat():
    hat, d = canvas()
    d.polygon([(52, 200), (112, 40), (150, 10), (196, 22), (150, 54), (204, 200)], fill=FULL)
    d.ellipse((10, 184, 246, 240), fill=FULL)
    d.ellipse((10, 196, 246, 240), fill=SHADE)
    gold, d = canvas()
    d.polygon([(60, 176), (196, 176), (202, 200), (54, 200)], fill=FULL)
    for x, y, r in ((116, 104, 18), (150, 144, 13), (96, 150, 10)):
        d.polygon(poly_star(x, y, r, r * 0.42, 5), fill=FULL)
    return [(hat, "#7141CA"), (gold, "#F2C744")]


def wand_layers(t=0.0):
    stick, d = canvas()
    d.line((40, 226, 150, 116), fill=FULL, width=18)
    d.line((40, 226, 66, 200), fill=SHADE, width=18)
    star, d = canvas()
    d.polygon(poly_star(170, 92, 58, 24, 5, rot=-90 + 12), fill=FULL)
    for (x, y, r), off in (((228, 34, 16), 0), ((96, 46, 13), 1 / 3), ((226, 168, 12), 2 / 3)):
        k = 0.35 + 0.65 * (0.5 + 0.5 * math.cos(2 * math.pi * (t + off)))
        d.polygon(poly_star(x, y, r * k, r * k * 0.28, 4), fill=SHADE)
    return [(stick, "#56637A"), (star, "#FFD43B")]


def wand():
    return wand_layers()


def castle():
    img, d = canvas()
    d.rectangle((40, 110, 216, 244), fill=FULL)
    for x0 in (14, 186):
        d.rectangle((x0, 70, x0 + 56, 244), fill=FULL)
        for k in range(3):
            d.rectangle((x0 + k * 20, 52, x0 + k * 20 + 14, 72), fill=FULL)
        d.rectangle((x0 + 20, 100, x0 + 36, 128), fill=DEEP)
    for k in range(5):
        d.rectangle((76 + k * 22, 94, 90 + k * 22, 112), fill=SHADE)
    d.chord((96, 160, 160, 224), 180, 360, fill=DEEP)
    d.rectangle((96, 192, 160, 244), fill=DEEP)
    d.line((42, 52, 42, 12), fill=SHADE, width=6)
    d.polygon([(44, 12), (76, 22), (44, 32)], fill=SHADE)
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
    # batch 5
    ("dog", dog, "#C99A6B"),
    ("owl", owl, "#A9825C"),
    ("bear", bear, "#9C6B45"),
    ("wolf", wolf, "#9AA8B6"),
    ("frog", frog, "#5BD66A"),
    ("snake", snake, "#4FD6A0"),
    ("spider", spider, "#9B7BFF"),
    ("octopus", octopus, "#FF6B9A"),
    ("turtle", turtle, "#3FAE7B"),
    ("crab", crab, "#FF5A36"),
    ("whale", whale, "#4D9BFF"),
    ("bat", bat, "#8B6CC9"),
    ("bee", bee, "#FFC93C"),
    ("snail", snail, "#D98C5F"),
    ("shark", shark, "#7F9BB5"),
    ("lemon", lemon, "#FFE14D"),
    ("cookie", cookie, "#D9A05B"),
    ("headphones", headphones, "#4DD0E1"),
    ("microphone", microphone, "#C9D1D8"),
    ("guitar", guitar, "#E0A060"),
    ("puzzle", puzzle, "#5AA8FF"),
    ("knight", knight, "#E8E4D8"),
    ("rook", rook, "#8A94A6"),
    ("basketball", basketball, "#FF8A3D"),
    ("sunglasses", sunglasses, "#4A90C2"),
    ("tophat", tophat, "#6C7A8C"),
    ("mustache", mustache, "#8C5A3C"),
    ("robot", robot, "#9AA8B6"),
    ("shuriken", shuriken, "#C9D1D8"),
    ("trident", trident, "#F2C744"),
    ("chest", chest, "#C08050"),
    ("book", book, "#E05A47"),
    ("clock", clock, "#E8E4D8"),
    ("globe", globe, "#3FA9F5"),
    ("atom", atom, "#7CFFB2"),
    ("peace", peace, "#B98CFF"),
    ("moneybag", moneybag, "#7BC86C"),
    ("gift", gift, "#FF5FCB"),
    ("balloon", balloon, "#FF3D6E"),
    ("horseshoe", horseshoe, "#B9C0C6"),
    ("palm", palm, "#4FA35A"),
    ("snowman", snowman, "#EEF4FA"),
    ("sailboat", sailboat, "#F2F4F7"),
    ("lighthouse", lighthouse, "#F2F4F7"),
    ("tent", tent, "#F2A65A"),
    ("crystal", crystal, "#C07CFF"),
    # batch 6
    ("elephant", elephant, "#9AA8B6"),
    ("pig", pig, "#F4A6B8"),
    ("dino", dino, "#6FCF7F"),
    ("jellyfish", jellyfish, "#C77DFF"),
    ("banana", banana, "#FFE14D"),
    ("cheese", cheese, "#FFC93C"),
    ("paperplane", paperplane, "#F2F4F7"),
    ("scissors", scissors, "#C9D1D8"),
    ("magnifier", magnifier, "#8FB8DE"),
    ("car", car, "#E5373E"),
    ("boxing-glove", boxingglove, "#E5373E"),
    ("dumbbell", dumbbell, "#8A94A6"),
    ("retro-tv", retrotv, "#C08050"),
    ("cassette", cassette, "#8C7BFF"),
    ("queen", queen, "#E8E4D8"),
    ("check", check, "#5BE36B"),
    ("question", question, "#3DB9FF"),
    ("recycle", recycle, "#4FD65A"),
    ("hashtag", hashtag, "#4DD0E1"),
    ("heartbeat", heartbeat, "#FF3D6E"),
    ("maple", maple, "#F06A2C"),
    ("wave", wave, "#3FA9F5"),
    ("tornado", tornado, "#A9B4BF"),
    ("castle", castle, "#B9A88A"),
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


def bell_frames(n):
    # Swings from its top, as if rung
    return [scaled(bell(), 0.88, 0.88).rotate(18 * math.sin(2 * math.pi * i / n), resample=Image.BICUBIC, center=(128, 28)) for i in range(n)]


def clock_frames(n):
    # The long hand goes round once
    frames = []
    for i in range(n):
        img, d = canvas()
        d.ellipse((18, 18, 238, 238), fill=SHADE)
        d.ellipse((38, 38, 218, 218), fill=FULL)
        for k in range(12):
            a = math.radians(k * 30)
            r0 = 64 if k % 3 else 56
            d.line((128 + r0 * math.cos(a), 128 + r0 * math.sin(a), 128 + 84 * math.cos(a), 128 + 84 * math.sin(a)), fill=DEEP, width=8 if k % 3 else 12)
        a = math.radians(-90 + 360 * i / n)
        d.line((128, 128, 128 + 66 * math.cos(a), 128 + 66 * math.sin(a)), fill=DEEP, width=10)
        d.line((128, 128, 170, 152), fill=DEEP, width=12)
        d.ellipse((116, 116, 140, 140), fill=DEEP)
        frames.append(img)
    return frames


def atom_frames(n):
    # An electron on each ring, a third of the way apart
    frames = []
    for i in range(n):
        img = atom()
        for k, tilt in enumerate((0, 60, 120)):
            t = 2 * math.pi * (i / n + k / 3)
            x, y = 128 + 114 * math.cos(t), 128 + 40 * math.sin(t)
            dot = Image.new("L", (S, S), 0)
            ImageDraw.Draw(dot).ellipse((x - 21, y - 21, x + 21, y + 21), fill=255)
            dot = dot.rotate(tilt, resample=Image.BICUBIC)
            img.paste(DEEP, mask=dot.point(lambda v: 255 if v > 100 else 0))
        frames.append(img)
    return frames


def shuriken_frames(n):
    # A quarter turn per loop: four blades, so it joins
    return [scaled(shuriken(), 0.9, 0.9).rotate(-90 * i / n, resample=Image.BICUBIC) for i in range(n)]


def balloon_frames(n):
    # Bobs on its string
    frames = []
    for i in range(n):
        a = 2 * math.pi * i / n
        b = scaled(balloon(), 0.86, 0.86)
        b = b.transform(b.size, Image.AFFINE, (1, 0, 0, 0, 1, -12 * math.sin(a)), resample=Image.BICUBIC)
        frames.append(b.rotate(4 * math.sin(a + 0.8), resample=Image.BICUBIC, center=(128, 240)))
    return frames


def bee_frames(n):
    # Wings beat while it hovers
    frames = []
    for i in range(n):
        img, d = canvas()
        wing = 0.45 + 0.55 * abs(math.cos(math.pi * i / 2))
        for x0 in (66, 120):
            d.ellipse((x0, 112 - 94 * wing, x0 + 70, 112), fill=SHADE)
        body = bee()
        mask = Image.new("L", (S, S), 0)
        md = ImageDraw.Draw(mask)
        md.ellipse((26, 88, 212, 222), fill=255)
        md.polygon([(206, 146), (246, 155), (206, 166)], fill=255)
        img.paste(body, mask=mask)
        img = scaled(img, 0.9, 0.9)
        frames.append(img.transform(img.size, Image.AFFINE, (1, 0, 0, 0, 1, -8 * math.sin(2 * math.pi * i / n)), resample=Image.BICUBIC))
    return frames


def moon_frames(n):
    # A month in a loop: the dark side stays faintly there, so the new moon isn't a blank square
    frames = []
    for i in range(n):
        img, d = canvas()
        d.ellipse((24, 24, 232, 232), fill=DEEP)
        # From the full moon, so a still thumbnail (the first frame) shows one
        phase = (i / n + 0.5) % 1
        lit = Image.new("L", (S, S), 0)
        ld = ImageDraw.Draw(lit)
        k = math.cos(2 * math.pi * phase)
        w = abs(k) * 104
        if phase < 0.5:
            ld.pieslice((24, 24, 232, 232), -90, 90, fill=255)
        else:
            ld.pieslice((24, 24, 232, 232), 90, 270, fill=255)
        ld.ellipse((128 - w, 24, 128 + w, 232), fill=0 if k > 0 else 255)
        img.paste(FULL, mask=lit)
        frames.append(img)
    return frames


def heartbeat_frames(n):
    # Beats while the trace runs through it, a whole beat of trace per loop
    beat = [1.0, 0.9, 0.97, 0.88] + [0.86] * 8
    trace = [(0, 132), (64, 132), (88, 100), (116, 170), (142, 82), (166, 150), (184, 132), (256, 132)]
    frames = []
    for i in range(n):
        k = beat[i % len(beat)]
        img = scaled(heart(), k, k)
        d = ImageDraw.Draw(img)
        for o in (-256, 0, 256):
            d.line([(x + o - 256 * i / n, y) for x, y in trace], fill=0, width=14, joint="curve")
        frames.append(img)
    return frames


def wave_frames(n):
    return [wave_tones(2 * math.pi * i / n) for i in range(n)]


def tornado_frames(n):
    return [tornado_tones(2 * math.pi * i / n) for i in range(n)]


def jellyfish_frames(n):
    # Squeezes and stretches as it swims up a little
    frames = []
    for i in range(n):
        a = 2 * math.pi * i / n
        j = scaled(jellyfish(), 0.86 + 0.06 * math.sin(a), 0.86 - 0.06 * math.sin(a))
        frames.append(j.transform(j.size, Image.AFFINE, (1, 0, 0, 0, 1, 10 * math.sin(a)), resample=Image.BICUBIC))
    return frames


def skull_frames(n):
    # The jaw chatters
    frames = []
    for i in range(n):
        # The lower jaw is its own piece, so a dark gap opens under the upper teeth
        drop = 30 * max(0.0, math.sin(4 * math.pi * i / n))
        img, d = canvas()
        d.ellipse((40, 22, 216, 176), fill=FULL)
        d.rounded_rectangle((78, 140, 178, 188), 16, fill=FULL)
        for x in (104, 128, 152):
            d.rectangle((x - 5, 172, x + 5, 188), fill=0)
        d.rounded_rectangle((84, 192 + drop, 172, 224 + drop), 14, fill=SHADE)
        for x in (104, 128, 152):
            d.rectangle((x - 5, 192 + drop, x + 5, 206 + drop), fill=0)
        d.ellipse((68, 84, 120, 136), fill=0)
        d.ellipse((136, 84, 188, 136), fill=0)
        d.polygon([(128, 134), (114, 160), (142, 160)], fill=0)
        frames.append(scaled(img, 0.86, 0.86))
    return frames


def sun_frames(n):
    # A sixth of a turn per loop: the rays alternate two shades, so it joins
    return [sun().rotate(-60 * i / n, resample=Image.BICUBIC) for i in range(n)]


def butterfly_frames(n):
    # Wings close and open twice a loop
    return [scaled(butterfly(), 0.3 + 0.62 * abs(math.cos(2 * math.pi * i / n)), 0.92) for i in range(n)]


def music_frames(n):
    # Hops, tilting as it lands
    frames = []
    for i in range(n):
        a = 2 * math.pi * i / n
        m = scaled(music(), 0.86, 0.86)
        m = m.transform(m.size, Image.AFFINE, (1, 0, 0, 0, 1, 14 * abs(math.sin(a)) - 4), resample=Image.BICUBIC)
        frames.append(m.rotate(8 * math.sin(a), resample=Image.BICUBIC))
    return frames


def signal_frames(n):
    # The bars light up one by one, from full: a still thumbnail (the first frame) shows all four
    frames = []
    for i in range(n):
        img, d = canvas()
        lit = (i + 4) % 5
        for k in range(4):
            h = 50 + k * 50
            d.rounded_rectangle((26 + k * 54, 236 - h, 70 + k * 54, 236), 8, fill=FULL if k < lit else DEEP)
        frames.append(img)
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
    ("animated-bell", bell_frames, 10, "#F5C542"),
    ("animated-clock", clock_frames, 12, "#E8E4D8"),
    ("animated-atom", atom_frames, 12, "#7CFFB2"),
    ("animated-shuriken", shuriken_frames, 8, "#C9D1D8"),
    ("animated-balloon", balloon_frames, 12, "#FF3D6E"),
    ("animated-bee", bee_frames, 8, "#FFC93C"),
    ("animated-moon", moon_frames, 16, "#C9D4FF"),
    # batch 6
    ("animated-heartbeat", heartbeat_frames, 12, "#FF3D6E"),
    ("animated-wave", wave_frames, 12, "#3FA9F5"),
    ("animated-tornado", tornado_frames, 12, "#A9B4BF"),
    ("animated-jellyfish", jellyfish_frames, 12, "#C77DFF"),
    ("animated-skull", skull_frames, 10, "#E8E4D8"),
    ("animated-sun", sun_frames, 10, "#FFB21E"),
    ("animated-butterfly", butterfly_frames, 12, "#59B8FF"),
    ("animated-music", music_frames, 10, "#B98CFF"),
    ("animated-signal", signal_frames, 10, "#5BE36B"),
]

# Animated icons in several colors: (name, frames -> list of layer lists, count)
ANIMATED_MULTI = [
    ("animated-campfire", lambda n: [campfire_layers(2 * math.pi * i / n) for i in range(n)], 8),
    ("animated-wand", lambda n: [wand_layers(i / n) for i in range(n)], 12),
    ("animated-kite", lambda n: [kite_layers(7 * math.sin(2 * math.pi * i / n), 2 * math.pi * i / n) for i in range(n)], 12),
    ("animated-vinyl", lambda n: [vinyl_layers(-360 * i / n) for i in range(n)], 12),
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
    # batch 5
    ("penguin", penguin),
    ("duck", duck),
    ("ladybug", ladybug),
    ("burger", burger),
    ("fries", fries),
    ("watermelon", watermelon),
    ("egg", egg),
    ("cupcake", cupcake),
    ("lollipop", lollipop),
    ("beer", beer),
    ("cocktail", cocktail),
    ("soccer", soccer),
    ("eightball", eightball),
    ("yinyang", yinyang),
    ("candle", candle),
    ("rainbow", rainbow),
    ("volcano", volcano),
    ("campfire", campfire),
    # batch 6
    ("lion", lion),
    ("cow", cow),
    ("chick", chick),
    ("mouse", mouse),
    ("monkey", monkey),
    ("panda", panda),
    ("unicorn", unicorn),
    ("parrot", parrot),
    ("apple", apple),
    ("strawberry", strawberry),
    ("grapes", grapes),
    ("carrot", carrot),
    ("pineapple", pineapple),
    ("taco", taco),
    ("popcorn", popcorn),
    ("cake", cake),
    ("kite", kite),
    ("ring", ring),
    ("palette", palette),
    ("pencil", pencil),
    ("skateboard", skateboard),
    ("floppy", floppy),
    ("vinyl", vinyl),
    ("joystick", joystick),
    ("bowling", bowling),
    ("sunflower", sunflower),
    ("rose", rose),
    ("wizard-hat", wizardhat),
    ("wand", wand),
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

    for name, frames, count in ANIMATED_MULTI:
        layer_frames = frames(count)
        strip = Image.new("RGBA", (OUT * len(layer_frames), OUT), (0, 0, 0, 0))
        for i, layers in enumerate(layer_frames):
            strip.alpha_composite(render_layers(layers), (i * OUT, 0))
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
