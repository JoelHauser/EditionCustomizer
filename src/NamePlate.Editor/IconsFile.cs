using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NamePlate.Editor;

public enum IconSource
{
    /// <summary>The category's own game icon (built-in categories only).</summary>
    Own,

    /// <summary>Another member icon, borrowed from the game at runtime.</summary>
    Game,

    /// <summary>A PNG under the plugin folder: the library, game-icons, or your own.</summary>
    File,
}

public enum NameMode
{
    Solid,
    Gradient,
    Letters,
}

/// <summary>
/// One icon as the editor sees it. Saved to icons.json in the plugin's format, with the
/// editor's own choices kept under "editor" so they can be edited again.
/// </summary>
public class EditableIcon
{
    public int Value;
    public bool IsCustom => !Categories.IsBuiltin(Value);
    public string Key => IsCustom ? Value.ToString() : Categories.EnumName(Value);

    public string? Name;

    /// <summary>Empty: the game's color. One: solid. More: gradient or per letter.</summary>
    public List<string> Colors = [];
    public NameMode Mode = NameMode.Solid;
    public double Animate;

    /// <summary>One of Shared.Motions; used when Animate is above 0.</summary>
    public string Motion = Shared.Motions.Scroll;
    public bool Reverse;

    public IconSource Source;
    public string? GameIcon;
    public string? File;
    public string? Tint;

    // Name effects: outline and glow are "#RRGGBB" or null for off. Glow and shadow share one
    // layer in the game, so only one of them is on at a time.
    public string? Outline;
    public string? Glow;
    public bool Shadow;
    public bool Bold;
    public bool Italic;
    public bool SmallCaps;

    /// <summary>Clan tag before the name, without brackets; null for none.</summary>
    public string? Tag;

    /// <summary>The tag's color: null for gray, "name" for the name's colors, or "#RRGGBB".</summary>
    public string? TagColor;

    /// <summary>Frames per second, for an animated icon (a strip of frames).</summary>
    public double Fps = 8;

    /// <summary>"pulse", "glow", "flip", "float" or null.</summary>
    public string? IconEffect;

    /// <summary>The prestige badge: null for the game's, "hide", or a PNG path.</summary>
    public string? Prestige;

    public bool HasEffects => Outline != null || Glow != null || Shadow || Bold || Italic || SmallCaps;

    public bool IsChanged => IsCustom || Name != null || Colors.Count > 0 || Source != IconSource.Own || Tint != null
        || HasEffects || Tag != null || IconEffect != null || Prestige != null || Animate > 0;

    public EditableIcon Clone()
    {
        var copy = (EditableIcon)MemberwiseClone();
        copy.Colors = [.. Colors];
        return copy;
    }
}

public class IconsFile
{
    [JsonPropertyName("dumpOriginalIcons")]
    public bool DumpOriginalIcons { get; set; }

    [JsonPropertyName("icons")]
    public List<Entry> Icons { get; set; } = [];

    [JsonPropertyName("bots")]
    public BotsConfig? Bots { get; set; }

    /// <summary>Your scav's name gets your look too.</summary>
    [JsonPropertyName("scavLook")]
    public bool ScavLook { get; set; }

    /// <summary>Other players on your server who run the mod see your look.</summary>
    [JsonPropertyName("shareLook")]
    public bool ShareLook { get; set; } = true;

    [JsonPropertyName("nemesis")]
    public NemesisConfig? Nemesis { get; set; }

    public class NemesisConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;

