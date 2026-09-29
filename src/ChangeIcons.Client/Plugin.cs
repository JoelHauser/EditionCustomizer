using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace ChangeIcons.Client;

[BepInPlugin("com.evgencheg.changeicons.client", "ChangeIcons.Client", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    public static ManualLogSource Log;
    public static string Folder;
    public static IconConfig Settings;

    private void Awake()
    {
        Log = Logger;
        Folder = Path.GetDirectoryName(Info.Location);

        var configPath = Path.Combine(Folder, "icons.json");
        try
        {
            Settings = File.Exists(configPath) ? IconConfig.Load(configPath) : new IconConfig();
            Log.LogInfo($"Loaded {Settings.Icons.Count} icon entr{(Settings.Icons.Count == 1 ? "y" : "ies")} from {configPath}");
        }
        catch (Exception e)
        {
            // A typo in icons.json should leave the game's icons alone, not stop the game
            Settings = new IconConfig { DumpOriginalIcons = false };
            Log.LogError($"Couldn't read {configPath}, changing nothing: {e.Message}");
        }

        new Harmony("com.evgencheg.changeicons.client").PatchAll(typeof(Plugin).Assembly);
    }
}
