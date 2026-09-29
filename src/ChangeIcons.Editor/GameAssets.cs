using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Media.Imaging;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;

namespace ChangeIcons.Editor;

/// <summary>
/// The game's icons, read straight out of EscapeFromTarkov_Data\resources.assets: every sprite
/// as a PNG in the plugin's game-icons folder (so the plugin can load any of them), plus the
/// member icon table with the game's names and colors.
/// </summary>
public class GameAssets
{
    public record Sprite(string Name, string File, int Width, int Height);

    public record MemberRow(string Name, int Category, string? Sprite, string Color);

    private record Index(string Source, long SourceSize, DateTime SourceTime, List<Sprite> Sprites, List<MemberRow> Members, List<int> Priority);

    /// <summary>Sprites bigger than this are backgrounds and banners, not icons.</summary>
    public const int MaxIconSize = 512;

    private readonly string _resources;
    private readonly string _folder;
    private Index? _index;

    public GameAssets(string sptPath, string pluginFolder)
    {
        _resources = Path.Combine(sptPath, "EscapeFromTarkov_Data", "resources.assets");
        _folder = Path.Combine(pluginFolder, "game-icons");
        LoadIndex();
    }

    public string Folder => _folder;

    public IReadOnlyList<Sprite> Sprites => _index?.Sprites ?? [];

    public IReadOnlyList<MemberRow> Members => _index?.Members ?? [];

    public bool Extracted => _index != null;

    /// <summary>The game was updated since the icons were read.</summary>
    public bool Stale
    {
        get
        {
            if (_index == null || !File.Exists(_resources))
            {
                return false;
            }

            var info = new FileInfo(_resources);
            return info.Length != _index.SourceSize || info.LastWriteTimeUtc != _index.SourceTime;
        }
    }

    public MemberRow? Member(int category) => Members.FirstOrDefault(m => m.Category == category);

    /// <summary>Path of a member category's own icon, relative to the plugin folder.</summary>
    public string? MemberIconFile(int category)
    {
        var sprite = Member(category)?.Sprite;
        var match = sprite == null ? null : Sprites.FirstOrDefault(s => s.Name == sprite);
        return match == null ? null : "game-icons/" + match.File;
    }

    private void LoadIndex()
    {
        var path = Path.Combine(_folder, "index.json");
        try
        {
            _index = File.Exists(path) ? JsonSerializer.Deserialize<Index>(File.ReadAllText(path)) : null;
        }
        catch (JsonException)
        {
            _index = null;
        }
    }

