namespace ChangeIcons.Editor;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var sptPath = FindSpt(args);
        if (sptPath == null)
        {
            using var dialog = new FolderBrowserDialog { Description = "Pick your SPT folder (the one with EscapeFromTarkov.exe)", UseDescriptionForTitle = true };
            if (dialog.ShowDialog() != DialogResult.OK || !IsSpt(dialog.SelectedPath))
            {
                MessageBox.Show("That isn't an SPT folder: it needs EscapeFromTarkov.exe and BepInEx.", "ChangeIcons Editor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            sptPath = dialog.SelectedPath;
        }

        Application.Run(new MainForm(sptPath));
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
