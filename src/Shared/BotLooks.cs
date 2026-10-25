#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;

// Compiled into both the plugin (net472, the game's Mono) and the editor (.NET 10), so the
// editor's preview is exactly what the game draws. Keep it to APIs both have, and keep every
// random draw in the same order: a bot's look is the sequence of draws seeded by its name.
namespace NamePlate.Shared
{
    /// <summary>The ways a name can move. Any of them can also run in reverse.</summary>
    public static class Motions
    {
        /// <summary>The colors travel along the name (gradient) or step letter to letter.</summary>
        public const string Scroll = "scroll";

        /// <summary>The whole name cycles through its colors (gradient); letters blend into the next color.</summary>
        public const string Pulse = "pulse";

        /// <summary>The gradient ripples; letters breathe brighter and darker in a wave.</summary>
        public const string Wave = "wave";

        /// <summary>Letters flash white now and then.</summary>
        public const string Sparkle = "sparkle";

        /// <summary>Every hue in turn, sweeping across the name; the name's own colors aren't used.</summary>
        public const string Rainbow = "rainbow";

        /// <summary>Letters now and then jump aside and flash another of the name's colors.</summary>
        public const string Glitch = "glitch";

        /// <summary>Letters hop up one after another.</summary>
        public const string Bounce = "bounce";

        public static readonly string[] All = { Scroll, Pulse, Wave, Sparkle, Rainbow, Glitch, Bounce };

        /// <summary>Those that show on a one-color name: the others need two colors to move between.</summary>
        public static readonly string[] OneColor = { Wave, Sparkle, Rainbow, Glitch, Bounce };

        /// <summary>Those that move the letters, not only their colors.</summary>
        public static bool MovesLetters(string motion) => motion == Glitch || motion == Bounce;
    }

    /// <summary>Looks on a name's letters, as icons.json and the bots section name them.</summary>
    public static class NameEffectKinds
    {
        public const string Outline = "outline";
        public const string Glow = "glow";
        public const string Shadow = "shadow";
        public const string Bold = "bold";
        public const string Italic = "italic";
        public const string SmallCaps = "smallcaps";

        public static readonly string[] All = { Outline, Glow, Shadow, Bold, Italic, SmallCaps };
    }

    /// <summary>A tag's color: a "#RRGGBB" of its own, this word for the name's colors, or none for gray.</summary>
    public static class TagColors
    {
        public const string Name = "name";
    }

    /// <summary>Movements or a halo on an icon itself.</summary>
    public static class IconEffectKinds
    {
        public static readonly string[] All = { "pulse", "glow", "flip", "float" };
    }

    /// <summary>What PMC bots may be given. Mapped from the "bots" section of icons.json.</summary>
    public sealed class BotRules
    {
        /// <summary>Percent of PMC bots that get a look at all.</summary>
        public int Share = 100;

        /// <summary>"icons/library/skull.png" style paths, or "member:Unheard".</summary>
        public string[] Icons = new string[0];

        public bool Solid = true;
        public bool Gradient = true;
        public bool Letters = true;

        /// <summary>Preset color lists to pick from.</summary>
        public string[][] Palettes = new string[0][];

        /// <summary>Also make up colors, not only presets.</summary>
        public bool RandomColors = true;

        /// <summary>Percent of names that move.</summary>
        public int AnimateChance = BotDefaults.AnimateChance;

        public double MaxSpeed = 1.0;

        /// <summary>Which of <see cref="Shared.Motions"/> they may use.</summary>
        public string[] Motions = Shared.Motions.All;

        /// <summary>Percent of bots with a look that also get a clan tag before their name.</summary>
        public int TagChance = BotDefaults.TagChance;

        /// <summary>The tags to pick from; null for the built-in <see cref="BotTags.All"/>.</summary>
        public string[] Tags;

        /// <summary>Percent of tagged bots whose tag is colored rather than gray.</summary>
        public int TagColorChance = BotDefaults.TagColorChance;

        /// <summary>
        /// BEAR bots draw warm colors, USEC bots cool ones (where the game says which side a bot is
        /// on: the death screen, the kill list, dogtags).
        /// </summary>
        public bool FactionColors;

        /// <summary>Percent of bots with a look whose name also gets one of <see cref="Effects"/>.</summary>
        public int EffectChance = BotDefaults.EffectChance;