        /// <summary>Deaths to one PMC before they're marked NEMESIS.</summary>
        [JsonPropertyName("after")] public int After { get; set; } = 2;
    }

    /// <summary>Your own color presets, for the editor (the plugin doesn't read them).</summary>
    [JsonPropertyName("presets")]
    public List<UserPreset>? Presets { get; set; }

    public class UserPreset
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("colors")] public List<string> Colors { get; set; } = [];
    }

    public class Entry
    {
        // "2048" or 2048: the plugin reads either
        [JsonPropertyName("category"), JsonConverter(typeof(StringOrNumber))] public string Category { get; set; } = "";
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("color")] public string? Color { get; set; }
        [JsonPropertyName("colors")] public List<string>? Colors { get; set; }
        [JsonPropertyName("colorMode")] public string? ColorMode { get; set; }
        [JsonPropertyName("animate")] public double? Animate { get; set; }
        [JsonPropertyName("motion")] public string? Motion { get; set; }
        [JsonPropertyName("reverse")] public bool? Reverse { get; set; }
        [JsonPropertyName("icon")] public string? Icon { get; set; }
        [JsonPropertyName("iconFrom")] public string? IconFrom { get; set; }
        [JsonPropertyName("outline")] public string? Outline { get; set; }
        [JsonPropertyName("glow")] public string? Glow { get; set; }
        [JsonPropertyName("shadow")] public bool? Shadow { get; set; }
        [JsonPropertyName("bold")] public bool? Bold { get; set; }
        [JsonPropertyName("italic")] public bool? Italic { get; set; }
        [JsonPropertyName("smallCaps")] public bool? SmallCaps { get; set; }
        [JsonPropertyName("tag")] public string? Tag { get; set; }
        [JsonPropertyName("tagColor")] public string? TagColor { get; set; }
        [JsonPropertyName("fps")] public double? Fps { get; set; }
        [JsonPropertyName("iconEffect")] public string? IconEffect { get; set; }
        [JsonPropertyName("prestige")] public string? Prestige { get; set; }
        [JsonPropertyName("editor")] public EditorInfo? Editor { get; set; }
    }

    public class EditorInfo
    {
        [JsonPropertyName("source")] public string? Source { get; set; }
        [JsonPropertyName("gameIcon")] public string? GameIcon { get; set; }
        [JsonPropertyName("file")] public string? File { get; set; }
        [JsonPropertyName("tint")] public string? Tint { get; set; }
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

        // The plugin's reader takes "animate": "0.5"; so must this, or it rejects a file the game reads
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    /// <summary>A string, or a number read as its text. Written back as a string.</summary>
    private class StringOrNumber : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Number
                ? System.Text.Encoding.UTF8.GetString(reader.ValueSpan)
                : reader.GetString() ?? "";

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue(value);
    }

    private const string CodePrefix = "CI1-";

    // As for the file, but on one line: a code should be short
    private static readonly JsonSerializerOptions CodeOptions = new(Options) { WriteIndented = false };

    /// <summary>A look as a short code to share: the entry as JSON, base64url, with a version prefix.</summary>
    public static string ToCode(Entry entry)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(entry, CodeOptions);
        return CodePrefix + Convert.ToBase64String(json).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>The look in a shared code, or null if it isn't one.</summary>
    public static Entry? FromCode(string? code)
    {
        code = code?.Trim();
        if (code == null || !code.StartsWith(CodePrefix, StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            var body = code[CodePrefix.Length..].Replace('-', '+').Replace('_', '/');
            body = body.PadRight(body.Length + (4 - body.Length % 4) % 4, '=');
            return JsonSerializer.Deserialize<Entry>(Convert.FromBase64String(body), Options);
        }
        catch (Exception e) when (e is FormatException or JsonException)
        {
            return null;
        }
    }

    public static IconsFile Load(string path) =>
        System.IO.File.Exists(path) ? JsonSerializer.Deserialize<IconsFile>(System.IO.File.ReadAllText(path), Options) ?? new() : new();

    public void Save(string path)
    {
        Io.ReplaceText(path, JsonSerializer.Serialize(this, Options));
    }

    public static EditableIcon ToEditable(Entry entry)
    {
        var value = Categories.Parse(entry.Category) ?? throw new FormatException($"\"{entry.Category}\" isn't a category");
        var icon = new EditableIcon
        {
            Value = value, Name = entry.Name, Animate = entry.Animate ?? 0,
            Motion = string.IsNullOrEmpty(entry.Motion) ? Shared.Motions.Scroll : entry.Motion.Trim().ToLowerInvariant(),
            Reverse = entry.Reverse ?? false,
            Outline = entry.Outline,
            Glow = entry.Glow,
            Shadow = entry.Glow == null && (entry.Shadow ?? false),
            Italic = entry.Italic ?? false,
            Tag = Shared.BotTags.Clean(entry.Tag),
            TagColor = string.IsNullOrWhiteSpace(entry.TagColor) ? null : entry.TagColor.Trim(),
            Fps = entry.Fps is > 0 ? entry.Fps.Value : 8,
            IconEffect = string.IsNullOrWhiteSpace(entry.IconEffect) ? null : entry.IconEffect.Trim().ToLowerInvariant(),
            Prestige = string.IsNullOrWhiteSpace(entry.Prestige) ? null : entry.Prestige.Trim(),
        };

        if (entry.Colors is { Count: > 1 })
        {
            icon.Colors = [.. entry.Colors];
            icon.Mode = string.Equals(entry.ColorMode, "letters", StringComparison.OrdinalIgnoreCase) ? NameMode.Letters : NameMode.Gradient;
        }
        else if (entry.Color != null)
        {
            icon.Colors = [entry.Color];
        }

        if (entry.Editor != null)
        {
            icon.Source = Enum.TryParse<IconSource>(entry.Editor.Source, true, out var source) ? source : IconSource.Own;
            icon.GameIcon = entry.Editor.GameIcon;
            icon.File = entry.Editor.File;
            icon.Tint = entry.Editor.Tint;
        }
        else if (entry.Icon != null)
        {
            icon.Source = IconSource.File;
            icon.File = entry.Icon;
        }
        else if (entry.IconFrom != null)
        {
            icon.Source = IconSource.Game;
            icon.GameIcon = entry.IconFrom;
        }

        // A new icon has no picture of its own in the game
        if (icon.IsCustom && icon.Source == IconSource.Own)
        {
            icon.Source = IconSource.Game;
            icon.GameIcon = "Default";
        }

        return icon;
    }

    /// <summary>The entry the plugin reads. <paramref name="bakedIcon"/> is set for a recolor.</summary>
    public static Entry FromEditable(EditableIcon icon, string? bakedIcon)
    {
        var entry = new Entry
        {
            Category = icon.Key,
            Name = icon.Name,
            Editor = new EditorInfo { Source = icon.Source.ToString(), GameIcon = icon.GameIcon, File = icon.File, Tint = icon.Tint },
        };

        if (icon.Colors.Count == 1 || (icon.Colors.Count > 1 && icon.Mode == NameMode.Solid))
        {
            entry.Color = icon.Colors[0];
        }
        else if (icon.Colors.Count > 1)
        {
            entry.Color = icon.Colors[0];
            entry.Colors = [.. icon.Colors];
            entry.ColorMode = icon.Mode == NameMode.Letters ? "letters" : "gradient";
        }

        // A moving name; one color can wave, sparkle, rainbow, glitch or bounce too
        entry.Animate = icon.Animate > 0 ? Math.Round(icon.Animate, 2) : null;
        entry.Motion = icon.Animate > 0 ? icon.Motion : null;
        entry.Reverse = icon.Animate > 0 && icon.Reverse ? true : null;

        entry.Outline = icon.Outline;
        entry.Glow = icon.Glow;
        entry.Shadow = icon.Shadow && icon.Glow == null ? true : null;
        entry.Italic = icon.Italic ? true : null;
        entry.Tag = icon.Tag;
        entry.TagColor = icon.Tag != null ? icon.TagColor : null;
        entry.IconEffect = icon.IconEffect;
        entry.Prestige = icon.Prestige;

        if (bakedIcon != null)
        {
            entry.Icon = bakedIcon;
        }
        else if (icon.Source == IconSource.Game)
        {
            entry.IconFrom = icon.GameIcon;
        }
        else if (icon.Source == IconSource.File)
        {
            entry.Icon = icon.File;
        }

        entry.Fps = entry.Icon != null && Math.Abs(icon.Fps - 8) > 0.01 ? Math.Round(icon.Fps, 1) : null;
        return entry;
    }
}