    /// <summary>
    /// Reads every sprite and the member table. Runs in a second or two; call off the UI thread.
    /// </summary>
    public void Extract(IProgress<string>? progress = null)
    {
        if (!File.Exists(_resources))
        {
            throw new FileNotFoundException("The game's resources.assets isn't there", _resources);
        }

        var manager = new AssetsManager();
        using (var tpk = typeof(GameAssets).Assembly.GetManifestResourceStream("classdata.tpk")!)
        {
            manager.LoadClassPackage(tpk);
        }

        try
        {
            var file = manager.LoadAssetsFile(_resources, false);
            manager.LoadClassDatabaseFromPackage(file.file.Metadata.UnityVersion);
            Directory.CreateDirectory(_folder);

            // Sprite path id -> name, for the member table's sprite references
            var spriteNames = new Dictionary<long, string>();
            var sprites = new List<Sprite>();
            var usedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var textures = new Dictionary<long, (byte[] Bgra, int Width, int Height)?>();
            var all = file.file.GetAssetsOfType(AssetClassID.Sprite);
            var done = 0;
            var failed = 0;

            foreach (var info in all)
            {
                if (++done % 100 == 0)
                {
                    progress?.Report($"Reading game icons... {done} / {all.Count}");
                }

                var sprite = manager.GetBaseField(file, info);
                var name = sprite["m_Name"].AsString;
                spriteNames[info.PathId] = name;

                var rd = sprite["m_RD"];
                var rect = rd["textureRect"];
                var width = (int)Math.Round(rect["width"].AsFloat);
                var height = (int)Math.Round(rect["height"].AsFloat);
                var texture = rd["texture"];
                if (width <= 0 || height <= 0 || width > MaxIconSize || height > MaxIconSize
                    || texture["m_FileID"].AsInt != 0 || texture["m_PathID"].AsLong == 0)
                {
                    continue;
                }

                var pathId = texture["m_PathID"].AsLong;
                if (!textures.TryGetValue(pathId, out var decoded))
                {
                    decoded = Decode(manager, file, pathId);
                    textures[pathId] = decoded;
                }

                if (decoded is not { } tex)
                {
                    continue;
                }

                var fileName = FileNameFor(name, info.PathId, usedFiles);
                var pixels = Crop(tex.Bgra, tex.Width, tex.Height, (int)rect["x"].AsFloat, (int)rect["y"].AsFloat, width, height);
                try
                {
                    Images.SavePng(Images.FromBgra(pixels, width, height), Path.Combine(_folder, fileName));
                    sprites.Add(new Sprite(name, fileName, width, height));
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    // One file that can't be written costs that icon, not the other thousand
                    failed++;
                }
            }

            if (failed > 0)
            {
                progress?.Report($"{failed} icon(s) couldn't be saved and were left out");
            }

            progress?.Report("Reading the member icon table...");
            var (members, priority) = ReadMemberTable(file, spriteNames);

            var source = new FileInfo(_resources);
            _index = new Index(_resources, source.Length, source.LastWriteTimeUtc, sprites.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList(), members, priority);
            File.WriteAllText(Path.Combine(_folder, "index.json"), JsonSerializer.Serialize(_index, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            manager.UnloadAll();
        }
    }

    private static (byte[] Bgra, int Width, int Height)? Decode(AssetsManager manager, AssetsFileInstance file, long pathId)
    {
        try
        {
            var field = manager.GetBaseField(file, pathId);
            var texture = TextureFile.ReadTextureFile(field);
            var data = texture.DecodeTextureRaw(texture.FillPictureData(file));
            return data == null ? null : (data, texture.m_Width, texture.m_Height);
        }
        catch (Exception)
        {
            // A handful use formats the decoder doesn't know (Crunch); leave those out
            return null;
        }
    }

    // Unity stores rows bottom-up; the PNG wants them top-down
    private static byte[] Crop(byte[] bgra, int texWidth, int texHeight, int x, int y, int width, int height)
    {
        var result = new byte[width * height * 4];
        for (var row = 0; row < height; row++)
        {
            var sourceRow = y + (height - 1 - row);
            if (sourceRow < 0 || sourceRow >= texHeight)
            {
                continue;
            }

            Buffer.BlockCopy(bgra, (sourceRow * texWidth + x) * 4, result, row * width * 4, Math.Min(width, texWidth - x) * 4);
        }

        return result;
    }

    private static string FileNameFor(string name, long pathId, HashSet<string> used)
    {
        var safe = new string(name.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '_').ToArray()).Trim('_', '.');
        if (safe.Length == 0)
        {
            safe = "sprite";
        }

        var fileName = safe + ".png";
        if (!used.Add(fileName))
        {
            fileName = $"{safe}_{pathId}.png";
            used.Add(fileName);
        }

        return fileName;
    }

    /// <summary>
    /// ChatSpecialIconSettings has no type information in the file, so it's read by hand in the
    /// order the class declares its fields: Priority, then IconsSettings (Name, Category,
    /// IconSprite, IconColor).
    /// </summary>
    private static (List<MemberRow>, List<int>) ReadMemberTable(AssetsFileInstance file, Dictionary<long, string> spriteNames)
    {
        var marker = Encoding.UTF8.GetBytes("ChatSpecialIconSettings");
        var reader = file.file.Reader;

        foreach (var info in file.file.GetAssetsOfType(AssetClassID.MonoBehaviour))
        {
            reader.Position = info.GetAbsoluteByteOffset(file.file);
            var raw = reader.ReadBytes((int)info.ByteSize);
            if (raw.AsSpan(0, Math.Min(raw.Length, 256)).IndexOf(marker) < 0)
            {
                continue;
            }

            try
            {
                var r = new RawReader(raw);
                r.Skip(4 + 8); // m_GameObject
                r.Skip(4); // m_Enabled, aligned
                r.Skip(4 + 8); // m_Script
                if (r.String() != "ChatSpecialIconSettings")
                {
                    continue;
                }

                var priority = Enumerable.Range(0, r.Int()).Select(_ => r.Int()).ToList();
                var rows = new List<MemberRow>();
                var count = r.Int();
                for (var i = 0; i < count; i++)
                {
                    var name = r.String();
                    var category = r.Int();
                    r.Skip(4);
                    var spriteId = r.Long();
                    var color = new[] { r.Float(), r.Float(), r.Float(), r.Float() };
                    var hex = "#" + string.Concat(color.Take(3).Select(c => ((int)Math.Round(Math.Clamp(c, 0, 1) * 255)).ToString("X2")));
                    rows.Add(new MemberRow(name, category, spriteNames.GetValueOrDefault(spriteId), hex));
                }

                return (rows, priority);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Layout changed in a game update; the editor falls back to its own names
            }
        }

        return ([], []);
    }

    private class RawReader(byte[] data)
    {
        private int _pos;

        public void Skip(int bytes) => _pos += bytes;

        public int Int()
        {
            var v = BitConverter.ToInt32(data, _pos);
            _pos += 4;
            return v;
        }

        public long Long()
        {
            var v = BitConverter.ToInt64(data, _pos);
            _pos += 8;
            return v;
        }

        public float Float()
        {
            var v = BitConverter.ToSingle(data, _pos);
            _pos += 4;
            return v;
        }

        public string String()
        {
            var length = Int();
            if (length < 0 || length > data.Length - _pos)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            var s = Encoding.UTF8.GetString(data, _pos, length);
            _pos = (_pos + length + 3) & ~3;
            return s;
        }
    }
}
