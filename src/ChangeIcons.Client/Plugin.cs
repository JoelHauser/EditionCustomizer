using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ChangeIcons.Client;

[BepInPlugin("com.evgencheg.changeicons.client", "ChangeIcons.Client", "1.2.0")]
public class Plugin : BaseUnityPlugin
{
    public static ManualLogSource Log;
    public static string Folder;
    public static IconConfig Settings;

    private string _configPath;
    private FileSystemWatcher _watcher;

    // Set from the watcher's thread, acted on in Update: Unity objects are main thread only
    private volatile bool _changed;
    private float _reloadAt;

    private void Awake()
    {
        Log = Logger;
        Folder = Path.GetDirectoryName(Info.Location);
        _configPath = Path.Combine(Folder, "icons.json");

        Settings = LoadConfig() ?? new IconConfig { DumpOriginalIcons = false };
        BotLooks.Configure(Settings.Bots);
        new Harmony("com.evgencheg.changeicons.client").PatchAll(typeof(Plugin).Assembly);

        // The editor saves icons.json and icons\*.png; pick those up without a restart
        _watcher = new FileSystemWatcher(Folder) { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size };
        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Renamed += OnFileChanged;
        _watcher.EnableRaisingEvents = true;
    }

    private IconConfig LoadConfig()
    {
        try
        {
            var config = File.Exists(_configPath) ? IconConfig.Load(_configPath) : new IconConfig();
            Log.LogInfo($"Loaded {config.Icons.Count} icon entr{(config.Icons.Count == 1 ? "y" : "ies")} from {_configPath}");
            return config;
        }
        catch (Exception e)
        {
            // A typo in icons.json should leave the icons as they are, not stop the game
            Log.LogError($"Couldn't read {_configPath}, changing nothing: {e.Message}");
            return null;
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Our own dump writes to originals\; that isn't a change to apply
        var relative = e.FullPath.Substring(Folder.Length).TrimStart('\\', '/');
        if (relative.Equals("icons.json", StringComparison.OrdinalIgnoreCase)
            || relative.StartsWith("icons" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            _changed = true;
        }
    }

    private void Update()
    {
        if (_changed)
        {
            // Wait for the burst of events one save makes to settle
            _changed = false;
            _reloadAt = Time.unscaledTime + 0.5f;
        }

        if (_reloadAt > 0 && Time.unscaledTime >= _reloadAt)
        {
            _reloadAt = 0;
            var config = LoadConfig();
            if (config != null)
            {
                Settings = config;
                BotLooks.Configure(config.Bots);
                IconTable.Reload();
                Log.LogInfo("Reloaded icons.json. Reopen a screen to see the change.");
            }
        }
    }

    private void OnDestroy() => _watcher?.Dispose();
}
