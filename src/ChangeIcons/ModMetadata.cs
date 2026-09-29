using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace ChangeIcons;

public sealed record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.evgencheg.changeicons";
    public string Name { get; init; } = "ChangeIcons";
    public string Author { get; init; } = "Evgencheg";
    public List<string>? Contributors { get; init; }
    public Version Version { get; init; } = new("1.1.0");
    public Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/Evgencheg/Tarkov-Change-Icons";
    public string License { get; init; } = "MIT";
}
