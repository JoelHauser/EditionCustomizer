"""Writes src/NamePlate/MedalArt.cs: the prestige medal at 16x16 for the server's startup banner.

    python scripts/make_banner_art.py

Each pixel becomes six hex digits, or "......" where the icon is see-through (a terminal cell is
either drawn or not, so the soft edge is cut at alpha 110).
"""
import os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
SOURCE = os.path.join(HERE, "..", "src", "NamePlate.Client", "icons", "library", "prestige-medal.png")
DEST = os.path.join(HERE, "..", "src", "NamePlate", "MedalArt.cs")
SIZE = 16


def main():
    small = Image.open(SOURCE).convert("RGBA").resize((SIZE, SIZE), Image.LANCZOS)
    rows = []
    for y in range(SIZE):
        row = ""
        for x in range(SIZE):
            r, g, b, a = small.getpixel((x, y))
            row += f"{r:02X}{g:02X}{b:02X}" if a >= 110 else "......"
        rows.append(row)
    body = "\n".join(f'        "{r}",' for r in rows)
    cs = f'''namespace NamePlate;

/// <summary>
/// The library's prestige medal (icons/library/prestige-medal.png) at 16x16 for the startup banner:
/// one row per string, six hex digits per pixel, "......" where it's see-through.
/// Made by scripts/make_banner_art.py; regenerate rather than edit by hand.
/// </summary>
internal static class MedalArt
{{
    public const int Size = {SIZE};

    public static readonly string[] Rows =
    [
{body}
    ];
}}
'''
    open(DEST, "w", encoding="utf-8", newline="\n").write(cs)
    print("->", os.path.normpath(DEST))


if __name__ == "__main__":
    main()
