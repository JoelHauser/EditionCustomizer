using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EFT.UI;
using Newtonsoft.Json;
using UnityEngine;
using IconsData = EFT.UI.ChatSpecialIconSettings.IconsData;

namespace NamePlate.Client;

/// <summary>
/// Applies icons.json to the game's icon table (ChatSpecialIconSettings), which is where every
/// member icon and nickname color comes from.
/// </summary>
public static class IconTable
{
    // Each table the game hands us, with the game's own rows so a reload can start over from
    // them. UI prefabs can hold their own copy of the asset.
    private static readonly Dictionary<ChatSpecialIconSettings, (IconsData[] Rows, EMemberCategory[] Priority)> Originals = new();

    // Categories whose dropdown label should be our name rather than the game's translation
    public static readonly HashSet<EMemberCategory> Named = new();

    // Multi-color nicknames by category
    private static readonly Dictionary<EMemberCategory, NameStyle> Styles = new();

    public static NameStyle StyleFor(EMemberCategory category) => Styles.TryGetValue(category, out var style) ? style : null;

    // Categories icons.json changes at all (name, colors or picture)
    private static readonly HashSet<EMemberCategory> Customized = new();

    public static bool IsCustomized(EMemberCategory category) => Customized.Contains(category);

    /// <summary>
    /// Set while the game draws someone who isn't you: a trader, a system sender, a chat bot, a
    /// scav on the death screen. A changed game icon (Standard, EoD, ...) is your look, not
    /// everyone's who shares the category, so lookups made meanwhile get the game's own rows.
    /// </summary>
    public static bool OthersScope;

    /// <summary>Who that someone else is, when the place drawing them says (for the look they share).</summary>
    public static string OthersName;

    // Each category's icons.json entry, for the look you share with other players
    private static readonly Dictionary<EMemberCategory, IconEntry> Entries = new();

    /// <summary>Your icons.json entry for a category, or null when it's the game's own.</summary>
    public static IconEntry EntryFor(EMemberCategory category) => Entries.TryGetValue(category, out var entry) ? entry : null;

    /// <summary>
    /// The game's own answer for a category, from its original rows: the same search
    /// GetDataByMemberCategory does, over the table as it was before icons.json.
    /// </summary>
    public static IconsData OriginalFor(ChatSpecialIconSettings settings, EMemberCategory category)
    {
        Apply(settings);
        if (!Originals.TryGetValue(settings, out var original))
        {
            return null;
        }

        // Of the flags the category has, the one earliest in Priority; Default (0) always counts
        var target = category;
        var best = -1;
        foreach (EMemberCategory flag in Enum.GetValues(typeof(EMemberCategory)))
        {
            if ((category & flag) != flag)
            {
                continue;
            }

            var index = Array.IndexOf(original.Priority, flag);
            if (index >= 0 && (best < 0 || index < best))
            {
                best = index;
                target = flag;
            }
        }

        return original.Rows.FirstOrDefault(r => r.Category == target);
    }

    // PNGs as loaded, with their file's write time. Their textures are never unloaded, so a reload
    // keeps the ones whose file hasn't changed rather than piling up copies on every editor save.
    private static readonly Dictionary<string, (Sprite Sprite, DateTime Written)> LoadedSprites = new();

    // Animated icons: every frame of a strip, by any of its frames
    private static readonly Dictionary<Sprite, Sprite[]> Strips = new();

    // Per category: an icon effect and frame rate, and the prestige badge setting
    private static readonly Dictionary<EMemberCategory, IconMotion> Motions = new();
    private static readonly Dictionary<EMemberCategory, string> Prestige = new();

    /// <summary>An icon's frames (null for a still one), frame rate and effect.</summary>
    public class IconMotion
    {
        public Sprite[] Frames;
        public float Fps = 8f;
        public string Effect;
        public Color Tint = Color.white;
    }

    /// <summary>The frames of an animated icon, given any of its frames; null for a still one.</summary>
    public static Sprite[] FramesOf(Sprite sprite) => sprite != null && Strips.TryGetValue(sprite, out var frames) ? frames : null;

    /// <summary>How your icon for a category moves, or null for not at all.</summary>
    public static IconMotion MotionFor(EMemberCategory category) => Motions.TryGetValue(category, out var motion) ? motion : null;

    /// <summary>The prestige badge for a category: null (the game's), "hide", or a PNG path.</summary>
    public static string PrestigeFor(EMemberCategory category) => Prestige.TryGetValue(category, out var value) ? value : null;

