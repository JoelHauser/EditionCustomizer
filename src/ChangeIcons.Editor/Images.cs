using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChangeIcons.Editor;

public static class Images
{
    private static readonly Dictionary<string, BitmapSource?> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Loads without holding the file open (the game reloads it when it changes).</summary>
    public static BitmapSource? Load(string path, bool cached = true)
    {
        if (cached && Cache.TryGetValue(path, out var hit))
        {
            return hit;
        }

        BitmapSource? image = null;
        if (File.Exists(path))
        {
            try
            {
                using var stream = new MemoryStream(File.ReadAllBytes(path));
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                image = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Bgra32, null, 0);
                image.Freeze();
            }
            catch (Exception e) when (e is NotSupportedException or FileFormatException or IOException or ArgumentException)
            {
                image = null;
            }
        }

        Cache[path] = image;
        return image;
    }

    public static void Forget(string path) => Cache.Remove(path);

    public static BitmapSource FromBgra(byte[] pixels, int width, int height)
    {
        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    public static void SavePng(BitmapSource image, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var buffer = new MemoryStream();
        encoder.Save(buffer);
        var bytes = buffer.ToArray();

        // Written in one go rather than through a temp file and a rename: a virus scanner
        // opening each new file can hold it long enough to fail the rename. Retry briefly for
        // the same reason.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                File.WriteAllBytes(path, bytes);
                break;
            }
            catch (IOException) when (attempt < 5)
            {
                Thread.Sleep(100 * attempt);
            }
        }

        Forget(path);
    }

    private static byte[] Pixels(BitmapSource image)
    {
        var bgra = image.Format == PixelFormats.Bgra32 ? image : new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        var pixels = new byte[bgra.PixelWidth * bgra.PixelHeight * 4];
        bgra.CopyPixels(pixels, bgra.PixelWidth * 4, 0);
        return pixels;
    }

    /// <summary>
    /// Centers an image on a transparent square, scaled down to fit <paramref name="maxSize"/>.
    /// The game stretches icons to a square box, so anything else comes out squashed.
    /// </summary>
    public static BitmapSource FitSquare(BitmapSource source, int maxSize)
    {
        var side = Math.Min(maxSize, Math.Max(source.PixelWidth, source.PixelHeight));
        var scale = (double)side / Math.Max(source.PixelWidth, source.PixelHeight);
        var width = source.PixelWidth * scale;
        var height = source.PixelHeight * scale;

        var visual = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);
        using (var dc = visual.RenderOpen())
        {
            dc.DrawImage(source, new Rect((side - width) / 2, (side - height) / 2, width, height));
        }

        var target = new RenderTargetBitmap(side, side, 96, 96, PixelFormats.Pbgra32);
        target.Render(visual);
        var result = new FormatConvertedBitmap(target, PixelFormats.Bgra32, null, 0);
        result.Freeze();
        return result;
    }

    /// <summary>
    /// Recolors an icon in one color, keeping its shading: each pixel's brightness becomes a shade
    /// of <paramref name="tint"/>, the brightest pixel the full color. Transparency is kept.
    /// </summary>
    public static BitmapSource Tint(BitmapSource source, Color tint)
    {
        var pixels = Pixels(source);
        var brightest = 1.0;
        for (var i = 0; i < pixels.Length; i += 4)
        {
            if (pixels[i + 3] > 0)
            {
                brightest = Math.Max(brightest, Luminance(pixels, i));
            }
        }

        for (var i = 0; i < pixels.Length; i += 4)
        {
            var shade = Luminance(pixels, i) / brightest;
            pixels[i] = (byte)Math.Round(tint.B * shade);
            pixels[i + 1] = (byte)Math.Round(tint.G * shade);
            pixels[i + 2] = (byte)Math.Round(tint.R * shade);
        }

        return FromBgra(pixels, source.PixelWidth, source.PixelHeight);
    }

    private static double Luminance(byte[] bgra, int i) => 0.114 * bgra[i] + 0.587 * bgra[i + 1] + 0.299 * bgra[i + 2];

    public static bool TryParseColor(string? text, out Color color)
    {
        color = Colors.White;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var hex = text.Trim().TrimStart('#');
        if ((hex.Length != 6 && hex.Length != 8) || !uint.TryParse(hex, NumberStyles.HexNumber, null, out var value))
        {
            return false;
        }

        // #RRGGBB or #RRGGBBAA, the way Unity reads it
        color = hex.Length == 6
            ? Color.FromArgb(255, (byte)(value >> 16), (byte)(value >> 8), (byte)value)
            : Color.FromArgb((byte)value, (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8));
        return true;
    }

    public static string ToHex(Color color) =>
        color.A == 255 ? $"#{color.R:X2}{color.G:X2}{color.B:X2}" : $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";

    public static Color ParseOr(string? text, Color fallback) => TryParseColor(text, out var c) ? c : fallback;
}
