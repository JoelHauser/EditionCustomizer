using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace ChangeIcons.Editor;

public partial class App : Application
{
    private record EditorSettings(string? SptPath);

    // Per user, outside the SPT folder, so it survives reinstalling the mod
    private static string SettingsPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChangeIcons Editor", "settings.json");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var sptPath = FindSpt(e.Args) ?? PickSpt(null);
        if (sptPath == null)
        {
            Shutdown();
            return;
        }

        Remember(sptPath);
        MainWindow = new MainWindow(sptPath);
        MainWindow.Show();
    }

    /// <summary>
    /// Opens the editor on another SPT folder. The current window closes (asking to save first);
    /// if that's cancelled, nothing changes.
    /// </summary>
    public void SwitchTo(string sptPath)
    {
        var old = MainWindow;
        var next = new MainWindow(sptPath);

        // The new window becomes the main one first, so closing the old doesn't end the app
        MainWindow = next;
        var closed = old == null;
        if (old != null)
        {
            old.Closed += (_, _) => closed = true;
            old.Close();
        }

        if (!closed)
        {
            // Cancelled at "Save your changes?"
            MainWindow = old;
            next.Close();
            return;
        }

        Remember(sptPath);
        next.Show();
    }

    /// <summary>Asks for an SPT folder; null if cancelled or not an SPT folder.</summary>
    public static string? PickSpt(Window? owner)
    {
        var dialog = new OpenFolderDialog { Title = "Pick your SPT folder (the one with EscapeFromTarkov.exe)" };
        var saved = Saved();
        if (saved != null)
        {
            dialog.InitialDirectory = saved;
        }

        if (dialog.ShowDialog(owner) != true)
        {
            return null;
        }

        if (!IsSpt(dialog.FolderName))
        {
            MessageBox.Show("That isn't an SPT folder: it needs EscapeFromTarkov.exe and BepInEx.", "ChangeIcons Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        return dialog.FolderName;
    }

    private static bool IsSpt(string path) =>
        File.Exists(Path.Combine(path, "EscapeFromTarkov.exe")) && Directory.Exists(Path.Combine(path, "BepInEx"));

    // A path argument; else the SPT install the exe sits in (the zip puts it in the SPT folder
    // itself; older versions put it in BepInEx\plugins\ChangeIcons); else the folder picked last time
    private static string? FindSpt(string[] args)
    {
        if (args.Length > 0 && IsSpt(args[0]))
        {
            return args[0];
        }

        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (IsSpt(dir.FullName))
            {
                return dir.FullName;
            }
        }

        return Saved();
    }

    private static string? Saved()
    {
        try
        {
            var path = File.Exists(SettingsPath) ? JsonSerializer.Deserialize<EditorSettings>(File.ReadAllText(SettingsPath))?.SptPath : null;
            return path != null && IsSpt(path) ? path : null;
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static void Remember(string sptPath)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new EditorSettings(sptPath), new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Only a convenience; the editor asks again next time
        }
    }
}