        /// <summary>Which of <see cref="NameEffectKinds"/> they may get; null for all.</summary>
        public string[] Effects;

        /// <summary>Percent of bots with an icon from the pool whose icon also moves or glows.</summary>
        public int IconEffectChance = BotDefaults.IconEffectChance;

        /// <summary>Which of <see cref="IconEffectKinds"/> they may get; null for all.</summary>
        public string[] IconEffects;
    }

    public sealed class Preset
    {
        public string Name;
        public string[] Colors;

        public Preset(string name, params string[] colors)
        {
            Name = name;
            Colors = colors;
        }
    }

    /// <summary>
    /// What PMC bots get when icons.json has no "bots" section: every library and member icon,
    /// every preset, every way of coloring. The editor starts from the same, so its preview holds.
    /// </summary>
    public static class BotDefaults
    {
        public static readonly Preset[] Presets =
        {
            new Preset("Rainbow", "#FF4D4D", "#FF9F1C", "#FFE14D", "#5BE36B", "#3DB9FF", "#8B5CFF", "#FF5FCB"),
            new Preset("Fire", "#FFE27A", "#FF9A2E", "#FF4B1F", "#B3120E"),
            new Preset("Ice", "#FFFFFF", "#B8F1FF", "#5CC8FF", "#2E7BFF"),
            new Preset("Toxic", "#F2FF6B", "#8CE83A", "#2BAF4A", "#0F7A3A"),
            new Preset("Gold", "#FFF4C2", "#F2C94C", "#C8912A", "#8C5A12"),
            new Preset("Blood", "#FF6B6B", "#D62828", "#7A0C0C"),
            new Preset("Neon", "#FF2BD6", "#8B3DFF", "#2BE3FF"),
            new Preset("Sunset", "#FFC46B", "#FF6F61", "#C94FD8", "#5B3FD1"),
            new Preset("Unheard", "#7FF3FF", "#55D0E6", "#2C7FE0"),
            new Preset("Tarkov", "#DCD7CA", "#C9B77F", "#8C7B55"),
            new Preset("Ocean", "#00E0C6", "#0098D8", "#1D4ED8"),
            new Preset("Candy", "#FF9BD2", "#FFFFFF", "#9BD8FF"),
            new Preset("Vaporwave", "#FF71CE", "#01CDFE", "#05FFA1", "#B967FF"),
            new Preset("Forest", "#B8E986", "#5FA55A", "#2E6B3F"),
            new Preset("Lava", "#FFD166", "#FF6B35", "#C1121F", "#7A1010"),
            new Preset("Mint", "#E0FFF4", "#7DF9C5", "#2EC4B6"),
            new Preset("Royal", "#F5D76E", "#9B5DE5", "#5B3CC4"),
            new Preset("Rose", "#FFE3EC", "#FF8FAB", "#C9184A"),
            new Preset("Camo", "#C2B280", "#8F9B62", "#5B6B32"),
            new Preset("Arctic", "#E8F7FF", "#9AD1F5", "#4A90C2"),
            new Preset("Bubblegum", "#FFC8DD", "#FFAFCC", "#BDE0FE", "#A2D2FF"),
            new Preset("Midnight", "#A5B4FC", "#6366F1", "#4338CA"),
            new Preset("Copper", "#F6C177", "#D08C60", "#9C4F2E"),
            new Preset("Glitch", "#00FF9C", "#FF00E1", "#00B3FF", "#FFFFFF"),
        };

        /// <summary>The member icons that are safe on anyone (no trader or system ones).</summary>
        public static readonly string[] MemberIcons =
        {
            "member:Developer", "member:UniqueId", "member:Sherpa", "member:Emissary", "member:Unheard",
        };

        public const int AnimateChance = 50;
        public const double MaxSpeed = 1.0;
        public const int TagChance = 35;
        public const int TagColorChance = 40;
        public const int EffectChance = 25;
        public const int IconEffectChance = 15;

        /// <summary>
        /// The icon pool: <paramref name="libraryIcons"/> ("icons/library/skull.png" ...) plus the
        /// member icons, in ordinal order so plugin and editor pick the same one for a name.
        /// </summary>
        public static string[] Icons(IEnumerable<string> libraryIcons)
        {
            var all = new List<string>(libraryIcons);
            all.AddRange(MemberIcons);
            all.Sort(StringComparer.Ordinal);
            return all.ToArray();
        }

