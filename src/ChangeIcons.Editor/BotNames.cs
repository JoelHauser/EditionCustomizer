using System.IO;
using System.Text.Json;

namespace ChangeIcons.Editor;

/// <summary>
/// The PMC names bots can get, for the preview: from Bot Callsigns Reloaded and Realistic PMC
/// Names when they're installed (both only change the server's name lists), else SPT's own.
/// </summary>
public static class BotNames
{
    public record Pool(List<string> Names, string Source);

    public static Pool Load(string sptPath)
    {
        var names = new List<string>();
        var sources = new List<string>();
        var mods = Path.Combine(sptPath, "SPT_Runtime", "user", "mods");

        if (Directory.Exists(mods))
        {
            foreach (var mod in Directory.EnumerateDirectories(mods))
            {
                // Bot Callsigns Reloaded: nameData\usec_new.json, bear_new.json {"Names": [...]}
                var nameData = Path.Combine(mod, "nameData");
                if (File.Exists(Path.Combine(nameData, "usec_new.json")))
                {
                    var before = names.Count;
                    names.AddRange(Strings(Path.Combine(nameData, "usec_new.json"), "Names"));
                    names.AddRange(Strings(Path.Combine(nameData, "bear_new.json"), "Names"));
                    names.AddRange(Strings(Path.Combine(nameData, "userDefinedNames.json"), "UsecNames"));
                    names.AddRange(Strings(Path.Combine(nameData, "userDefinedNames.json"), "BearNames"));
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
                    names.AddRange(Strings(Path.Combine(config, "u_names.json"), null));
                    names.AddRange(Strings(Path.Combine(config, "b_names.json"), null));
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
            names.AddRange(Strings(Path.Combine(types, "usec.json"), "firstName"));
            names.AddRange(Strings(Path.Combine(types, "bear.json"), "firstName"));
            sources.Add("SPT's own PMC names");
        }

        var distinct = names.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return new Pool(distinct, string.Join(" + ", sources));
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
        catch (Exception e) when (e is JsonException or IOException)
        {
            return [];
        }
    }
}
