using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EFT;
using EFT.UI;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace NamePlate.Client;

/// <summary>
/// Name Plate sharing between players on one server (Fika): yours goes to the server mod whenever
/// it changes, everyone else's comes back, and they're drawn wherever the game draws that player
/// (lobby and group lists, chat, invites, the death screen, the kill list, dogtags, the flea).
///
/// Requests go through SPT's own RequestHandler, found by name, so they reach whichever server
/// the game is connected to with its session. Answers arrive on a worker thread and are handed to
/// the main thread, where sprites can be made. A server without the routes (an older Name Plate)
/// costs one log line, and players keep the game's own name and icon for each other as before.
/// </summary>
public static class SharedNamePlates
{
    private const float CheckEvery = 5f;
    private const float FetchEvery = 60f;
    private const int GiveUpAfter = 3;
    private const string Library = "icons/library/";
    private const string PictureFolder = "shared";

    private static readonly Regex Hash = new("^[0-9a-f]{40}$");

    private sealed class Remote
    {
        public string ProfileId;
        public string Nickname;
        public string Raw;
        public IconEntry Entry;
        public string Picture;
        public BotLooks.Look Drawn;
    }

    private static readonly ConcurrentQueue<Action> MainThread = new();
    private static readonly Dictionary<string, Remote> ById = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Remote> ByName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> Fetching = new();

    private static MethodInfo _postAsync;
    private static bool _resolved;
    private static int _failures;
    private static bool _off;
    private static float _nextCheck;
    private static float _nextFetch;
    private static bool _fetchBusy;
    private static bool _sendBusy;
    private static string _sent;
    private static string _ownId;

    // ---- What the log says, so testers' logs show what was sent, received and drawn. Each line is
    // written once per change, never per frame or per screen.
    private const string Tag = "[Name Plate sharing] ";
    private static bool _announced;
    private static string _lastList;
    private static readonly Dictionary<string, string> Met = new(StringComparer.Ordinal);
    private static readonly HashSet<string> MetReported = new(StringComparer.Ordinal);

    /// <summary>The Name Plate another player shares, or null for the game's own.</summary>
    public static BotLooks.Look For(string profileId, string nickname)
    {
        var remote = Find(profileId, nickname);
        if (remote == null)
        {
            return null;
        }

        try
        {
            if (remote.Drawn == null)
            {
                remote.Drawn = Build(remote);
                Plugin.Log.LogInfo($"{Tag}Drawing {remote.Nickname}'s Name Plate ({Describe(remote.Entry, remote.Picture != null)})"
                    + (remote.Picture != null && remote.Drawn.Icon == null ? "; their picture hasn't arrived yet, the icon follows" : ""));
            }

            return remote.Drawn;
        }
        catch (Exception e)
        {
            Safe.Report("building a shared Name Plate", e);
            return null;
        }
    }

    /// <summary>Someone new is about (a raid started): ask for Name Plates soon rather than in a minute.</summary>
    public static void FetchSoon() => _nextFetch = Mathf.Min(_nextFetch, Time.unscaledTime + 2f);

    /// <summary>
    /// A real player joined your raid. After the next fetch the log says whether they share their
    /// Name Plate, the first thing to know when someone reports not seeing a friend's.
    /// </summary>
    public static void MetInRaid(string profileId, string nickname)
    {
        if (string.IsNullOrEmpty(profileId) || profileId == _ownId || LocalPlayer.IsYou(nickname) || MetReported.Contains(profileId))
        {
            return;
        }

        Met[profileId] = nickname;
        FetchSoon();
    }

    // "3 colors, wave, tag [EZ], owl icon" -- enough to compare with what the other player set
    private static string Describe(IconEntry e, bool ownPicture)
    {
        var parts = new List<string>
        {
            e.Colors is { Count: > 1 } ? $"{e.Colors.Count} colors" : e.Color ?? "the game's color",
        };
        if (e.Animate > 0)
        {
            parts.Add(string.IsNullOrEmpty(e.Motion) ? "scroll" : e.Motion);
        }

        if (!string.IsNullOrEmpty(e.Tag))
        {
            parts.Add($"tag [{e.Tag}]" + (string.IsNullOrEmpty(e.TagColor) ? "" : $" in {e.TagColor}"));
        }

        parts.Add(ownPicture ? "their own picture"
            : !string.IsNullOrEmpty(e.Icon) ? Path.GetFileNameWithoutExtension(e.Icon) + " icon"
            : !string.IsNullOrEmpty(e.IconFrom) ? $"the game's {e.IconFrom} icon"
            : "no icon");
        return string.Join(", ", parts);
    }