        public static BotRules Rules(IEnumerable<string> libraryIcons)
        {
            var palettes = new string[Presets.Length][];
            for (var i = 0; i < Presets.Length; i++)
            {
                palettes[i] = Presets[i].Colors;
            }

            return new BotRules
            {
                Icons = Icons(libraryIcons),
                Palettes = palettes,
                AnimateChance = AnimateChance,
                MaxSpeed = MaxSpeed,
                TagChance = TagChance,
                EffectChance = EffectChance,
                IconEffectChance = IconEffectChance,
            };
        }
    }

    public sealed class BotLook
    {
        public string[] Colors;

        /// <summary>"solid", "gradient" or "letters".</summary>
        public string Mode;

        /// <summary>0 for still.</summary>
        public double Speed;

        /// <summary>One of <see cref="Shared.Motions"/>, or null for still.</summary>
        public string Motion;

        public bool Reverse;

        /// <summary>An entry of <see cref="BotRules.Icons"/>, or null for the bot's own icon.</summary>
        public string Icon;

        /// <summary>A clan tag shown before the name, or null.</summary>
        public string Tag;

        /// <summary>The tag's color: "#RRGGBB", <see cref="TagColors.Name"/>, or null for gray.</summary>
        public string TagColor;

        /// <summary>One of <see cref="NameEffectKinds"/>, or null.</summary>
        public string Effect;

        /// <summary>The outline's or glow's color, "#RRGGBB"; null for the others.</summary>
        public string EffectColor;

        /// <summary>One of <see cref="IconEffectKinds"/>, or null.</summary>
        public string IconEffect;
    }

    public static class BotLookGenerator
    {
        public const string MemberPrefix = "member:";

        public const string Bear = "bear";
        public const string Usec = "usec";

        /// <param name="side"><see cref="Bear"/>, <see cref="Usec"/> or null when it isn't known.</param>
        public static BotLook For(string name, BotRules rules, string side = null)
        {
            if (rules == null || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(name.Trim()))
            {
                return null;
            }

            var rng = new Rng(Hash(name.Trim()));
            if (rng.Next(100) >= rules.Share)
            {
                return null;
            }

            var modes = new List<string>();
            if (rules.Solid)
            {
                modes.Add("solid");
            }

            if (rules.Gradient)
            {
                modes.Add("gradient");
            }

            if (rules.Letters)
            {
                modes.Add("letters");
            }

            if (modes.Count == 0)
            {
                return null;
            }

            var look = new BotLook { Mode = modes[rng.Next(modes.Count)] };
            var palettes = rules.Palettes ?? new string[0][];
            var faction = rules.FactionColors && (side == Bear || side == Usec) ? side : null;
            if (faction != null)
            {
                var own = new List<string[]>();
                foreach (var p in palettes)
                {
                    if (IsWarm(p) == (faction == Bear))
                    {
                        own.Add(p);
                    }
                }

                palettes = own.ToArray();
            }

            var usePreset = palettes.Length > 0 && (!rules.RandomColors || rng.Next(2) == 0);

            if (look.Mode == "solid")
            {
                if (usePreset)
                {
                    var palette = palettes[rng.Next(palettes.Length)];
                    look.Colors = new[] { palette[rng.Next(palette.Length)] };
                }
                else
                {
                    look.Colors = new[] { RandomScheme(rng, faction)[0] };
                }
            }
            else
            {
                look.Colors = usePreset ? palettes[rng.Next(palettes.Length)] : RandomScheme(rng, faction);
                if (look.Colors.Length < 2)
                {
                    look.Colors = new[] { look.Colors[0], RandomScheme(rng, faction)[0] };
                }
            }

            // One-color names can only wave or sparkle; the rest need colors to move between
            var allowed = new List<string>();
            foreach (var motion in rules.Motions ?? Motions.All)
            {
                if (look.Mode != "solid" || Array.IndexOf(Motions.OneColor, motion) >= 0)
                {
                    allowed.Add(motion);
                }
            }

            if (allowed.Count > 0 && rng.Next(100) < rules.AnimateChance)
            {
                var max = Math.Max(0.1, rules.MaxSpeed);
                look.Motion = allowed[rng.Next(allowed.Count)];
                look.Speed = Math.Round(0.1 + rng.NextDouble() * (max - 0.1), 2);
                look.Reverse = rng.Next(2) == 1;
            }

            // The draw stays, so everything drawn after it is where it was; the icon itself is the
            // pool's best match for the name (see PickIcon)
            var icons = rules.Icons ?? new string[0];
            if (icons.Length > 0)
            {
                rng.Next(icons.Length);
                look.Icon = PickIcon(name.Trim(), icons);
            }

            // The extras come last, each drawing the same numbers whether it fires or not, so turning
            // one on or off, or up or down, never changes the rest of a look
            var tags = rules.Tags ?? BotTags.All;
            var tagRoll = rng.Next(100);
            var tagPick = rng.Next(Math.Max(1, tags.Length));
            if (tags.Length > 0 && tagRoll < rules.TagChance)
            {
                look.Tag = BotTags.Clean(tags[tagPick]);
            }

            var effects = rules.Effects ?? NameEffectKinds.All;
            var effectRoll = rng.Next(100);
            var effectPick = rng.Next(Math.Max(1, effects.Length));
            var colorPick = rng.Next(look.Colors.Length);
            if (effects.Length > 0 && effectRoll < rules.EffectChance)
            {
                look.Effect = effects[effectPick];
                look.EffectColor = look.Effect == NameEffectKinds.Glow ? look.Colors[colorPick]
                    : look.Effect == NameEffectKinds.Outline ? "#000000"
                    : null;
            }

            var iconEffects = rules.IconEffects ?? IconEffectKinds.All;
            var iconRoll = rng.Next(100);
            var iconPick = rng.Next(Math.Max(1, iconEffects.Length));
            if (look.Icon != null && iconEffects.Length > 0 && iconRoll < rules.IconEffectChance)
            {
                look.IconEffect = iconEffects[iconPick];
            }

            // A colored tag: one of the name's colors, or the name's colors running through it
            var tagColorRoll = rng.Next(100);
            var tagColorPick = rng.Next(look.Colors.Length + 1);
            if (look.Tag != null && tagColorRoll < rules.TagColorChance)
            {
                look.TagColor = tagColorPick == look.Colors.Length ? TagColors.Name : look.Colors[tagColorPick];
            }

            return look;
        }

