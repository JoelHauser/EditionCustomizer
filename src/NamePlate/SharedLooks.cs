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
/// Every player's look, so players on one server (Fika) see each other's names and icons. Each
/// game sends its own look when it changes (<see cref="LookRouter"/>) and asks for everyone's.
///
/// Kept by session: a game can only set its own look, and the nickname is the server's, not one
/// the game sends. The look itself is the plugin's icons.json entry, kept as it came; a picture
/// nobody else has (your own PNG, a recolor) comes along as PNG bytes, kept by hash. Saved to
/// looks.json in the mod folder, so looks are there for players who join before their owner does.
/// </summary>
[Injectable(InjectionType.Singleton)]
public class SharedLooks(ISptLogger<SharedLooks> logger, ProfileHelper profileHelper, ModHelper modHelper)
{
    // A look is a few hundred bytes; an animated icon strip can be a few hundred KB
    public const int MaxLookBytes = 16 * 1024;
    public const int MaxPictureBytes = 512 * 1024;

    public sealed class Stored
    {
        [JsonPropertyName("nickname")] public string Nickname { get; set; } = "";
        [JsonPropertyName("look")] public JsonElement Look { get; set; }
        [JsonPropertyName("picture")] public string? PictureHash { get; set; }
        [JsonPropertyName("updated")] public DateTime Updated { get; set; }
    }

    private sealed class LooksFile
    {
        [JsonPropertyName("looks")] public Dictionary<string, Stored> Looks { get; set; } = [];
        [JsonPropertyName("pictures")] public Dictionary<string, string> Pictures { get; set; } = [];
    }

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly ConcurrentDictionary<string, Stored> _looks = new();

    // Hash -> base64 PNG
    private readonly ConcurrentDictionary<string, string> _pictures = new();
    private readonly object _saveGate = new();
    private string? _path;

    private string PathOnDisk => _path ??= System.IO.Path.Combine(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), "looks.json");

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

            var file = JsonSerializer.Deserialize<LooksFile>(File.ReadAllText(PathOnDisk), Options) ?? new LooksFile();
            foreach (var (id, look) in file.Looks)
            {
                _looks[id] = look;
            }

            foreach (var (hash, png) in file.Pictures)
            {
                _pictures[hash] = png;
            }

            logger.Info($"Name Plate: {_looks.Count} shared look(s) loaded from {PathOnDisk}");
        }
        catch (Exception e)
        {
            logger.Warning($"Name Plate: couldn't read {PathOnDisk}, starting without saved looks: {e.Message}");
        }
    }

    /// <summary>
    /// Sets (or with a null look, clears) the look of the player behind this session. Each one is a
    /// line in the server console: games only send when their look changes, so it stays quiet.
    /// </summary>
    public string Set(MongoId sessionId, JsonElement? look, string? picture)
    {
        EnsureLoaded();
        var before = _looks.TryGetValue(sessionId.ToString(), out var had) ? had.Nickname : null;
        var reply = SetLook(sessionId, look, picture);
        var who = _looks.TryGetValue(sessionId.ToString(), out var now) ? now.Nickname : before ?? $"session {sessionId}";
        var what = look is { ValueKind: JsonValueKind.Object } ? $"look{(string.IsNullOrEmpty(picture) ? "" : " with their own picture")}" : "nothing";
        logger.Info($"Name Plate: {who} shared {what}: {reply}");
        return reply;
    }

    private string SetLook(MongoId sessionId, JsonElement? look, string? picture)
    {
        var id = sessionId.ToString();
        if (look is not { ValueKind: JsonValueKind.Object } given)
        {
            var removed = _looks.TryRemove(id, out _);
            if (removed)
            {
                Save();
            }

            return removed ? "look cleared" : "no look to clear";
        }

        var json = given.GetRawText();
        if (json.Length > MaxLookBytes)
        {
            return "look too big, not kept";
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

        _looks[id] = new Stored
        {
            Nickname = nickname,
            Look = JsonDocument.Parse(json).RootElement.Clone(),
            PictureHash = hash,
            Updated = DateTime.UtcNow,
        };
        Save();
        return "look kept";
    }

    public IEnumerable<(string ProfileId, Stored Look)> All()
    {
        EnsureLoaded();
        return _looks.Select(p => (p.Key, p.Value));
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
                // Pictures nobody's look uses any more go
                var used = _looks.Values.Select(l => l.PictureHash).Where(h => h != null).ToHashSet();
                foreach (var hash in _pictures.Keys.Where(h => !used.Contains(h)).ToList())
                {
                    _pictures.TryRemove(hash, out _);
                }

                var file = new LooksFile { Looks = _looks.ToDictionary(p => p.Key, p => p.Value), Pictures = _pictures.ToDictionary(p => p.Key, p => p.Value) };
                var temp = PathOnDisk + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(file, Options));
                File.Move(temp, PathOnDisk, overwrite: true);
            }
            catch (Exception e)
            {
                logger.Warning($"Name Plate: couldn't save {PathOnDisk}: {e.Message}");
            }
        }
    }
}