/// <summary>
/// The game's member categories (EMemberCategory) and which ones are safe to hand out.
/// </summary>
public static class Categories
{
    private static readonly (int Value, string Enum, string Label)[] Builtin =
    [
        (0, "Default", "Standard"),
        (1, "Developer", "Developer"),
        (2, "UniqueId", "Edge of Darkness"),
        (4, "Trader", "Trader"),
        (8, "Group", "Group"),
        (0x10, "System", "System"),
        (0x20, "ChatModerator", "Chat moderator"),
        (0x40, "ChatModeratorWithPermanentBan", "Chat moderator (ban)"),
        (0x80, "UnitTest", "Unit test"),
        (0x100, "Sherpa", "Sherpa"),
        (0x200, "Emissary", "Emissary"),
        (0x400, "Unheard", "Unheard"),
    ];

    /// <summary>Icons a profile can be given without breaking it (what the server command allows).</summary>
    public static readonly int[] Grantable = [1, 2, 0x100, 0x200, 0x400];

    /// <summary>Member icons worth listing, editing and borrowing.</summary>
    public static readonly int[] WithIcons = [0, 1, 2, 0x100, 0x200, 0x400];

    public const int FirstCustom = 0x800;

    public static bool IsBuiltin(int value) => Builtin.Any(b => b.Value == value);

    public static string EnumName(int value) => Builtin.FirstOrDefault(b => b.Value == value).Enum ?? value.ToString();

    public static string Label(int value) => Builtin.FirstOrDefault(b => b.Value == value).Label ?? $"Custom {value}";

