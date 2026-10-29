using System.Text.Json;
using System.Text.Json.Nodes;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;

namespace NamePlate;

/// <summary>
/// Name Plates made on the host, for players on other PCs. The editor saves a Name Plate on the PC
/// it runs on; run on a Fika host (a headless one, say), it gives a friend's profile an icon whose
/// Name Plate only the host's icons.json has, and the friend's game showed the default. So the
/// server reads the host's icons.json (the game folder this server sits in) and answers each player
/// with the Name Plate for the icon their profile shows.
///
/// Only when it's clearly meant for them: a new icon (2048+), which nothing but icons.json defines,
/// or an icon the editor saved to that character (assigned-nameplates.json). A host who changed
/// Standard for themselves doesn't hand that to every Standard player. The player's own icons.json
/// wins over this; the plugin only uses it for an icon its own file doesn't define.
/// </summary>
[Injectable(InjectionType.Singleton)]
public class HostNamePlates(ProfileHelper profileHelper, SharedNamePlates shared)
{
    private const string Library = "icons/library/";
    private const int CustomFlags = 0x7FFFF800;

    public const string AssignedFile = "assigned-nameplates.json";

    private static readonly JsonDocumentOptions Lenient = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };

    // SPT 4.x: the server is in SPT_Runtime, the game one folder up
    private static string PluginFolder => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "BepInEx", "plugins", "NamePlate"));

    public record Answer(int Category, JsonNode? NamePlate, string? Picture, string Why);

    public Answer For(MongoId sessionId)
    {
        int selected;
        try
        {
            // SPT throws rather than answer null for a session it has no profile for
            selected = (int)(profileHelper.GetPmcProfile(sessionId)?.Info?.SelectedMemberCategory ?? MemberCategory.Default);
        }
        catch (Exception)
        {
            return new Answer(0, null, null, "no character on this session");
        }

        if ((selected & CustomFlags) == 0 && !Assigned(sessionId.ToString(), selected))
        {
            return new Answer(selected, null, null, "the icon you show isn't one made on the host");
        }

        var iconsJson = Path.Combine(PluginFolder, "icons.json");
        if (!File.Exists(iconsJson))
        {
            return new Answer(selected, null, null, "the host has no icons.json");
        }

        JsonObject? entry;
        try
        {
            entry = (JsonNode.Parse(File.ReadAllText(iconsJson), documentOptions: Lenient)?["icons"] as JsonArray)?
                .OfType<JsonObject>()
                .FirstOrDefault(e => CategoryOf(e["category"]) == selected);
        }
        catch (Exception e) when (e is IOException or JsonException or InvalidOperationException)
        {
            return new Answer(selected, null, null, $"the host's icons.json couldn't be read ({e.Message})");
        }

        if (entry == null)
        {
            return new Answer(selected, null, null, $"the host's icons.json has no Name Plate for icon {selected}");
        }

        entry = (JsonObject)entry.DeepClone();
        entry.Remove("category");

        // A prestige picture is a file on the host; "hide" travels
        if (entry["prestige"] is JsonValue prestige && !string.Equals(prestige.ToString(), "hide", StringComparison.OrdinalIgnoreCase))
        {
            entry.Remove("prestige");
        }

        // Library icons every install has; any other picture comes along as PNG bytes
        string? picture = null;
        if (entry["icon"]?.ToString() is { Length: > 0 } icon && !icon.Replace('\\', '/').StartsWith(Library, StringComparison.OrdinalIgnoreCase))
        {
            entry.Remove("icon");
            var path = Path.GetFullPath(Path.Combine(PluginFolder, icon));
            if (path.StartsWith(PluginFolder, StringComparison.OrdinalIgnoreCase) && File.Exists(path) && new FileInfo(path).Length <= SharedNamePlates.MaxPictureBytes)
            {
                picture = Convert.ToBase64String(File.ReadAllBytes(path));
            }
        }

        return new Answer(selected, entry, picture, "made on the host");
    }

    private static int? CategoryOf(JsonNode? node)
    {
        var text = node?.ToString().Trim();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        if (int.TryParse(text, out var number))
        {
            return number;
        }

        return Enum.TryParse<MemberCategory>(text, true, out var category) ? (int)category : null;
    }

    /// <summary>The editor saved this icon to this character (Save to character on the host).</summary>
    private bool Assigned(string profileId, int selected)
    {
        try
        {
            var path = Path.Combine(shared.ModFolder, AssignedFile);
            if (!File.Exists(path))
            {
                return false;
            }

            var assigned = JsonNode.Parse(File.ReadAllText(path), documentOptions: Lenient)?[profileId];
            return assigned is JsonValue value && value.TryGetValue<int>(out var category) && category == selected;
        }
        catch (Exception e) when (e is IOException or JsonException or InvalidOperationException)
        {
            return false;
        }
    }
}
