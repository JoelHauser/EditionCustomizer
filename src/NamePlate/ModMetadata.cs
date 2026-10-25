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
    public Version Version { get; init; } = new(ModVersion.Value);
    public Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    // None: SPT loads no mods at all when one is found. Sharing the chat command with ChangeIcons
    // or this mod before its rename is handled in CommandName instead.
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/JoelHauser/EditionCustomizer";
    public string License { get; init; } = "MIT";
}