    public static int? Parse(string text)
    {
        if (int.TryParse(text, out var number))
        {
            return number;
        }

        var match = Builtin.FirstOrDefault(b => b.Enum.Equals(text.Trim(), StringComparison.OrdinalIgnoreCase));
        return match.Enum == null ? null : match.Value;
    }

    public static bool IsValidCustom(int value) => value >= FirstCustom && (value & (value - 1)) == 0;

    public static int NextFreeCustom(IEnumerable<int> used)
    {
        var taken = used.ToHashSet();
        for (var value = FirstCustom; value > 0; value <<= 1)
        {
            if (!taken.Contains(value))
            {
                return value;
            }
        }

        throw new InvalidOperationException("No free icon slots left");
    }
}

/// <summary>"bots" in icons.json, as the plugin reads it (see NamePlate.Client.BotConfig).</summary>
public class BotsConfig
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("share")] public int Share { get; set; } = 100;
    [JsonPropertyName("icons")] public List<string> Icons { get; set; } = [];
    [JsonPropertyName("modes")] public List<string> Modes { get; set; } = ["solid", "gradient", "letters"];
    [JsonPropertyName("palettes")] public List<List<string>> Palettes { get; set; } = [];
    [JsonPropertyName("randomColors")] public bool RandomColors { get; set; } = true;
    [JsonPropertyName("animateChance")] public int AnimateChance { get; set; } = Shared.BotDefaults.AnimateChance;
    [JsonPropertyName("maxSpeed")] public double MaxSpeed { get; set; } = 1.0;
    [JsonPropertyName("motions")] public List<string> Motions { get; set; } = [.. Shared.Motions.All];
    [JsonPropertyName("tagChance")] public int TagChance { get; set; } = Shared.BotDefaults.TagChance;
    [JsonPropertyName("tagColorChance")] public int TagColorChance { get; set; } = Shared.BotDefaults.TagColorChance;
    [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
    [JsonPropertyName("factionColors")] public bool FactionColors { get; set; }
    [JsonPropertyName("effectChance")] public int EffectChance { get; set; } = Shared.BotDefaults.EffectChance;
    [JsonPropertyName("effects")] public List<string> Effects { get; set; } = [.. Shared.NameEffectKinds.All];
    [JsonPropertyName("iconEffectChance")] public int IconEffectChance { get; set; } = Shared.BotDefaults.IconEffectChance;
    [JsonPropertyName("iconEffects")] public List<string> IconEffects { get; set; } = [.. Shared.IconEffectKinds.All];

    // The library and member icons, and the motions, there were at the last save: anything newer
    // starts on (the plugin does the same). Missing in an older file: everything on.
    [JsonPropertyName("knownIcons")] public List<string>? KnownIcons { get; set; }
    [JsonPropertyName("knownMotions")] public List<string>? KnownMotions { get; set; }

    /// <summary>Turns on what's new since the last save, then remembers it all as seen.</summary>
    public void TakeInNew(string[] pool)
    {
        Icons = (KnownIcons == null ? Icons.Concat(pool) : Icons.Concat(pool.Except(KnownIcons)))
            .Distinct().OrderBy(i => i, StringComparer.Ordinal).ToList();
        Motions = (KnownMotions == null ? Shared.Motions.All : Motions.Concat(Shared.Motions.All.Except(KnownMotions))).Distinct().ToList();
        KnownIcons = [.. pool];
        KnownMotions = [.. Shared.Motions.All];
    }

    public Shared.BotRules ToRules() => new()
    {
        Share = Math.Clamp(Share, 0, 100),
        Icons = [.. Icons.OrderBy(i => i, StringComparer.Ordinal)],
        Solid = Modes.Contains("solid"),
        Gradient = Modes.Contains("gradient"),
        Letters = Modes.Contains("letters"),
        Palettes = Palettes.Where(p => p.Count > 0).Select(p => p.ToArray()).ToArray(),
        RandomColors = RandomColors,
        AnimateChance = Math.Clamp(AnimateChance, 0, 100),
        MaxSpeed = MaxSpeed,
        Motions = [.. Motions],
        TagChance = Math.Clamp(TagChance, 0, 100),
        TagColorChance = Math.Clamp(TagColorChance, 0, 100),
        Tags = Tags?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToArray(),
        FactionColors = FactionColors,
        EffectChance = Math.Clamp(EffectChance, 0, 100),
        Effects = [.. Effects],
        IconEffectChance = Math.Clamp(IconEffectChance, 0, 100),
        IconEffects = [.. IconEffects],
    };
}
