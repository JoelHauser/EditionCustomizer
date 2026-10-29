using System.IO;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NamePlate.Editor;

/// <summary>
/// Reads and writes the icons on an SPT profile: characters.pmc.Info.MemberCategory (which icons
/// you have) and SelectedMemberCategory (the one shown), the same fields the server command sets.
/// </summary>
public class Profiles(string sptPath, string backupFolder)
{
    public record Summary(string Path, string Username, string? Edition, string? Nickname, int MemberCategory, int Selected)
    {
        public bool HasCharacter => Nickname != null;

        public override string ToString() =>
            HasCharacter ? $"{Nickname}  ({Username}, {Edition})" : $"{Username}  (no character yet)";
    }

    public string Folder => System.IO.Path.Combine(sptPath, "SPT_Runtime", "user", "profiles");

    public List<Summary> List()
    {
        var list = new List<Summary>();
        if (!Directory.Exists(Folder))
        {
            return list;
        }

        foreach (var file in Directory.EnumerateFiles(Folder, "*.json"))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                var root = doc.RootElement;
                var info = root.TryGetProperty("info", out var i) ? i : default;
                var pmcInfo = root.TryGetProperty("characters", out var c) && c.TryGetProperty("pmc", out var p) && p.ValueKind == JsonValueKind.Object
                    && p.TryGetProperty("Info", out var pi) && pi.ValueKind == JsonValueKind.Object ? pi : default;

                list.Add(new Summary(
                    file,
                    String(info, "username") ?? System.IO.Path.GetFileNameWithoutExtension(file),
                    String(info, "edition"),
                    String(pmcInfo, "Nickname"),
                    Int(pmcInfo, "MemberCategory"),
                    Int(pmcInfo, "SelectedMemberCategory")));
            }
            catch (Exception e) when (e is JsonException or IOException or FormatException or UnauthorizedAccessException)
            {
                // Not a profile, being written, or with fields other mods changed; leave it out
            }
        }

        return list;
    }

    private static string? String(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int Int(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

    public static bool ServerRunning() => Process.GetProcessesByName("SPT.Server").Length > 0;

    /// <summary>
    /// Writes the two fields and nothing else. The server keeps profiles in memory and saves
    /// over the file, so this refuses while it runs. Backs the file up first.
    /// </summary>
    public string Write(Summary profile, int memberCategory, int selected)
    {
        if (ServerRunning())
        {
            throw new InvalidOperationException("The SPT server is running. It would save over the change, so close it first.");
        }

        var text = File.ReadAllText(profile.Path);
        var root = JsonNode.Parse(text) ?? throw new InvalidDataException("The profile is empty");
        var info = root["characters"]?["pmc"]?["Info"] as JsonObject ?? throw new InvalidDataException("The profile has no character yet");

        // Copy the original before touching it
        // Two saves in the same second get folders of their own
        var stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var backupDir = System.IO.Path.Combine(backupFolder, stamp);
        for (var n = 2; File.Exists(System.IO.Path.Combine(backupDir, System.IO.Path.GetFileName(profile.Path))); n++)
        {
            backupDir = System.IO.Path.Combine(backupFolder, $"{stamp}_{n}");
        }

        Directory.CreateDirectory(backupDir);
        var backup = System.IO.Path.Combine(backupDir, System.IO.Path.GetFileName(profile.Path));
        File.Copy(profile.Path, backup);

        info["MemberCategory"] = memberCategory;
        info["SelectedMemberCategory"] = selected;

        var output = root.ToJsonString(new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

        // Check the result reads back as the original file with just the two fields changed
        var expected = JsonNode.Parse(text)!;
        expected["characters"]!["pmc"]!["Info"]!["MemberCategory"] = memberCategory;
        expected["characters"]!["pmc"]!["Info"]!["SelectedMemberCategory"] = selected;
        if (!JsonNode.DeepEquals(expected, JsonNode.Parse(output)))
        {
            throw new InvalidDataException("The rewritten profile didn't read back the same; nothing was saved");
        }

        Io.ReplaceText(profile.Path, output);
        Assign(root["info"]?["id"]?.GetValue<string>() ?? System.IO.Path.GetFileNameWithoutExtension(profile.Path), selected);
        return backup;
    }

    /// <summary>
    /// Notes in the server mod's assigned-nameplates.json that this character was given this icon
    /// here, so the server hands this PC's Name Plate for it to the player's own game, wherever they
    /// play from (a friend on a Fika host). New icons (2048+) go to whoever wears them anyway.
    /// </summary>
    private void Assign(string profileId, int selected)
    {
        var modFolder = System.IO.Path.Combine(sptPath, "SPT_Runtime", "user", "mods", "NamePlate");
        if (!Directory.Exists(modFolder))
        {
            return;
        }

        var path = System.IO.Path.Combine(modFolder, "assigned-nameplates.json");
        JsonObject assigned;
        try
        {
            assigned = File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? [] : [];
        }
        catch (JsonException)
        {
            assigned = [];
        }

        assigned[profileId] = selected;
        Io.ReplaceText(path, assigned.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}
