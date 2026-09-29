#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;

// Compiled into both the plugin (net472, the game's Mono) and the editor (.NET 10), so the
// editor's preview is exactly what the game draws. Keep it to APIs both have, and keep every
// random draw in the same order: a bot's look is the sequence of draws seeded by its name.
namespace ChangeIcons.Shared
{
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

        /// <summary>Percent of multi-color names that move.</summary>
        public int AnimateChance = 30;

        public double MaxSpeed = 1.0;
    }

    public sealed class BotLook
    {
        public string[] Colors;

        /// <summary>"solid", "gradient" or "letters".</summary>
        public string Mode;

        public double Speed;

        /// <summary>An entry of <see cref="BotRules.Icons"/>, or null for the bot's own icon.</summary>
        public string Icon;
    }

    public static class BotLookGenerator
    {
        public const string MemberPrefix = "member:";

        public static BotLook For(string name, BotRules rules)
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
                    look.Colors = new[] { RandomColor(rng) };
                }
            }
            else
            {
                look.Colors = usePreset ? palettes[rng.Next(palettes.Length)] : RandomPalette(rng);
                if (look.Colors.Length < 2)
                {
                    look.Colors = new[] { look.Colors[0], RandomColor(rng) };
                }

                if (rng.Next(100) < rules.AnimateChance)
                {
                    var max = Math.Max(0.1, rules.MaxSpeed);
                    look.Speed = Math.Round(0.1 + rng.NextDouble() * (max - 0.1), 2);
                }
            }

            var icons = rules.Icons ?? new string[0];
            look.Icon = icons.Length > 0 ? icons[rng.Next(icons.Length)] : null;
            return look;
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

        private static string RandomColor(Rng rng) =>
            Hex(rng.NextDouble() * 360, 0.55 + 0.4 * rng.NextDouble(), 0.85 + 0.15 * rng.NextDouble());

        // Two to four colors a step of hue apart, so they read as one scheme
        private static string[] RandomPalette(Rng rng)
        {
            var count = 2 + rng.Next(3);
            var hue = rng.NextDouble() * 360;
            var step = 25 + rng.NextDouble() * 55;
            var colors = new string[count];
            for (var i = 0; i < count; i++)
            {
                colors[i] = Hex((hue + i * step) % 360, 0.6 + 0.35 * rng.NextDouble(), 0.8 + 0.2 * rng.NextDouble());
            }

            return colors;
        }

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
}
