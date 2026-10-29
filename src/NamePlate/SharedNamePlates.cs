using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;

namespace NamePlate;

/// <summary>
/// Name Plate sharing: every player's Name Plate, so players on one server (Fika) see each other's
/// names and icons. Each game sends its own when it changes (<see cref="ShareRouter"/>) and asks for
/// everyone's.
///
/// Kept by session: a game can only set its own Name Plate, and the nickname is the server's, not
/// one the game sends. The Name Plate itself is the plugin's icons.json entry, kept as it came; a
/// picture nobody else has (your own PNG, a recolor) comes along as PNG bytes, kept by hash. Saved
/// to shared-nameplates.json in the mod folder, so they're there for players who join before their
/// owner does.
/// </summary>
[Injectable(InjectionType.Singleton)]
public class SharedNamePlates(ISptLogger<SharedNamePlates> logger, ProfileHelper profileHelper, ModHelper modHelper)
{
    // A Name Plate is a few hundred bytes; an animated icon strip can be a few hundred KB
    public const int MaxNamePlateBytes = 16 * 1024;
    public const int MaxPictureBytes = 512 * 1024;

    public sealed class Stored
    {
        [JsonPropertyName("nickname")] public string Nickname { get; set; } = "";
        [JsonPropertyName("nameplate")] public JsonElement NamePlate { get; set; }
        [JsonPropertyName("picture")] public string? PictureHash { get; set; }
        [JsonPropertyName("updated")] public DateTime Updated { get; set; }
    }

    private sealed class SharedFile
    {
        [JsonPropertyName("nameplates")] public Dictionary<string, Stored> NamePlates { get; set; } = [];
        [JsonPropertyName("pictures")] public Dictionary<string, string> Pictures { get; set; } = [];
    }

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly ConcurrentDictionary<string, Stored> _shared = new();

    // Hash -> base64 PNG
    private readonly ConcurrentDictionary<string, string> _pictures = new();
    private readonly object _saveGate = new();
    private string? _path;

    public string ModFolder => modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

    private string PathOnDisk => _path ??= System.IO.Path.Combine(ModFolder, "shared-nameplates.json");

    private bool _loaded;

    // On first use rather than at startup: the routes are the only way in
    private void EnsureLoaded()
    {
        lock (_saveGate)
        {
            if (!_loaded)
            {
                _loaded = true;
                Load();
            }
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(PathOnDisk))
            {
                return;
            }

            var file = JsonSerializer.Deserialize<SharedFile>(File.ReadAllText(PathOnDisk), Options) ?? new SharedFile();
            foreach (var (id, plate) in file.NamePlates)
            {
                _shared[id] = plate;
            }

            foreach (var (hash, png) in file.Pictures)
            {
                _pictures[hash] = png;
            }

            logger.Info($"Name Plate sharing: {_shared.Count} shared Name Plate(s) loaded from {PathOnDisk}");
        }
        catch (Exception e)
        {
            logger.Warning($"Name Plate sharing: couldn't read {PathOnDisk}, starting without saved Name Plates: {e.Message}");
        }
    }

    /// <summary>
    /// Sets (or with null, clears) the Name Plate of the player behind this session. Each one is a
    /// line in the server console: games only send when their Name Plate changes, so it stays quiet.
    /// </summary>
    public string Set(MongoId sessionId, JsonElement? namePlate, string? picture)
    {
        EnsureLoaded();
        var before = _shared.TryGetValue(sessionId.ToString(), out var had) ? had.Nickname : null;
        var reply = SetNamePlate(sessionId, namePlate, picture);
        var who = _shared.TryGetValue(sessionId.ToString(), out var now) ? now.Nickname : before ?? $"session {sessionId}";
        var what = namePlate is { ValueKind: JsonValueKind.Object }
            ? $"their Name Plate{(string.IsNullOrEmpty(picture) ? "" : " with their own picture")}"
            : "nothing";
        logger.Info($"Name Plate sharing: {who} sent {what}: {reply}");
        return reply;
    }

    private string SetNamePlate(MongoId sessionId, JsonElement? namePlate, string? picture)
    {
        var id = sessionId.ToString();
        if (namePlate is not { ValueKind: JsonValueKind.Object } given)
        {
            var removed = _shared.TryRemove(id, out _);
            if (removed)
            {
                Save();
            }

            return removed ? "Name Plate no longer shared" : "nothing to stop sharing";
        }

        var json = given.GetRawText();
        if (json.Length > MaxNamePlateBytes)
        {
            return "Name Plate too big, not kept";
        }

        // SPT throws rather than answer null for a session it has no profile for
        string? nickname;
        try
        {
            nickname = profileHelper.GetPmcProfile(sessionId)?.Info?.Nickname;
        }
        catch (Exception)
        {
            nickname = null;
        }

        if (string.IsNullOrEmpty(nickname))
        {
            return "no character on this session, not kept";
        }

        string? hash = null;
        if (!string.IsNullOrEmpty(picture))
        {
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(picture);
            }
            catch (FormatException)
            {
                return "picture isn't base64, not kept";
            }

            if (bytes.Length > MaxPictureBytes)
            {
                return $"picture over {MaxPictureBytes / 1024} KB, not kept";
            }

            hash = Convert.ToHexString(SHA1.HashData(bytes)).ToLowerInvariant();
            _pictures[hash] = picture;
        }

        _shared[id] = new Stored
        {
            Nickname = nickname,
            NamePlate = JsonDocument.Parse(json).RootElement.Clone(),
            PictureHash = hash,
            Updated = DateTime.UtcNow,
        };
        Save();
        return "Name Plate shared";
    }

    public IEnumerable<(string ProfileId, Stored Shared)> All()
    {
        EnsureLoaded();
        return _shared.Select(p => (p.Key, p.Value));
    }

    public string? Picture(string? hash)
    {
        EnsureLoaded();
        return hash != null && _pictures.TryGetValue(hash, out var png) ? png : null;
    }

    private void Save()
    {
        lock (_saveGate)
        {
            try
            {
                // Pictures no shared Name Plate uses any more go
                var used = _shared.Values.Select(s => s.PictureHash).Where(h => h != null).ToHashSet();
                foreach (var hash in _pictures.Keys.Where(h => !used.Contains(h)).ToList())
                {
                    _pictures.TryRemove(hash, out _);
                }

                var file = new SharedFile { NamePlates = _shared.ToDictionary(p => p.Key, p => p.Value), Pictures = _pictures.ToDictionary(p => p.Key, p => p.Value) };
                var temp = PathOnDisk + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(file, Options));
                File.Move(temp, PathOnDisk, overwrite: true);
            }
            catch (Exception e)
            {
                logger.Warning($"Name Plate sharing: couldn't save {PathOnDisk}: {e.Message}");
            }
        }
    }
}