    private static Remote Find(string profileId, string nickname)
    {
        if (!string.IsNullOrEmpty(profileId) && ById.TryGetValue(profileId, out var byId))
        {
            return byId;
        }

        return !string.IsNullOrEmpty(nickname) && ByName.TryGetValue(nickname, out var byName) ? byName : null;
    }

    /// <summary>From the plugin's Update, on the main thread.</summary>
    public static void Tick()
    {
        while (MainThread.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Safe.Report("handling a shared Name Plate", e);
            }
        }

        var now = Time.unscaledTime;
        if (_off || now < _nextCheck)
        {
            return;
        }

        _nextCheck = now + CheckEvery;
        var session = TarkovApplication.Exist(out var app) ? app.GetClientBackEndSession() : null;
        if (session?.Profile == null || !Resolve())
        {
            return;
        }

        _ownId = session.Profile.Id;
        if (!_announced)
        {
            _announced = true;
            Plugin.Log.LogInfo($"{Tag}Name Plate {ModVersion.Value}: your Name Plate is "
                + (Plugin.Settings.ShareNamePlate ? "shared with other players on this server" : "not shared (switched off on the editor's Profile page)")
                + $"; other players' Name Plates are checked every {FetchEvery:0} s. Lines starting [Name Plate sharing] are what to send with a bug report.");
        }

