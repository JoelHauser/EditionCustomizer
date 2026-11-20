using System;
using EFT.UI;
using HarmonyLib;

namespace NamePlate.Client;

/// <summary>
/// The Developer icon is a real flag on the profile, and EFT gives Developer profiles one thing a
/// player notices: the console pops open on every error or exception (ConsoleScreen.OnUnityLog ->
/// PullUpConsole, which shows only for Developer). Wearing the icon shouldn't do that, so it's
/// skipped unless the config asks for it. Opening the console yourself is a different path and
/// still works. Forge issue #4.
/// </summary>
[HarmonyPatch(typeof(ConsoleScreen), nameof(ConsoleScreen.PullUpConsole))]
public static class DeveloperConsolePatch
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        try
        {
            return Plugin.DeveloperConsolePopup?.Value == true;
        }
        catch (Exception e)
        {
            Safe.Report("deciding whether errors open the console", e);
            return true;
        }
    }
}