    private static bool _dumped;

    public static bool IsCustom(EMemberCategory category) => !Enum.IsDefined(typeof(EMemberCategory), category);

    /// <summary>
    /// The Name Plate made on the host for the icon you show (<see cref="SharedNamePlates"/>), or null.
    /// Used only when this PC's icons.json doesn't define that icon.
    /// </summary>
    public static IconEntry HostEntry { get; private set; }

    private static string _hostRaw;

    /// <summary>Takes the host's Name Plate; true when that changed what's drawn (icons reloaded).</summary>
    public static bool SetHostEntry(IconEntry entry, string raw)
    {
        if (raw == _hostRaw)
        {
            return false;
        }

        _hostRaw = raw;
        HostEntry = entry;
        Reload();
        return true;
    }

    /// <summary>Whether this PC's own icons.json has an entry for that icon (the host's doesn't count).</summary>
    public static bool DefinedHere(EMemberCategory category) => Entries.TryGetValue(category, out var entry) && entry != HostEntry;

    /// <summary>
    /// The profile wears a new icon (2048+) that neither this PC's icons.json nor the host defines,
    /// so the game shows the default. False until icons.json has been applied (nothing to check
    /// against yet).
    /// </summary>
    public static bool WarnIfUndefined(EMemberCategory selected)
    {
        if (Originals.Count == 0)
        {
            return false;
        }

        for (var bit = 0x800; bit > 0; bit <<= 1)
        {
            var flag = (EMemberCategory)bit;
            if (((int)selected & bit) != 0 && IsCustom(flag) && !Customized.Contains(flag))
            {
                Plugin.Log.LogWarning($"Your profile wears icon {bit}, but neither this PC's icons.json nor the server has a Name Plate "
                    + "for it, so the game shows its default. Make it in Name Plate.exe, on this PC or on the server's PC "
                    + "(that one needs Name Plate {ModVersion.Value} or later on the server).");
            }
        }

        return true;
    }

    public static void Apply(ChatSpecialIconSettings settings)
    {
        if (settings == null || Originals.ContainsKey(settings))
        {
            return;
        }

        Originals[settings] = (settings.IconsSettings.Select(Copy).ToArray(), settings.Priority.ToArray());
        LogTable(settings, "Game icon table");

        if (Plugin.Settings.DumpOriginalIcons && !_dumped)
        {
            _dumped = true;
            DumpOriginals(settings);
        }

        ApplyConfig(settings);
    }

    /// <summary>
    /// icons.json changed: put every table back to the game's rows and apply it again. Screens
    /// already open keep what they drew until they are shown again.
    /// </summary>
    public static void Reload()
    {
        // Strips stay: they're keyed by sprite, and the unchanged sprites kept here still need theirs
        foreach (var changed in LoadedSprites.Where(p => WrittenAt(p.Key) != p.Value.Written).Select(p => p.Key).ToList())
        {
            LoadedSprites.Remove(changed);
        }

        Motions.Clear();
        Prestige.Clear();
        Entries.Clear();
        Named.Clear();
        Styles.Clear();
        Customized.Clear();

        foreach (var pair in Originals)
        {
            if (pair.Key == null)
            {
                continue;
            }

            pair.Key.IconsSettings = pair.Value.Rows.Select(Copy).ToArray();
            pair.Key.Priority = pair.Value.Priority.ToArray();
            ApplyConfig(pair.Key);
        }

        NameColorizer.RefreshAll();
    }

    private static IconsData Copy(IconsData row) =>
        new() { Name = row.Name, Category = row.Category, IconSprite = row.IconSprite, IconColor = row.IconColor };