        /// <summary>A palette leans warm (reds to yellows, BEAR) or cool (greens to purples, USEC).</summary>
        public static bool IsWarm(string[] palette)
        {
            double x = 0, y = 0;
            foreach (var hex in palette)
            {
                var rgb = NamePaint.Parse(hex);
                if (rgb == null)
                {
                    continue;
                }

                var hue = Hue(rgb[0], rgb[1], rgb[2]);
                x += Math.Cos(hue * Math.PI / 180);
                y += Math.Sin(hue * Math.PI / 180);
            }

            var mean = Wrap(Math.Atan2(y, x) * 180 / Math.PI);
            return mean < 75 || mean >= 300;
        }

        private static double Hue(double r, double g, double b)
        {
            var max = Math.Max(r, Math.Max(g, b));
            var d = max - Math.Min(r, Math.Min(g, b));
            if (d <= 0)
            {
                return 0;
            }

            var h = max == r ? (g - b) / d % 6 : max == g ? (b - r) / d + 2 : (r - g) / d + 4;
            return Wrap(h * 60);
        }

        /// <summary>
        /// The icon scoring highest for this name (each icon scored by a hash of name and icon).
        /// Adding icons to the pool only moves the names a new icon now wins, and taking one out
        /// only moves the names it had; picking by index moved every bot whenever the pool changed.
        /// </summary>
        public static string PickIcon(string name, string[] icons)
        {
            string best = null;
            var bestScore = 0UL;
            foreach (var icon in icons)
            {
                // FNV-1a alone barely stirs the last characters, where icon paths differ; the
                // SplitMix64 finish spreads them over every bit
                var score = (ulong)Hash(name + "\n" + icon);
                score = (score ^ (score >> 30)) * 0xBF58476D1CE4E5B9UL;
                score = (score ^ (score >> 27)) * 0x94D049BB133111EBUL;
                score ^= score >> 31;
                if (best == null || score > bestScore || (score == bestScore && string.CompareOrdinal(icon, best) < 0))
                {
                    best = icon;
                    bestScore = score;
                }
            }

            return best;
        }

