using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace ChangeIcons.Editor;

/// <summary>
/// The game's icons as the plugin saved them to originals\, and the image work the editor does.
/// </summary>
public class GameIcons
{
    public record Row(string Category, int Value, string? Name, string? Color, int Size);

    private readonly string _folder;
    private readonly Dictionary<string, Row> _rows = new(StringComparer.OrdinalIgnoreCase);

    public GameIcons(string pluginFolder)
    {
        _folder = Path.Combine(pluginFolder, "originals");
        var table = Path.Combine(_folder, "table.json");
        if (System.IO.File.Exists(table))
        {
            try
            {
                var rows = JsonSerializer.Deserialize<Row[]>(System.IO.File.ReadAllText(table), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                foreach (var row in rows ?? [])
                {
                    _rows[row.Category] = row;
                }
            }
            catch (JsonException)
            {
                // Only colors and sizes come from it; the editor works without
            }
        }
    }

    /// <summary>The plugin has run in game and saved the game's icons.</summary>
    public bool Available => Directory.Exists(_folder) && Directory.EnumerateFiles(_folder, "*.png").Any();

    /// <summary>Size to save imported images at: the game's own icon size when known.</summary>
    public int IconSize => _rows.Values.Select(r => r.Size).Where(s => s > 0).DefaultIfEmpty(64).Max();

    public Row? Get(string category) => _rows.GetValueOrDefault(category);

    public string PathOf(string category) => Path.Combine(_folder, category + ".png");

    public Bitmap? Load(string category) => Images.TryLoad(PathOf(category));
}

public static class Images
{
    public static Bitmap? TryLoad(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            // Copy so the file isn't held open (the game reloads it when it changes)
            using var stream = new MemoryStream(File.ReadAllBytes(path));
            using var image = Image.FromStream(stream);
            return new Bitmap(image);
        }
        catch (Exception e) when (e is ArgumentException or OutOfMemoryException or ExternalException)
        {
            return null;
        }
    }

    /// <summary>
    /// Centers an image on a transparent square, scaled down to fit <paramref name="maxSize"/>.
    /// The game stretches icons to a square box, so anything else comes out squashed.
    /// </summary>
    public static Bitmap FitSquare(Image source, int maxSize)
    {
        var side = Math.Min(maxSize, Math.Max(source.Width, source.Height));
        var scale = (float)side / Math.Max(source.Width, source.Height);
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));

        var result = new Bitmap(side, side, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(result);
        g.Clear(Color.Transparent);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.DrawImage(source, (side - width) / 2, (side - height) / 2, width, height);
        return result;
    }

    /// <summary>
    /// Recolors an icon in one color, keeping its shading: each pixel's brightness becomes a shade
    /// of <paramref name="tint"/>, the brightest pixel the full color. Transparency is kept.
    /// </summary>
    public static Bitmap Tint(Bitmap source, Color tint)
    {
        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        var rect = new Rectangle(0, 0, source.Width, source.Height);

        using (var g = Graphics.FromImage(result))
        {
            g.DrawImage(source, rect);
        }

        var data = result.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        try
        {
            var bytes = new byte[data.Stride * data.Height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

            // BGRA
            var brightest = 0.0;
            for (var i = 0; i < bytes.Length; i += 4)
            {
                if (bytes[i + 3] > 0)
                {
                    brightest = Math.Max(brightest, Luminance(bytes, i));
                }
            }

            brightest = Math.Max(brightest, 1);
            for (var i = 0; i < bytes.Length; i += 4)
            {
                var shade = Luminance(bytes, i) / brightest;
                bytes[i] = (byte)Math.Round(tint.B * shade);
                bytes[i + 1] = (byte)Math.Round(tint.G * shade);
                bytes[i + 2] = (byte)Math.Round(tint.R * shade);
            }

            Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
        }
        finally
        {
            result.UnlockBits(data);
        }

        return result;
    }

    private static double Luminance(byte[] bgra, int i) => 0.114 * bgra[i] + 0.587 * bgra[i + 1] + 0.299 * bgra[i + 2];

    public static bool TryParseColor(string? text, out Color color)
    {
        color = Color.White;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var hex = text.Trim().TrimStart('#');
        if ((hex.Length != 6 && hex.Length != 8) || !uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var value))
        {
            return false;
        }

        // #RRGGBB or #RRGGBBAA, the way Unity reads it
        color = hex.Length == 6
            ? Color.FromArgb(255, (int)(value >> 16) & 0xFF, (int)(value >> 8) & 0xFF, (int)value & 0xFF)
            : Color.FromArgb((int)value & 0xFF, (int)(value >> 24) & 0xFF, (int)(value >> 16) & 0xFF, (int)(value >> 8) & 0xFF);
        return true;
    }

    public static string ToHex(Color color) =>
        color.A == 255 ? $"#{color.R:X2}{color.G:X2}{color.B:X2}" : $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
}