        SendOwn(session.Profile);
        if (now >= _nextFetch && !_fetchBusy)
        {
            _nextFetch = now + FetchEvery;
            FetchAll();
        }
    }

    // ---------------------------------------------------------------- yours

    private static void SendOwn(Profile profile)
    {
        if (_sendBusy)
        {
            return;
        }

        var (namePlate, picturePath, signature) = OwnNamePlate(profile);
        if (signature == _sent)
        {
            return;
        }

        var bytes = picturePath == null ? null : File.ReadAllBytes(picturePath);
        var body = new JObject { ["nameplate"] = namePlate, ["picture"] = bytes == null ? null : Convert.ToBase64String(bytes) };
        var what = namePlate is JObject sent
            ? $"your Name Plate ({Describe(sent.ToObject<IconEntry>(), bytes != null)}{(bytes != null ? $", {bytes.Length / 1024 + 1} KB" : "")})"
            : "nothing (the icon you show is the game's own" + (Plugin.Settings.ShareNamePlate ? ")" : ", or sharing is off)");
        _sendBusy = true;
        Post("/nameplate/share", body.ToString(Formatting.None), "message", reply =>
        {
            _sendBusy = false;
            if (reply != null)
            {
                _sent = signature;
                Plugin.Log.LogInfo($"{Tag}Sent {what}; the server says: {(string)reply["message"]}");
            }
        });
    }

    /// <summary>
    /// The icons.json entry for the icon you show, as other players need it: your own picture
    /// comes along as PNG bytes, the color the game gives the icon is filled in, and things only
    /// this PC has (the dropdown name, a prestige picture) are left out.
    /// </summary>
    private static (JToken NamePlate, string PicturePath, string Signature) OwnNamePlate(Profile profile)
    {
        var settings = EFTHardSettings.Instance.ChatSpecialIconSettings;
        var row = settings.GetDataByMemberCategory(profile.Info.SelectedMemberCategory);
        var entry = row == null || !Plugin.Settings.ShareNamePlate ? null : IconTable.EntryFor(row.Category);
        if (entry == null)
        {
            return (JValue.CreateNull(), null, "none");
        }

        var copy = JsonConvert.DeserializeObject<IconEntry>(JsonConvert.SerializeObject(entry));
        copy.Name = null;
        copy.Prestige = null;
        copy.Color ??= "#" + ColorUtility.ToHtmlStringRGB(row.IconColor);

        string picture = null;
        var pictureTime = "";
        if (!string.IsNullOrEmpty(copy.Icon) && !copy.Icon.Replace('\\', '/').StartsWith(Library, StringComparison.OrdinalIgnoreCase))
        {
            var path = Path.Combine(Plugin.Folder, copy.Icon);
            if (File.Exists(path))
            {
                picture = path;
                pictureTime = path + File.GetLastWriteTimeUtc(path).Ticks;
            }

            copy.Icon = null;
        }

        // Without a picture of its own, the icon is the game's for that category
        if (string.IsNullOrEmpty(copy.Icon) && string.IsNullOrEmpty(copy.IconFrom) && picture == null)
        {
            copy.IconFrom = IconTable.IsCustom(row.Category) ? nameof(EMemberCategory.Default) : row.Category.ToString();
        }

        var namePlate = JObject.Parse(JsonConvert.SerializeObject(copy, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
        }));
        return (namePlate, picture, namePlate.ToString(Formatting.None) + "|" + pictureTime);
    }

    // ---------------------------------------------------------------- theirs

    private static void FetchAll()
    {
        _fetchBusy = true;
        Post("/nameplate/shared", "{}", "nameplates", reply =>
        {
            _fetchBusy = false;
            if (reply != null)
            {
                Take(reply["nameplates"] as JArray);
            }
        });
    }

    private static void Take(JArray shared)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in shared ?? new JArray())
        {
            var id = (string)item["profileId"];
            var nickname = (string)item["nickname"];
            if (string.IsNullOrEmpty(id) || id == _ownId || item["nameplate"] is not JObject namePlate)
            {
                continue;
            }

            seen.Add(id);
            var raw = namePlate.ToString(Formatting.None);
            var picture = (string)item["picture"];
            if (ById.TryGetValue(id, out var known) && known.Raw == raw && known.Picture == picture && known.Nickname == nickname)
            {
                continue;
            }

            var remote = new Remote
            {
                ProfileId = id, Nickname = nickname, Raw = raw, Picture = picture != null && Hash.IsMatch(picture) ? picture : null,
                Entry = namePlate.ToObject<IconEntry>(),
            };
            Forget(id);
            ById[id] = remote;
            if (!string.IsNullOrEmpty(nickname))
            {
                ByName[nickname] = remote;
            }

            FetchPicture(remote);
        }

        foreach (var gone in new List<string>(ById.Keys))
        {
            if (!seen.Contains(gone))
            {
                Forget(gone);
            }
        }

        // Whose Name Plates the server has, said again only when that changes
        var names = new List<string>();
        foreach (var r in ById.Values)
        {
            names.Add(string.IsNullOrEmpty(r.Nickname) ? r.ProfileId : r.Nickname);
        }

        names.Sort(StringComparer.OrdinalIgnoreCase);
        var list = string.Join(", ", names.ToArray());
        if (list != _lastList)
        {
            _lastList = list;
            Plugin.Log.LogInfo(names.Count == 0
                ? $"{Tag}The server has no Name Plates from other players yet"
                : $"{Tag}The server has Name Plates from {names.Count} other player(s): {list}");
        }

        // Players met in a raid since the last fetch: do they share their Name Plate?
        foreach (var met in Met)
        {
            MetReported.Add(met.Key);
            Plugin.Log.LogInfo(Find(met.Key, met.Value) != null
                ? $"{Tag}{met.Value} is in your raid and shares their Name Plate"
                : $"{Tag}{met.Value} is in your raid but doesn't share a Name Plate: they need Name Plate {ModVersion.Value} or later with sharing on, "
                  + "and this server needs the Name Plate server mod");
        }

        Met.Clear();
    }

    private static void Forget(string id)
    {
        if (ById.TryGetValue(id, out var old))
        {
            ById.Remove(id);
            if (old.Nickname != null && ByName.TryGetValue(old.Nickname, out var named) && named == old)
            {
                ByName.Remove(old.Nickname);
            }
        }
    }

    private static string PicturePath(string hash) => PictureFolder + "/" + hash + ".png";

    // Pictures are kept by hash in shared\, so each is only ever fetched once
    private static void FetchPicture(Remote remote)
    {
        var hash = remote.Picture;
        if (hash == null || File.Exists(Path.Combine(Plugin.Folder, PicturePath(hash))) || !Fetching.Add(hash))
        {
            return;
        }

        // No expected field: the server leaves "picture" out for a hash it doesn't have
        Post("/nameplate/picture", new JObject { ["hash"] = hash }.ToString(Formatting.None), null, reply =>
        {
            Fetching.Remove(hash);
            var png = (string)reply?["picture"];
            if (png == null)
            {
                if (reply != null)
                {
                    Plugin.Log.LogWarning($"{Tag}{remote.Nickname}'s picture isn't on the server; their Name Plate shows without it");
                }

                return;
            }

            var bytes = Convert.FromBase64String(png);
            Directory.CreateDirectory(Path.Combine(Plugin.Folder, PictureFolder));
            File.WriteAllBytes(Path.Combine(Plugin.Folder, PicturePath(hash)), bytes);
            Plugin.Log.LogInfo($"{Tag}Got {remote.Nickname}'s picture ({bytes.Length / 1024 + 1} KB)");

            // Build again with the picture
            foreach (var r in ById.Values)
            {
                if (r.Picture == hash)
                {
                    r.Drawn = null;
                }
            }
        });
    }

    /// <summary>A shared entry, drawn the way the plugin draws your own Name Plate.</summary>
    private static BotLooks.Look Build(Remote remote)
    {
        var entry = remote.Entry;
        var color = ColorUtility.TryParseHtmlString(entry.Color, out var c) ? c : new Color(0.765f, 0.804f, 0.827f);
        var tagged = NameStyle.From(entry, color) ?? NameStyle.Create(new[] { color }, false, 0);

        Sprite icon = null;
        if (remote.Picture != null)
        {
            var path = PicturePath(remote.Picture);
            icon = File.Exists(Path.Combine(Plugin.Folder, path)) ? IconTable.FileSprite(path) : null;
        }
        else if (!string.IsNullOrEmpty(entry.Icon) && File.Exists(Path.Combine(Plugin.Folder, entry.Icon)))
        {
            icon = IconTable.FileSprite(entry.Icon);
        }
        else if (!string.IsNullOrEmpty(entry.IconFrom) && Enum.TryParse(entry.IconFrom, true, out EMemberCategory from))
        {
            icon = IconTable.GameSprite(from);
        }

        var frames = IconTable.FramesOf(icon);
        var effect = string.IsNullOrWhiteSpace(entry.IconEffect) ? null : entry.IconEffect.Trim().ToLowerInvariant();
        return new BotLooks.Look
        {
            Style = new NameStyle { Colors = tagged.Colors, Paint = tagged.Paint },
            Tagged = tagged,
            Icon = icon,
            IconMotion = frames == null && effect == null ? null
                : new IconTable.IconMotion { Frames = frames, Fps = entry.Fps > 0 ? entry.Fps : 8f, Effect = effect, Tint = tagged.Colors[0] },
        };
    }

    // ---------------------------------------------------------------- the server

    private static bool Resolve()
    {
        if (!_resolved)
        {
            _resolved = true;
            var handler = AccessTools.TypeByName("SPT.Common.Http.RequestHandler");
            _postAsync = handler == null ? null : AccessTools.Method(handler, "PostJsonAsync", new[] { typeof(string), typeof(string) });
            if (_postAsync == null)
            {
                _off = true;
                Plugin.Log.LogWarning($"{Tag}Off: SPT's RequestHandler wasn't found, so Name Plates can't be sent or fetched");
            }
        }

        return _postAsync != null;
    }

    /// <summary>
    /// Posts to a route; the reply (null on failure) is handled on the main thread. A reply without
    /// <paramref name="expect"/> isn't Name Plate's: a server without the routes answers some routes
    /// with an error object, which mustn't pass for "shared".
    /// </summary>
    private static void Post(string route, string json, string expect, Action<JObject> done)
    {
        // Counted on the main thread, where everything else here lives
        void Fail(string why) => MainThread.Enqueue(() =>
        {
            if (++_failures >= GiveUpAfter && !_off)
            {
                _off = true;
                Plugin.Log.LogWarning($"{Tag}Turned off for this session: the server didn't answer {route} ({why}). "
                    + $"Is the server's Name Plate mod {ModVersion.Value} or later?");
            }

            done(null);
        });

        try
        {
            if (_postAsync.Invoke(null, new object[] { route, json }) is not Task<string> task)
            {
                Fail("no request");
                return;
            }

            task.ContinueWith(t =>
            {
                JObject reply = null;
                try
                {
                    reply = t.IsFaulted || string.IsNullOrEmpty(t.Result) ? null : JObject.Parse(t.Result);
                }
                catch (JsonException)
                {
                }

                if (reply == null || (expect != null && reply[expect] == null))
                {
                    Fail(t.IsFaulted ? t.Exception?.GetBaseException().Message : "not a reply from Name Plate");
                    return;
                }

                MainThread.Enqueue(() =>
                {
                    _failures = 0;
                    done(reply);
                });
            });
        }
        catch (Exception e)
        {
            Fail((e is TargetInvocationException { InnerException: { } inner } ? inner : e).Message);
        }
    }
}
