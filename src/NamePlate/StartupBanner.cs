using System.Text;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using Spectre.Console;

namespace NamePlate;

/// <summary>
/// A small banner when the SPT server finishes loading: the prestige medal drawn in colored half
/// blocks (two pixels per character), with "Name Plate" in a rainbow beside it.
///
/// It goes straight to Spectre.Console's AnsiConsole, the console SPT's own console logger writes
/// to, as one write so nothing lands in the middle of it. Not through ISptLogger, which escapes
/// markup and allows one color per line, so the log file only has SPT's own "Mod: Name Plate ...
/// loaded" line. If drawing fails for any reason, one plain line goes through the logger instead.
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class StartupBanner(ISptLogger<StartupBanner> logger, IReadOnlyList<SptMod> loadedMods) : IOnLoad
{
    private static readonly (byte R, byte G, byte B)[] Rainbow =
    [
        (0xFF, 0x4D, 0x4D), (0xFF, 0x9F, 0x1C), (0xFF, 0xE1, 0x4D), (0x5B, 0xE3, 0x6B),
        (0x3D, 0xB9, 0xFF), (0x8B, 0x5C, 0xFF), (0xFF, 0x5F, 0xCB),
    ];

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var version = new ModMetadata().Version.ToString();
        var pluginFound = ClientPluginFound();
        try
        {
            AnsiConsole.Console.Markup(Banner(version, pluginFound));
        }
        catch (Exception)
        {
            logger.Info($"Name Plate {version} loaded" + (pluginFound ? "" : " (game plugin not found next to this server)"));
        }

        if (CommandName.Taken(loadedMods) is { } other)
        {
            logger.Warning($"Name Plate: {other.Name} is installed too and has 'spt {CommandName.Default}', so Name Plate's chat command is "
                + $"'spt {CommandName.Fallback}'. Name Plate does everything {other.Name} does; you can remove {other.Name}.");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Is the BepInEx plugin in the game folder this server sits in (SPT 4.x: the server is in
    /// SPT_Runtime, the game one folder up)? A server on another machine, a Fika dedicated host for
    /// example, won't find it, and that's fine.
    /// </summary>
    private static bool ClientPluginFound() =>
        File.Exists(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "BepInEx", "plugins", "NamePlate", "NamePlate.Client.dll")));

    private static string Banner(string version, bool pluginFound)
    {
        // Text beside the medal's middle rows
        var beside = new Dictionary<int, string>
        {
            [3] = "   " + Gradient("Name Plate") + $" [grey]{Markup.Escape(version)} loaded[/]",
            [4] = "   [grey]" + (pluginFound ? "colors, tags and icons for your name" : "game plugin not found next to this server") + "[/]",
        };

        var sb = new StringBuilder();
        for (var line = 0; line < MedalArt.Size / 2; line++)
        {
            sb.Append(' ');
            for (var x = 0; x < MedalArt.Size; x++)
            {
                sb.Append(Cell(Pixel(line * 2, x), Pixel(line * 2 + 1, x)));
            }

            if (beside.TryGetValue(line, out var text))
            {
                sb.Append(text);
            }

            sb.Append('\n');
        }

        return sb.ToString();
    }

    private static string? Pixel(int row, int x)
    {
        var hex = MedalArt.Rows[row].Substring(x * 6, 6);
        return hex == "......" ? null : "#" + hex;
    }

    // One character for two pixels: the upper half block in the top pixel's color over the
    // bottom one's, or a lower half block when only the bottom pixel is drawn
    private static string Cell(string? top, string? bottom) => (top, bottom) switch
    {
        (null, null) => " ",
        (null, _) => $"[{bottom}]▄[/]",
        (_, null) => $"[{top}]▀[/]",
        _ => $"[{top} on {bottom}]▀[/]",
    };

    /// <summary>Each letter in its own color along the rainbow; spaces stay plain.</summary>
    private static string Gradient(string text)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == ' ')
            {
                sb.Append(' ');
                continue;
            }

            var t = (double)i / (text.Length - 1) * (Rainbow.Length - 1);
            var k = Math.Min((int)t, Rainbow.Length - 2);
            var f = t - k;
            var (a, b) = (Rainbow[k], Rainbow[k + 1]);
            byte Mix(byte p, byte q) => (byte)Math.Round(p + (q - p) * f);
            sb.Append($"[bold #{Mix(a.R, b.R):X2}{Mix(a.G, b.G):X2}{Mix(a.B, b.B):X2}]").Append(Markup.Escape(text[i].ToString())).Append("[/]");
        }

        return sb.ToString();
    }
}
