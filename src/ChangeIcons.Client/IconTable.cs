using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EFT.UI;
using Newtonsoft.Json;
using UnityEngine;
using IconsData = EFT.UI.ChatSpecialIconSettings.IconsData;

namespace ChangeIcons.Client;

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

    private static readonly Dictionary<string, Sprite> LoadedSprites = new();
    private static bool _dumped;

    public static bool IsCustom(EMemberCategory category) => !Enum.IsDefined(typeof(EMemberCategory), category);

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
        LoadedSprites.Clear();
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

            foreach (var entry in Plugin.Settings.Icons)
            {
                if (!TryParseCategory(entry.Category, out var category))
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
                var style = NameStyle.From(entry);
                if (style != null)
                {
                    Styles[category] = style;
                    row.IconColor = style.Colors[0];
                }

                if (!string.IsNullOrEmpty(entry.Icon))
                {
                    var sprite = LoadSprite(entry.Icon);
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

    /// <summary>A PNG under the plugin folder as a sprite, loaded once.</summary>
    public static Sprite FileSprite(string relativePath) => LoadSprite(relativePath);

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

    private static Sprite LoadSprite(string relativePath)
    {
        if (LoadedSprites.TryGetValue(relativePath, out var cached))
        {
            return cached;
        }

        var path = Path.Combine(Plugin.Folder, relativePath);
        if (!File.Exists(path))
        {
            Plugin.Log.LogWarning($"Icon file not found: {path}");
            return null;
        }

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = relativePath, filterMode = FilterMode.Bilinear };
        if (!texture.LoadImage(File.ReadAllBytes(path)))
        {
            Plugin.Log.LogWarning($"Couldn't read {path} as an image");
            return null;
        }

        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = relativePath;
        texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
        sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
        LoadedSprites[relativePath] = sprite;
        Plugin.Log.LogInfo($"Loaded icon {relativePath} ({texture.width}x{texture.height})");
        return sprite;
    }

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
