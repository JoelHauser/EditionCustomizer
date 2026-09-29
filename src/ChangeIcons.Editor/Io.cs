using System.IO;

namespace ChangeIcons.Editor;

public static class Io
{
    /// <summary>
    /// Writes <paramref name="text"/> next to <paramref name="path"/> and swaps it in, so a crash
    /// never leaves half a file. The swap is retried briefly: a virus scanner looking at the new
    /// file can hold it for a moment.
    /// </summary>
    public static void ReplaceText(string path, string text)
    {
        var temp = path + ".tmp";
        File.WriteAllText(temp, text);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                File.Move(temp, path, overwrite: true);
                return;
            }
            catch (IOException) when (attempt < 5)
            {
                Thread.Sleep(100 * attempt);
            }
        }
    }
}
