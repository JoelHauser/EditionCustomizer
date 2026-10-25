using System.IO;
using System.Text.Json;

namespace NamePlate.Editor;

/// <summary>
/// The PMC names bots can get, for the preview: from Bot Callsigns Reloaded and Realistic PMC
/// Names when they're installed (both only change the server's name lists), else SPT's own.
/// </summary>
public static class BotNames
{
    /// <param name="Sides">Each name's side as the look generator names it (BotLookGenerator.Bear/Usec), for faction colors.</param>
    public record Pool(List<string> Names, IReadOnlyDictionary<string, string> Sides, string Source);

    private const string Usec = Shared.BotLookGenerator.Usec;
    private const string Bear = Shared.BotLookGenerator.Bear;

    public static Pool Load(string sptPath)
    {
        var names = new List<(string Name, string Side)>();
        var sources = new List<string>();
        var mods = Path.Combine(sptPath, "SPT_Runtime", "user", "mods");

        void Add(string path, string? property, string side) => names.AddRange(Strings(path, property).Select(n => (n, side)));

        if (Directory.Exists(mods))
        {
            foreach (var mod in Directory.EnumerateDirectories(mods))
            {
                // Bot Callsigns Reloaded: nameData\usec_new.json, bear_new.json {"Names": [...]}
                var nameData = Path.Combine(mod, "nameData");
                if (File.Exists(Path.Combine(nameData, "usec_new.json")))
                {
                    var before = names.Count;
                    Add(Path.Combine(nameData, "usec_new.json"), "Names", Usec);
                    Add(Path.Combine(nameData, "bear_new.json"), "Names", Bear);
                    Add(Path.Combine(nameData, "userDefinedNames.json"), "UsecNames", Usec);
                    Add(Path.Combine(nameData, "userDefinedNames.json"), "BearNames", Bear);
                    if (names.Count > before)
                    {
                        sources.Add("Bot Callsigns Reloaded");
                    }
                }

                // Realistic PMC Names: config\u_names.json, b_names.json [...]
                var config = Path.Combine(mod, "config");
                if (File.Exists(Path.Combine(config, "u_names.json")) && File.Exists(Path.Combine(config, "b_names.json")))
                {
                    var before = names.Count;
                    Add(Path.Combine(config, "u_names.json"), null, Usec);
                    Add(Path.Combine(config, "b_names.json"), null, Bear);
                    if (names.Count > before)
                    {
                        sources.Add("Realistic PMC Names");
                    }
                }
            }
        }

        if (names.Count == 0)
        {
            var types = Path.Combine(sptPath, "SPT_Runtime", "SPT_Data", "database", "bots", "types");
            Add(Path.Combine(types, "usec.json"), "firstName", Usec);
            Add(Path.Combine(types, "bear.json"), "firstName", Bear);
            sources.Add("SPT's own PMC names");
        }

        // A name on both sides keeps the first
        var sides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, side) in names.Where(n => !string.IsNullOrWhiteSpace(n.Name)))
        {
            sides.TryAdd(name.Trim(), side);
        }

        return new Pool([.. sides.Keys], sides, string.Join(" + ", sources));
    }

    // A JSON array of strings, at the root or under one property
    private static IEnumerable<string> Strings(string path, string? property)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            var element = doc.RootElement;
            if (property != null && (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out element)))
            {
                return [];
            }

            return element.ValueKind == JsonValueKind.Array
                ? element.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).ToList()
                : [];
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }
}
