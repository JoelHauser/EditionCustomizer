using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace NamePlate.Client;

/// <summary>
/// icons.json, next to the plugin DLL.
/// </summary>
public class IconConfig
{
    /// <summary>
    /// Writes every member icon the game ships to originals\ as a PNG, to use as a template. Off
    /// unless asked for: the editor reads the game's icons itself.
    /// </summary>
    [JsonProperty("dumpOriginalIcons")]
    public bool DumpOriginalIcons;

    [JsonProperty("icons")]
    public List<IconEntry> Icons = new();

    [JsonProperty("bots")]
    public BotConfig Bots;

    /// <summary>Your scav's name gets your Name Plate too (the game counts it as someone else).</summary>
    [JsonProperty("scavLook")]
    public bool ScavLook;

    /// <summary>
    /// Name Plate sharing: other players on your server who run the mod (Fika) see your Name Plate;
    /// off, they see the game's. Theirs show for you either way.
    /// </summary>
    [JsonProperty("shareNamePlate")]
    public bool ShareNamePlate = true;

    // The first 1.1.0 build called it "shareLook"
    [JsonProperty("shareLook")]
    private bool? LegacyShareLook
    {
        get => null;
        set
        {
            if (value is { } share)
            {
                ShareNamePlate = share;
            }
        }
    }

    [JsonProperty("nemesis")]
    public NemesisConfig Nemesis;

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

    /// <summary>"scroll" (default), "pulse", "wave", "sparkle", "rainbow", "glitch" or "bounce" (Shared.Motions).</summary>
    [JsonProperty("motion")]
    public string Motion;

    /// <summary>Run the motion the other way.</summary>
    [JsonProperty("reverse")]
    public bool Reverse;

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

    /// <summary>Outline around the name's letters, "#RRGGBB".</summary>
    [JsonProperty("outline")]
    public string Outline;

    /// <summary>A soft glow behind the name, "#RRGGBB". Glow and shadow share one layer: glow wins.</summary>
    [JsonProperty("glow")]
    public string Glow;

    /// <summary>A drop shadow under the name.</summary>
    [JsonProperty("shadow")]
    public bool Shadow;

    [JsonProperty("bold")]
    public bool Bold;

    [JsonProperty("italic")]
    public bool Italic;

    [JsonProperty("smallCaps")]
    public bool SmallCaps;

    /// <summary>A clan tag shown before your name, "[TAG] Name". Without brackets.</summary>
    [JsonProperty("tag")]
    public string Tag;

    /// <summary>The tag's color: "#RRGGBB", or "name" to paint it with the name's colors. Missing: a quiet gray.</summary>
    [JsonProperty("tagColor")]
    public string TagColor;

    /// <summary>Frames per second when the icon PNG is a strip of square frames side by side.</summary>
    [JsonProperty("fps")]
    public float Fps;

    /// <summary>"pulse", "glow", "flip" or "float": a movement or halo on the icon itself.</summary>
    [JsonProperty("iconEffect")]
    public string IconEffect;

    /// <summary>The prestige badge beside the icon: "hide", or a PNG path to show instead.</summary>
    [JsonProperty("prestige")]
    public string Prestige;
}

/// <summary>
/// "bots" in icons.json: looks for PMC bots, worked out from each bot's name so the same name
/// always looks the same.
/// </summary>
public class BotConfig
{
    /// <summary>On unless set to false, the same as the editor reads it.</summary>
    [JsonProperty("enabled")]
    public bool Enabled = true;

    /// <summary>Percent of PMC bots that get a look.</summary>
    [JsonProperty("share")]
    public int Share = 100;

    /// <summary>Icon pool: paths under the plugin folder, or "member:Unheard".</summary>
    [JsonProperty("icons")]
    public List<string> Icons = new();

    /// <summary>
    /// Any of "solid", "gradient", "letters"; missing means all three. (No default list here:
    /// Newtonsoft adds to an existing list instead of replacing it.)
    /// </summary>
    [JsonProperty("modes")]
    public List<string> Modes;

    [JsonProperty("palettes")]
    public List<List<string>> Palettes = new();

    [JsonProperty("randomColors")]
    public bool RandomColors = true;

    [JsonProperty("animateChance")]
    public int AnimateChance = Shared.BotDefaults.AnimateChance;

    [JsonProperty("maxSpeed")]
    public double MaxSpeed = 1.0;

    /// <summary>
    /// Which of Shared.Motions bots may use; missing means all. No
    /// default list here: Newtonsoft adds to an existing list instead of replacing it.
    /// </summary>
    [JsonProperty("motions")]
    public List<string> Motions;

    /// <summary>Percent of bots with a look that also wear a clan tag.</summary>
    [JsonProperty("tagChance")]
    public int TagChance = Shared.BotDefaults.TagChance;

    /// <summary>Percent of tagged bots whose tag is colored (one of the name's colors, or all of them) rather than gray.</summary>
    [JsonProperty("tagColorChance")]
    public int TagColorChance = Shared.BotDefaults.TagColorChance;

    /// <summary>Tags to pick from; missing means the built-in list (Shared.BotTags).</summary>
    [JsonProperty("tags")]
    public List<string> Tags;

    /// <summary>BEAR bots draw warm colors, USEC bots cool ones.</summary>
    [JsonProperty("factionColors")]
    public bool FactionColors;

    /// <summary>Percent of bots whose name gets an effect (outline, glow, shadow, bold, italic, small caps).</summary>
    [JsonProperty("effectChance")]
    public int EffectChance = Shared.BotDefaults.EffectChance;

    /// <summary>Which name effects they may get; missing means all.</summary>
    [JsonProperty("effects")]
    public List<string> Effects;

    /// <summary>Percent of bots whose icon pulses, glows, flips or floats.</summary>
    [JsonProperty("iconEffectChance")]
    public int IconEffectChance = Shared.BotDefaults.IconEffectChance;

    /// <summary>Which icon effects they may get; missing means all.</summary>
    [JsonProperty("iconEffects")]
    public List<string> IconEffects;

    /// <summary>
    /// The library and member icons there were when "icons" was saved. Any icon not in it is new
    /// since, and joins the pool: everything starts on. Missing (an older file) means all are on.
    /// </summary>
    [JsonProperty("knownIcons")]
    public List<string> KnownIcons;

    /// <summary>The same for "motions": a motion added since starts on.</summary>
    [JsonProperty("knownMotions")]
    public List<string> KnownMotions;
}
