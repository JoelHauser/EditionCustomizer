using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace ChangeIcons.Editor;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var sptPath = FindSpt(e.Args);
        if (sptPath == null)
        {
            var dialog = new OpenFolderDialog { Title = "Pick your SPT folder (the one with EscapeFromTarkov.exe)" };
            if (dialog.ShowDialog() != true || !IsSpt(dialog.FolderName))
            {
                MessageBox.Show("That isn't an SPT folder: it needs EscapeFromTarkov.exe and BepInEx.", "ChangeIcons Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown();
                return;
            }

            sptPath = dialog.FolderName;
        }

        MainWindow = new MainWindow(sptPath);
        MainWindow.Show();
    }

    private static bool IsSpt(string path) =>
        File.Exists(Path.Combine(path, "EscapeFromTarkov.exe")) && Directory.Exists(Path.Combine(path, "BepInEx"));

    // A path argument, else the folder the exe sits in or any above it (it lives in
    // BepInEx\plugins\ChangeIcons)
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

        return null;
    }
}
