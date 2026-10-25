using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EFT;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NamePlate.Client;

/// <summary>
/// The PMCs that keep killing you. Every death to a PMC (a bot, or with Fika a real player) is
/// counted by the killer's name in nemesis.json; one that has killed you often enough is marked
/// NEMESIS on the death screen, and when you kill someone who has killed you, the kill list says
/// REVENGE. Bot looks come from names too, so a nemesis looks the same every time you meet.
/// </summary>
public static class Nemesis
{
    public static readonly Color MarkColor = new(1f, 0.36f, 0.30f, 1f);
    private const int KeepRaids = 50;

    public class Killer
    {
        [JsonProperty("deaths")] public int Deaths;
        [JsonProperty("side")] public string Side;
        [JsonProperty("last")] public DateTime Last;
    }

    private class Saved
    {
        [JsonProperty("killers")] public Dictionary<string, Killer> Killers = new(StringComparer.OrdinalIgnoreCase);

        // Deaths already counted (killer's profile id and the raid's date): the death screen can
        // show more than once for one death
        [JsonProperty("counted")] public List<string> Counted = new();
    }

    private static Saved _saved;
    private static string _path;

    public static void Load(string folder)
    {
        _path = Path.Combine(folder, "nemesis.json");
        try
        {
            _saved = File.Exists(_path) ? JsonConvert.DeserializeObject<Saved>(File.ReadAllText(_path)) : null;
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"Couldn't read {_path}, starting a new nemesis list: {e.Message}");
        }

        _saved ??= new Saved();
        _saved.Killers = new Dictionary<string, Killer>(_saved.Killers ?? new Dictionary<string, Killer>(), StringComparer.OrdinalIgnoreCase);
        _saved.Counted ??= new List<string>();
    }

    private static NemesisConfig Config => Plugin.Settings.Nemesis ?? new NemesisConfig();

    /// <summary>How many times this name has killed you.</summary>
    public static int DeathsTo(string name) =>
        _saved != null && !string.IsNullOrEmpty(name) && _saved.Killers.TryGetValue(name, out var killer) ? killer.Deaths : 0;

    /// <summary>A death to a PMC, once per raid however often the death screen shows.</summary>
    public static void Count(string name, string profileId, string side, int raidDate)
    {
        if (_saved == null || string.IsNullOrEmpty(name) || !Config.Enabled)
        {
            return;
        }

        var key = $"{profileId}:{raidDate}";
        if (_saved.Counted.Contains(key))
        {
            return;
        }

        _saved.Counted.Add(key);
        if (_saved.Counted.Count > KeepRaids)
        {
            _saved.Counted.RemoveRange(0, _saved.Counted.Count - KeepRaids);
        }

        if (!_saved.Killers.TryGetValue(name, out var killer))
        {
            _saved.Killers[name] = killer = new Killer();
        }

        killer.Deaths++;
        killer.Side = side;
        killer.Last = DateTime.UtcNow;
        Plugin.Log.LogInfo($"Killed by {name}: {killer.Deaths} time(s) now");
        Save();
    }

    private static void Save()
    {
        try
        {
            File.WriteAllText(_path, JsonConvert.SerializeObject(_saved, Formatting.Indented));
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"Couldn't save {_path}: {e.Message}");
        }
    }

    /// <summary>The death screen's killer: NEMESIS once they've killed you often enough.</summary>
    public static void MarkKiller(TMP_Text label, string name)
    {
        var deaths = DeathsTo(name);
        Marker.Set(label, Config.Enabled && deaths >= Math.Max(1, Config.After) ? $"NEMESIS x{deaths}" : null);
    }

    /// <summary>A kill list row: REVENGE on anyone who has killed you before.</summary>
    public static void MarkVictim(TMP_Text label, string name) =>
        Marker.Set(label, Config.Enabled && DeathsTo(name) > 0 ? "REVENGE" : null);

    public static bool IsPmc(WildSpawnType role, EPlayerSide side) =>
        BotLooks.IsPmcBot(role) || side is EPlayerSide.Bear or EPlayerSide.Usec;
}

/// <summary>"nemesis" in icons.json.</summary>
public class NemesisConfig
{
    [JsonProperty("enabled")]
    public bool Enabled = true;

    /// <summary>Deaths to one PMC before they're marked NEMESIS.</summary>
    [JsonProperty("after")]
    public int After = 2;
}

/// <summary>
/// A small word in red just after a name label's text, as its own child label so the name's
/// colors and layout are left alone. Labels are reused, so null hides it.
/// </summary>
public static class Marker
{
    private const string ObjectName = "NamePlate_Marker";

    public static void Set(TMP_Text label, string text)
    {
        if (label == null)
        {
            return;
        }

        var existing = label.transform.Cast<Transform>().FirstOrDefault(t => t.name == ObjectName);
        if (text == null)
        {
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
            }

            return;
        }

        TextMeshProUGUI mark;
        if (existing != null)
        {
            mark = existing.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            var go = new GameObject(ObjectName, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(label.transform, false);
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            mark = go.GetComponent<TextMeshProUGUI>();
            mark.raycastTarget = false;
            mark.enableWordWrapping = false;
            mark.overflowMode = TextOverflowModes.Overflow;
            mark.alignment = TextAlignmentOptions.MidlineLeft;
            mark.fontStyle = FontStyles.Bold;
        }

        mark.font = label.font;
        mark.fontSize = Mathf.Max(10f, label.fontSize * 0.55f);
        mark.color = Nemesis.MarkColor;
        mark.text = text;

        // Just after the text (a tag of ours included), at the label's middle height. Positions are in
        // the label's own space, whose origin is its pivot; the child is anchored there to match.
        var rect = (RectTransform)mark.transform;
        rect.anchorMin = rect.anchorMax = label.rectTransform.pivot;
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(300f, mark.fontSize * 1.4f);
        var box = label.rectTransform.rect;
        var markWidth = mark.GetPreferredValues(text).x;
        var x = Mathf.Min(TextRight(label, box) + mark.fontSize * 0.6f, Mathf.Max(box.xMin, box.xMax - markWidth));
        rect.anchoredPosition = new Vector2(x, box.center.y);
        mark.gameObject.SetActive(true);
    }

    // Where the label's text ends. Drawn bounds when the label has been drawn; before that (a screen
    // still opening) they're empty, so the width the text wants and the label's alignment instead.
    private static float TextRight(TMP_Text label, Rect box)
    {
        label.ForceMeshUpdate();
        var bounds = label.textBounds;
        if (bounds.size.x > 0.01f)
        {
            return bounds.max.x;
        }

        var width = Mathf.Min(label.GetPreferredValues(label.text).x, box.width);
        var horizontal = (int)label.alignment & 0xFF;
        return (horizontal & 0x04) != 0 ? box.xMax
            : (horizontal & 0x22) != 0 ? box.center.x + width / 2
            : box.xMin + width;
    }
}
