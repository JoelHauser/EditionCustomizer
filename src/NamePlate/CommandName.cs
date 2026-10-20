using SPTarkov.Server.Core.Models.Spt.Mod;

namespace NamePlate;

/// <summary>
/// The chat command's name. Evgencheg's ChangeIcons (the mod this one grew out of) and this mod
/// before its rename also register "spt membercategory", and SPT stops the server when two
/// commands share a name. With either of them loaded, this mod's command is "spt nameplate".
/// Not declared incompatible instead: SPT then loads no mods at all, which on a modded profile
/// is worse than the crash.
/// </summary>
public static class CommandName
{
    public const string Default = "membercategory";
    public const string Fallback = "nameplate";

    private static readonly string[] SameCommand = ["com.evgencheg.changeicons", "com.mybutthasarash.editioncustomizer"];

    /// <summary>The loaded mod that has "spt membercategory", if there is one.</summary>
    public static IModMetadata? Taken(IEnumerable<SptMod> loadedMods) =>
        loadedMods.Select(m => m.ModMetadata).FirstOrDefault(m => SameCommand.Contains(m.ModGuid, StringComparer.OrdinalIgnoreCase));

    public static string For(IEnumerable<SptMod> loadedMods) => Taken(loadedMods) == null ? Default : Fallback;
}