        /// <summary>FNV-1a over the name's UTF-16 code units: the same on every runtime.</summary>
        public static uint Hash(string text)
        {
            var hash = 2166136261u;
            foreach (var ch in text)
            {
                hash ^= ch;
                hash *= 16777619u;
            }

            return hash;
        }

        /// <summary>
        /// Made-up colors that belong together: neighbouring hues, opposites, a triad, shades of one
        /// hue, pastels or neons -- two to five of them.
        /// </summary>
        private static string[] RandomScheme(Rng rng, string faction)
        {
            var kind = rng.Next(6);

            // The same draw either way; a faction squeezes it into its own range of hues
            var hue = rng.NextDouble() * (faction == null ? 360 : 110) + (faction == Bear ? -35 : faction == Usec ? 160 : 0);
            var count = 2 + rng.Next(4);
            var colors = new string[count];

            for (var i = 0; i < count; i++)
            {
                double h, s, v;
                switch (kind)
                {
                    case 0: // neighbouring hues
                        h = hue + i * (25 + rng.NextDouble() * 35);
                        s = 0.6 + 0.35 * rng.NextDouble();
                        v = 0.8 + 0.2 * rng.NextDouble();
                        break;
                    case 1: // opposites, alternating
                        h = hue + (i % 2) * 180 + (rng.NextDouble() - 0.5) * 30;
                        s = 0.55 + 0.4 * rng.NextDouble();
                        v = 0.8 + 0.2 * rng.NextDouble();
                        break;
                    case 2: // a triad
                        h = hue + (i % 3) * 120 + (rng.NextDouble() - 0.5) * 20;
                        s = 0.6 + 0.35 * rng.NextDouble();
                        v = 0.85 + 0.15 * rng.NextDouble();
                        break;
                    case 3: // shades of one hue, light to deep
                        h = hue + (rng.NextDouble() - 0.5) * 16;
                        s = 0.35 + 0.6 * i / Math.Max(1, count - 1);
                        v = 1.0 - 0.35 * i / Math.Max(1, count - 1);
                        break;
                    case 4: // pastels
                        h = hue + i * (30 + rng.NextDouble() * 50);
                        s = 0.22 + 0.2 * rng.NextDouble();
                        v = 0.95 + 0.05 * rng.NextDouble();
                        break;
                    default: // neons
                        h = hue + i * (90 + rng.NextDouble() * 60);
                        s = 0.9 + 0.1 * rng.NextDouble();
                        v = 1.0;
                        break;
                }

                colors[i] = Hex(Wrap(h), s, v);
            }

            return colors;
        }

        private static double Wrap(double hue) => (hue % 360 + 360) % 360;

        public static string Hex(double h, double s, double v)
        {
            var c = v * s;
            var x = c * (1 - Math.Abs(h / 60 % 2 - 1));
            var m = v - c;
            double r, g, b;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            return "#" + Byte(r + m) + Byte(g + m) + Byte(b + m);
        }

        private static string Byte(double value) =>
            ((int)Math.Round(Math.Max(0, Math.Min(1, value)) * 255)).ToString("X2", CultureInfo.InvariantCulture);

        /// <summary>SplitMix64. System.Random isn't guaranteed to match between Mono and .NET.</summary>
        private sealed class Rng
        {
            private ulong _state;

            public Rng(uint seed)
            {
                _state = seed;
            }

            private ulong NextULong()
            {
                _state += 0x9E3779B97F4A7C15UL;
                var z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }

            public int Next(int maxExclusive) => maxExclusive <= 0 ? 0 : (int)(NextULong() % (ulong)maxExclusive);

            public double NextDouble() => (NextULong() >> 11) * (1.0 / (1UL << 53));
        }
    }

    /// <summary>
    /// The color of a name at a point and moment: the one formula behind the game's names and the
    /// editor's preview. Colors are 0..1 RGB.
    /// </summary>
    public sealed class NamePaint
    {
        private readonly double[][] _colors;
        private readonly bool _letters;
        private readonly string _motion;
        private readonly double _speed;
        private readonly double _sign;

