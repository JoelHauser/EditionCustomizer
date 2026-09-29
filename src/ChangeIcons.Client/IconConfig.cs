using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ChangeIcons.Client;

/// <summary>
/// icons.json, next to the plugin DLL.
/// </summary>
public class IconConfig
{
    /// <summary>
    /// Writes every icon the game ships to originals\ as a PNG, to use as a template.
    /// </summary>
    [JsonProperty("dumpOriginalIcons")]
    public bool DumpOriginalIcons = true;

    [JsonProperty("icons")]
    public List<IconEntry> Icons = new();

    public static IconConfig Load(string path) =>
        JsonConvert.DeserializeObject<IconConfig>(File.ReadAllText(path)) ?? new IconConfig();
}

public class IconEntry
{
    /// <summary>
    /// A game category name (Unheard, UniqueId, Sherpa, Emissary, Developer) to change an
    /// existing icon, or a number to add a new one. New ones must be a single free flag:
    /// 2048, 4096, 8192, ...
    /// </summary>
    [JsonProperty("category")]
    public string Category;

    /// <summary>
    /// Shown in the settings dropdown. Required for new icons; replaces the game's label on
    /// existing ones.
    /// </summary>
    [JsonProperty("name")]
    public string Name;

    /// <summary>
    /// Nickname color, "#RRGGBB" or "#RRGGBBAA".
    /// </summary>
    [JsonProperty("color")]
    public string Color;

    /// <summary>
    /// Several nickname colors. With two or more they replace "color": a gradient across the
    /// name, or one color per letter.
    /// </summary>
    [JsonProperty("colors")]
    public List<string> Colors;

    /// <summary>"gradient" (default) or "letters".</summary>
    [JsonProperty("colorMode")]
    public string ColorMode;

    /// <summary>
    /// Moves the colors along the name: gradient cycles per second, or letter steps per second
    /// divided by 4. 0 keeps them still.
    /// </summary>
    [JsonProperty("animate")]
    public float Animate;

    /// <summary>
    /// PNG path relative to the plugin folder.
    /// </summary>
    [JsonProperty("icon")]
    public string Icon;

    /// <summary>
    /// Borrow the picture of a game icon instead of a PNG, e.g. "Unheard".
    /// </summary>
    [JsonProperty("iconFrom")]
    public string IconFrom;
}
