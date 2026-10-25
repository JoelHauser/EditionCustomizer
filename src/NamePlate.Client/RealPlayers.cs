using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EFT;
using HarmonyLib;
using Newtonsoft.Json;

namespace NamePlate.Client;

/// <summary>
/// The real people this game has met, so bot looks never go on them. Alone in SPT that's only you,
/// but with Fika, a friend's dogtag or their offer on the flea market would otherwise get a made-up
/// bot look. Learned in raids (a PMC-side player whose role isn't a bot PMC's: human PMCs have the
/// role "assault") and from your friends list, and kept in players.json between sessions.
/// </summary>
public static class RealPlayers
{
    private static readonly HashSet<string> Ids = new(StringComparer.Ordinal);
    private static readonly HashSet<string> Names = new(StringComparer.OrdinalIgnoreCase);
    private static string _path;

    private class Saved
    {
        [JsonProperty("ids")] public List<string> Ids = new();
        [JsonProperty("names")] public List<string> Names = new();
    }

    public static void Load(string folder)
    {
        _path = Path.Combine(folder, "players.json");
        try
        {
            if (File.Exists(_path) && JsonConvert.DeserializeObject<Saved>(File.ReadAllText(_path)) is { } saved)
            {
                Ids.UnionWith(saved.Ids.Where(s => !string.IsNullOrEmpty(s)));
                Names.UnionWith(saved.Names.Where(s => !string.IsNullOrEmpty(s)));
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"Couldn't read {_path}, starting a new list of real players: {e.Message}");
        }
    }

    /// <summary>A real person seen in a raid.</summary>
    public static void Note(string profileId, string nickname)
    {
        var added = !string.IsNullOrEmpty(profileId) && Ids.Add(profileId);
        added |= !string.IsNullOrEmpty(nickname) && Names.Add(nickname);
        if (!added || _path == null)
        {
            return;
        }

        try
        {
            File.WriteAllText(_path, JsonConvert.SerializeObject(new Saved { Ids = Ids.ToList(), Names = Names.ToList() }, Formatting.Indented));
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"Couldn't save {_path}: {e.Message}");
        }
    }

    /// <summary>Whether a profile or name belongs to a real person rather than a bot.</summary>
    public static bool IsReal(string profileId, string nickname)
    {
        if ((!string.IsNullOrEmpty(profileId) && Ids.Contains(profileId)) || (!string.IsNullOrEmpty(nickname) && Names.Contains(nickname)))
        {
            return true;
        }

        try
        {
            // Your friends, including ones you haven't raided with yet
            var session = TarkovApplication.Exist(out var app) ? app.GetClientBackEndSession() : null;
            var friends = session?.SocialNetwork?.FriendsList;
            if (friends != null)
            {
                foreach (var friend in friends)
                {
                    if ((!string.IsNullOrEmpty(profileId) && friend.Id == profileId)
                        || (!string.IsNullOrEmpty(nickname) && string.Equals(friend.Info?.Nickname, nickname, StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }
            }
        }
        catch (Exception)
        {
            // No session yet, or the friends list isn't loaded: only the saved list counts
        }

        return false;
    }
}

/// <summary>Every player joining a raid, local or (with Fika) remote: note the real ones.</summary>
[HarmonyPatch(typeof(GameWorld), nameof(GameWorld.RegisterPlayer))]
public static class RealPlayerSeenPatch
{
    public static void Postfix(IPlayer iPlayer)
    {
        try
        {
            if (iPlayer is Player player && player.Profile?.Info is { } info
                && info.Side is EPlayerSide.Bear or EPlayerSide.Usec
                && !BotLooks.IsPmcBot(info.Settings?.Role ?? WildSpawnType.assault))
            {
                RealPlayers.Note(player.ProfileId, info.Nickname);
                SharedLooks.MetInRaid(player.ProfileId, info.Nickname);
            }
        }
        catch (Exception e)
        {
            Safe.Report("noting a real player", e);
        }
    }
}