        /// <param name="letters">One color per letter, rather than a gradient across the name.</param>
        /// <param name="motion">One of <see cref="Motions"/>; null with a speed means scroll.</param>
        public NamePaint(double[][] colors, bool letters, string motion, double speed, bool reverse)
        {
            _colors = colors != null && colors.Length > 0 ? colors : new[] { new[] { 1.0, 1.0, 1.0 } };
            _letters = letters;
            _speed = Math.Max(0, speed);
            _motion = _speed > 0 ? (string.IsNullOrEmpty(motion) ? Motions.Scroll : motion.Trim().ToLowerInvariant()) : null;
            _sign = reverse ? -1 : 1;

            // A motion this doesn't know would draw still yet count as moving, and the plugin
            // would recolor the name every frame for nothing
            if (_motion != null && Array.IndexOf(Motions.All, _motion) < 0)
            {
                _motion = null;
            }

            // Scroll and pulse have nothing to move between on one color
            if (_colors.Length < 2 && (_motion == Motions.Scroll || _motion == Motions.Pulse))
            {
                _motion = null;
            }
        }

        public bool Moving => _motion != null;

        /// <summary>The letters move (bounce, glitch), not just their colors.</summary>
        public bool MovesLetters => Motions.MovesLetters(_motion);

        /// <summary>
        /// How far a letter is moved, in ems (the font's size): a bounce hops letters up in turn, a
        /// glitch knocks one aside now and then. Positive y is up.
        /// </summary>
        public void Offset(int letter, double time, out double dx, out double dy)
        {
            dx = dy = 0;
            var t = time * _speed * _sign;
            if (_motion == Motions.Bounce)
            {
                var wave = Math.Sin(2 * Math.PI * (t - letter * 0.09));
                dy = wave > 0 ? 0.16 * wave : 0;
            }
            else if (_motion == Motions.Glitch)
            {
                var h = Mix(letter, (int)Math.Floor(Math.Abs(t) * 9));
                if (h % 100 < 9)
                {
                    dx = ((int)(h / 100 % 7) - 3) * 0.035;
                    dy = ((int)(h / 700 % 5) - 2) * 0.03;
                }
            }
        }

        private static uint Mix(int a, int b)
        {
            var h = unchecked((uint)a * 2654435761u ^ (uint)b * 2246822519u);
            h ^= h >> 13;
            h = unchecked(h * 3266489917u);
            return h ^ (h >> 16);
        }

        public int Count => _colors.Length;

        public double[] Color(int index) => _colors[index];

        /// <summary>"#RRGGBB" to 0..1 RGB; null if it isn't one.</summary>
        public static double[] Parse(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                return null;
            }