    private static void ApplyConfig(ChatSpecialIconSettings settings)
    {
        try
        {
            var rows = settings.IconsSettings.ToList();
            var priority = settings.Priority.ToList();
            var fallbackSprite = rows.FirstOrDefault(r => r.Category == EMemberCategory.Default)?.IconSprite;

            // The host's Name Plate goes last, and only for an icon this PC's icons.json leaves alone
            var host = HostEntry;
            foreach (var entry in host == null ? Plugin.Settings.Icons : Plugin.Settings.Icons.Concat(new[] { host }))
            {
                if (!TryParseCategory(entry.Category, out var category) || (entry == host && Customized.Contains(category)))
                {
                    continue;
                }

                var row = rows.FirstOrDefault(r => r.Category == category);
                if (row == null)
                {
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Plugin.Log.LogWarning($"Icon {entry.Category} is new and needs a \"name\", skipped");
                        continue;
                    }

                    row = new IconsData { Category = category, IconSprite = fallbackSprite, IconColor = Color.white };
                    rows.Add(row);
                }

                Customized.Add(category);
                Entries[category] = entry;
                if (!string.IsNullOrEmpty(entry.Name))
                {
                    row.Name = entry.Name;
                    Named.Add(category);
                }

                if (!string.IsNullOrEmpty(entry.Color))
                {
                    if (ColorUtility.TryParseHtmlString(entry.Color, out var color))
                    {
                        row.IconColor = color;
                    }
                    else
                    {
                        Plugin.Log.LogWarning($"Icon {entry.Category}: \"{entry.Color}\" isn't a color, use #RRGGBB");
                    }
                }

                // Places that take one color get the first
                var style = NameStyle.From(entry, row.IconColor);
                if (style != null)
                {
                    Styles[category] = style;
                    row.IconColor = style.Colors[0];
                }

                if (!string.IsNullOrEmpty(entry.Icon))
                {
                    var sprite = FileSprite(entry.Icon);
                    if (sprite != null)
                    {
                        row.IconSprite = sprite;
                    }
                }
                else if (!string.IsNullOrEmpty(entry.IconFrom))
                {
                    // From the game's rows, so a borrowed icon is never one icons.json replaced
                    var source = TryParseCategory(entry.IconFrom, out var from)
                        ? Originals[settings].Rows.FirstOrDefault(r => r.Category == from)
                        : null;
                    if (source?.IconSprite != null)
                    {
                        row.IconSprite = source.IconSprite;
                    }
                    else
                    {
                        Plugin.Log.LogWarning($"Icon {entry.Category}: the game has no \"{entry.IconFrom}\" icon to borrow");
                    }
                }

                var frames = FramesOf(row.IconSprite);
                var effect = string.IsNullOrWhiteSpace(entry.IconEffect) ? null : entry.IconEffect.Trim().ToLowerInvariant();
                if (frames != null || effect != null)
                {
                    Motions[category] = new IconMotion
                    {
                        Frames = frames,
                        Fps = entry.Fps > 0 ? entry.Fps : 8f,
                        Effect = effect,
                        Tint = style?.Colors[0] ?? row.IconColor,
                    };
                }

                if (!string.IsNullOrWhiteSpace(entry.Prestige))
                {
                    Prestige[category] = entry.Prestige.Trim();
                }

                // The settings dropdown only offers categories that are in Priority
                if (!priority.Contains(category))
                {
                    priority.Add(category);
                }
            }

            settings.IconsSettings = rows.ToArray();
            settings.Priority = priority.ToArray();
            LogTable(settings, "After icons.json");
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"Couldn't apply icons.json: {e}");
        }
    }

    private static bool TryParseCategory(string text, out EMemberCategory category)
    {
        category = EMemberCategory.Default;
        if (string.IsNullOrWhiteSpace(text))
        {
            Plugin.Log.LogWarning("An icon entry has no \"category\", skipped");
            return false;
        }

        if (int.TryParse(text, out var number))
        {
            category = (EMemberCategory)number;
        }
        else if (!Enum.TryParse(text.Trim(), true, out category))
        {
            Plugin.Log.LogWarning($"\"{text}\" isn't a category the game knows, skipped");
            return false;
        }

        // A new one has to be a single flag of its own, or the game's flag checks match it
        // against everything it overlaps
        if (IsCustom(category) && (number < 0x800 || (number & (number - 1)) != 0))
        {
            Plugin.Log.LogWarning($"Icon {text}: new icons must be 2048, 4096, 8192, ... (a free single flag), skipped");
            return false;
        }

        return true;
    }

    /// <summary>The game's own picture for a member category, whatever icons.json changed.</summary>
    public static Sprite GameSprite(EMemberCategory category)
    {
        if (Originals.Count == 0)
        {
            Apply(EFTHardSettings.Instance.ChatSpecialIconSettings);
        }

        foreach (var pair in Originals)
        {
            var row = pair.Value.Rows.FirstOrDefault(r => r.Category == category);
            if (row?.IconSprite != null)
            {
                return row.IconSprite;
            }
        }

        return null;
    }

    /// <summary>A PNG under the plugin folder as a sprite (an animated one's first frame), loaded once.</summary>
    public static Sprite FileSprite(string relativePath)
    {
        if (LoadedSprites.TryGetValue(relativePath, out var cached))
        {
            return cached.Sprite;
        }

        var path = Path.Combine(Plugin.Folder, relativePath);
        if (!File.Exists(path))
        {
            Plugin.Log.LogWarning($"Icon file not found: {path}");
            return null;
        }

        var written = File.GetLastWriteTimeUtc(path);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = relativePath, filterMode = FilterMode.Bilinear };
        if (!texture.LoadImage(File.ReadAllBytes(path)))
        {
            Plugin.Log.LogWarning($"Couldn't read {path} as an image");
            return null;
        }

        texture.hideFlags = HideFlags.DontUnloadUnusedAsset;

        // A strip of square frames side by side is an animated icon
        var count = texture.height > 0 && texture.width % texture.height == 0 ? texture.width / texture.height : 1;
        var frames = new Sprite[count >= 2 ? count : 1];
        var side = count >= 2 ? texture.height : texture.width;
        for (var i = 0; i < frames.Length; i++)
        {
            frames[i] = Sprite.Create(texture, new Rect(i * side, 0, side, texture.height), new Vector2(0.5f, 0.5f), 100f);
            frames[i].name = frames.Length > 1 ? $"{relativePath}#{i}" : relativePath;
            frames[i].hideFlags = HideFlags.DontUnloadUnusedAsset;
        }

        if (frames.Length > 1)
        {
            foreach (var frame in frames)
            {
                Strips[frame] = frames;
            }
        }

        var sprite = frames[0];
        LoadedSprites[relativePath] = (sprite, written);
        Plugin.Log.LogInfo($"Loaded icon {relativePath} ({texture.width}x{texture.height})");
        return sprite;
    }

    // A missing file reads as 1601, so it counts as changed
    private static DateTime WrittenAt(string relativePath) => File.GetLastWriteTimeUtc(Path.Combine(Plugin.Folder, relativePath));

    /// <summary>
    /// originals\: each game icon as a PNG plus table.json with their names and colors, for the
    /// editor and as templates.
    /// </summary>
    private static void DumpOriginals(ChatSpecialIconSettings settings)
    {
        var folder = Path.Combine(Plugin.Folder, "originals");
        Directory.CreateDirectory(folder);

        foreach (var row in settings.IconsSettings)
        {
            if (row.IconSprite == null)
            {
                continue;
            }

            try
            {
                // The game's textures aren't readable, so copy through the GPU
                var sprite = row.IconSprite;
                var source = sprite.texture;
                var rect = sprite.textureRect;
                var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(source, rt);
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var copy = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
                copy.ReadPixels(rect, 0, 0);
                copy.Apply();
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);

                File.WriteAllBytes(Path.Combine(folder, $"{row.Category}.png"), copy.EncodeToPNG());
                UnityEngine.Object.Destroy(copy);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Couldn't save the {row.Category} icon: {e.Message}");
            }
        }

        try
        {
            var table = settings.IconsSettings.Select(r => new
            {
                category = r.Category.ToString(),
                value = (int)r.Category,
                name = r.Name,
                color = "#" + ColorUtility.ToHtmlStringRGBA(r.IconColor),
                size = r.IconSprite == null ? 0 : (int)r.IconSprite.rect.width,
                shownInSettings = settings.Priority.Contains(r.Category),
            });
            File.WriteAllText(Path.Combine(folder, "table.json"), JsonConvert.SerializeObject(table, Formatting.Indented));
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"Couldn't save table.json: {e.Message}");
        }

        Plugin.Log.LogInfo($"Saved the game's icons to {folder}");
    }

    private static void LogTable(ChatSpecialIconSettings settings, string title)
    {
        var lines = settings.IconsSettings.Select(r =>
            $"  {(int)r.Category,6} {r.Category,-10} name=\"{r.Name}\" color=#{ColorUtility.ToHtmlStringRGBA(r.IconColor)} "
            + $"sprite={(r.IconSprite == null ? "none" : $"{r.IconSprite.name} {r.IconSprite.rect.width}x{r.IconSprite.rect.height}")}");
        Plugin.Log.LogInfo($"{title}:\n{string.Join("\n", lines)}\n  priority: {string.Join(", ", settings.Priority)}");
    }
}
