using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace NamePlate;

public sealed record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.mybutthasarash.nameplate";
    public string Name { get; init; } = "Name Plate";
    public string Author { get; init; } = "Evgencheg";
    public List<string>? Contributors { get; init; }
    public Version Version { get; init; } = new("1.0.0");
    public Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    // This mod before it was renamed (Edition Customizer / ChangeIcons)
    public List<string>? Incompatibilities { get; init; } = ["com.mybutthasarash.editioncustomizer"];
    public Dictionary<string, Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/JoelHauser/EditionCustomizer";
    public string License { get; init; } = "MIT";
}