            var text = hex.Trim().TrimStart('#');
            if (text.Length < 6 || !uint.TryParse(text.Substring(0, 6), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            {
                return null;
            }

            return new[] { ((value >> 16) & 0xFF) / 255.0, ((value >> 8) & 0xFF) / 255.0, (value & 0xFF) / 255.0 };
        }

        /// <summary>
        /// The color at <paramref name="x"/> (0 = the name's left edge, 1 = its right edge) on letter
        /// <paramref name="letter"/> (counting visible letters only), <paramref name="time"/> seconds in.
        /// </summary>
        public void At(double x, int letter, double time, double[] rgb)
        {
            var n = _colors.Length;
            var t = time * _speed * _sign;

            if (_letters)
            {
                switch (_motion)
                {
                    case Motions.Scroll:
                        Copy(_colors[Mod(letter + (int)Math.Floor(t * 4), n)], rgb);
                        return;
                    case Motions.Pulse:
                        {
                            var step = Math.Floor(t);
                            var i = letter + (int)step;
                            Lerp(_colors[Mod(i, n)], _colors[Mod(i + 1, n)], t - step, rgb);
                            return;
                        }
                    case Motions.Wave:
                        Copy(_colors[Mod(letter, n)], rgb);
                        Scale(rgb, 0.62 + 0.38 * (0.5 + 0.5 * Math.Sin(2 * Math.PI * (t - letter * 0.12))));
                        return;
                    case Motions.Sparkle:
                        Copy(_colors[Mod(letter, n)], rgb);
                        Flash(letter, t, rgb);
                        return;
                    case Motions.Rainbow:
                        Hsv(Frac(letter * 0.09 + t * 0.5), rgb);
                        return;
                    case Motions.Glitch:
                        Copy(_colors[Mod(letter, n)], rgb);
                        Glitched(letter, t, rgb);
                        return;
                    default:
                        Copy(_colors[Mod(letter, n)], rgb);
                        return;
                }
            }

            switch (_motion)
            {
                case Motions.Scroll:
                    Sample(Frac(x + t) * n, true, rgb);
                    return;
                case Motions.Pulse:
                    Sample(Frac(t) * n, true, rgb);
                    return;
                case Motions.Wave:
                    Sample(Clamp01(x + 0.16 * Math.Sin(2 * Math.PI * (t + x))) * (n - 1), false, rgb);
                    return;
                case Motions.Sparkle:
                    Sample(Clamp01(x) * (n - 1), false, rgb);
                    Flash(letter, t, rgb);
                    return;
                case Motions.Rainbow:
                    Hsv(Frac(x * 0.8 + t * 0.5), rgb);
                    return;
                case Motions.Glitch:
                    Sample(Clamp01(x) * (n - 1), false, rgb);
                    Glitched(letter, t, rgb);
                    return;
                default:
                    Sample(Clamp01(x) * (n - 1), false, rgb);
                    return;
            }
        }

        // Each letter flashes white briefly, at its own moment in the cycle
        private static void Flash(int letter, double t, double[] rgb)
        {
            // An integer mix rather than a string hash: this runs per letter, per frame
            var h = unchecked((uint)letter * 2654435761u);
            h ^= h >> 15;
            var phase = (h % 1000) / 1000.0;
            var cycle = Frac(t * 0.6 + phase);
            if (cycle < 0.14)
            {
                var amount = 1 - cycle / 0.14;
                for (var k = 0; k < 3; k++)
                {
                    rgb[k] += (1 - rgb[k]) * amount * 0.85;
                }
            }
        }

        // While a letter is knocked aside (see Offset) it shows another of the colors, or white
        private void Glitched(int letter, double t, double[] rgb)
        {
            var h = Mix(letter, (int)Math.Floor(Math.Abs(t) * 9));
            if (h % 100 < 9)
            {
                var pick = (int)(h / 3500 % (uint)(_colors.Length + 1));
                if (pick == _colors.Length)
                {
                    rgb[0] = rgb[1] = rgb[2] = 1;
                }
                else
                {
                    Copy(_colors[pick], rgb);
                }
            }
        }

        // A fully saturated, slightly soft hue (0..1 round the wheel)
        private static void Hsv(double hue, double[] rgb)
        {
            var h = hue * 6;
            var i = (int)Math.Floor(h);
            var f = h - i;
            const double v = 1.0, s = 0.72;
            double p = v * (1 - s), q = v * (1 - s * f), u = v * (1 - s * (1 - f));
            switch (Mod(i, 6))
            {
                case 0: rgb[0] = v; rgb[1] = u; rgb[2] = p; break;
                case 1: rgb[0] = q; rgb[1] = v; rgb[2] = p; break;
                case 2: rgb[0] = p; rgb[1] = v; rgb[2] = u; break;
                case 3: rgb[0] = p; rgb[1] = q; rgb[2] = v; break;
                case 4: rgb[0] = u; rgb[1] = p; rgb[2] = v; break;
                default: rgb[0] = v; rgb[1] = p; rgb[2] = q; break;
            }
        }

        private void Sample(double position, bool wrap, double[] rgb)
        {
            var n = _colors.Length;
            var i = (int)Math.Floor(position);
            var f = position - i;
            var a = _colors[wrap ? Mod(i, n) : Math.Max(0, Math.Min(n - 1, i))];
            var b = _colors[wrap ? Mod(i + 1, n) : Math.Max(0, Math.Min(n - 1, i + 1))];
            Lerp(a, b, f, rgb);
        }

        private static void Lerp(double[] a, double[] b, double f, double[] rgb)
        {
            for (var k = 0; k < 3; k++)
            {
                rgb[k] = a[k] + (b[k] - a[k]) * f;
            }
        }

        private static void Copy(double[] a, double[] rgb)
        {
            rgb[0] = a[0];
            rgb[1] = a[1];
            rgb[2] = a[2];
        }

        private static void Scale(double[] rgb, double factor)
        {
            for (var k = 0; k < 3; k++)
            {
                rgb[k] *= factor;
            }
        }

        private static int Mod(int a, int n) => ((a % n) + n) % n;

        private static double Frac(double v) => v - Math.Floor(v);

        private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;
    }
}
