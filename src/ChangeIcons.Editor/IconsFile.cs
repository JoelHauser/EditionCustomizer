using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChangeIcons.Editor;

public enum IconSource
{
    /// <summary>The category's own game icon (built-in categories only).</summary>
    Own,

    /// <summary>Another game icon, borrowed.</summary>
    Game,

    /// <summary>A PNG in the plugin's icons folder.</summary>
    File,
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
    public string? Color;
    public IconSource Source;
    public string? GameIcon;
    public string? File;
    public string? Tint;

    public bool IsChanged => IsCustom || Name != null || Color != null || Source != IconSource.Own || Tint != null;
}

public class IconsFile
{
    [JsonPropertyName("dumpOriginalIcons")]
    public bool DumpOriginalIcons { get; set; } = true;

    [JsonPropertyName("icons")]
    public List<Entry> Icons { get; set; } = [];

    public class Entry
    {
        [JsonPropertyName("category")] public string Category { get; set; } = "";
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("color")] public string? Color { get; set; }
        [JsonPropertyName("icon")] public string? Icon { get; set; }
        [JsonPropertyName("iconFrom")] public string? IconFrom { get; set; }
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
    };

    public static IconsFile Load(string path) =>
        System.IO.File.Exists(path) ? JsonSerializer.Deserialize<IconsFile>(System.IO.File.ReadAllText(path), Options) ?? new() : new();

    public void Save(string path)
    {
        var temp = path + ".tmp";
        System.IO.File.WriteAllText(temp, JsonSerializer.Serialize(this, Options));
        System.IO.File.Move(temp, path, overwrite: true);
    }

    public static EditableIcon ToEditable(Entry entry)
    {
        var value = Categories.Parse(entry.Category) ?? throw new FormatException($"\"{entry.Category}\" isn't a category");
        var icon = new EditableIcon { Value = value, Name = entry.Name, Color = entry.Color };

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

    /// <summary>Game icons worth offering to recolor or borrow.</summary>
    public static readonly string[] WithIcons = ["Default", "Developer", "UniqueId", "Sherpa", "Emissary", "Unheard"];

    public const int FirstCustom = 0x800;

    public static bool IsBuiltin(int value) => Builtin.Any(b => b.Value == value);

    public static string EnumName(int value) => Builtin.FirstOrDefault(b => b.Value == value).Enum ?? value.ToString();

    public static string Label(int value) => Builtin.FirstOrDefault(b => b.Value == value).Label ?? $"Custom {value}";

    public static string Label(string enumName) => Builtin.FirstOrDefault(b => b.Enum.Equals(enumName, StringComparison.OrdinalIgnoreCase)).Label ?? enumName;

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
