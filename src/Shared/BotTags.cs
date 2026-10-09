#nullable disable

// Compiled into both the plugin and the editor, like BotLooks.cs. A bot's tag is picked from this
// list by its name, so changing the list (or its order) changes which tag every name gets.
namespace NamePlate.Shared
{
    /// <summary>
    /// Clan tags for PMC bots, shown in brackets before the name: "[EZ] Name". Gamer humor and the
    /// kind of thing people troll with, every one 1-5 letters or digits (<see cref="MaxLength"/>).
    /// </summary>
    public static class BotTags
    {
        /// <summary>The longest a tag can be, yours or a bot's; longer ones are cut to this.</summary>
        public const int MaxLength = 5;

        /// <summary>A tag as it's shown: trimmed and cut to <see cref="MaxLength"/>; null if empty.</summary>
        public static string Clean(string tag)
        {
            if (tag == null)
            {
                return null;
            }

            tag = tag.Trim();
            if (tag.Length > MaxLength)
            {
                tag = tag.Substring(0, MaxLength);
            }

            return tag.Length == 0 ? null : tag;
        }

        public static readonly string[] All =
        {
            // Trash talk
            "EZ", "2EZ", "EZPZ", "GG", "GGEZ", "GGWP", "WP", "L2P", "NOOB", "N00B", "NOOBS", "NUB",
            "SCRUB", "REKT", "R3KT", "OWNED", "OWN3D", "PWNED", "PWND", "PWN", "DIFF", "DIFFD", "RATIO", "COPE",
            "MALD", "TILT", "TILTD", "SALTY", "SALT", "TOXIC", "CRY", "TEARS", "WAH", "SKILL", "ISSUE", "TRYHD",
            "SWEAT", "KICKD", "BAN", "CLAPD", "TAPD", "FARMD", "NEXT", "ZERO", "1TAP", "2TAP", "ONE", "0IQ",
            "1IQ", "NOIQ", "200IQ", "TROLL", "THROW", "FEED", "INT",

            // Bots pretending
            "REAL", "HUMAN", "IRL", "NPC", "BOT", "BOTS", "AI", "BEEP", "BOOP", "ROBOT", "TRUST", "SUS",
            "FREN", "PEACE", "HIPPY", "HUGS", "HUGZ", "NICE", "KIND", "NOTME", "SORRY", "SRY", "MB", "OOPS",
            "OOF", "RIP", "F",

            // Bad connection
            "LAG", "LAGGY", "PING", "DSYNC", "999MS", "1FPS", "2FPS", "0FPS", "30FPS", "60FPS", "144HZ", "SPUD",
            "TOAST", "56K", "WIFI", "NONET", "OFFLN", "AFK", "BRB", "ALTF4", "CTRLZ", "CRASH", "FROZE",

            // Rats and campers
            "RAT", "R4T", "RATS", "RATTY", "CAMP", "CAMPR", "BUSH", "DOOR", "CORNR", "ANGLE", "HOLD", "PEEK",
            "PEEKR", "PRONE", "SNEAK", "NINJA", "GOBLN", "LURK", "STAIR", "ROOF",

            // Tarkov
            "TIMMY", "CHAD", "CHADS", "GIGA", "HEAD", "EYES", "LEGS", "IFAK", "LEDX", "GPU", "BTC", "MULE",
            "KAPPA", "LABS", "WOODS", "MOSIN", "TOZ", "KEDR", "SKS", "PM", "NAKED", "NOKIT", "GEARD", "LOOTR",
            "LOOT", "LOOTD", "HOARD", "STASH", "SCAV", "SCAVS", "PMC", "KEYS", "DORMS", "EXFIL", "STUCK", "LOST",
            "NOMAP", "KIA", "MIA", "WIPE", "WIPED", "FLEA", "TAXED", "SCAM", "RUBLE", "USD", "EUROS", "STIMS",
            "JUICE", "ZOOM", "BLEED", "HEALS",

            // Playstyles
            "YOLO", "SEND", "RUSH", "RUSHB", "PUSH", "WAIT", "FLANK", "BAIT", "B8", "ACE", "CARRY", "1V9",
            "1V5", "1V1", "SOLO", "DUO", "TRIO", "SOLOQ", "STACK", "SPRAY", "DUMP", "PRAY", "360", "720",
            "BHOP", "SLIDE", "MELEE", "KNIFE", "PUNCH", "BONK", "YEET", "YOINK", "SLAP", "SMACK", "WHACK", "NADES",
            "NADE", "FRAG", "TKER", "TK", "BLUE", "SPEED", "SLOW", "SNAIL", "CASUL", "GAMER", "PRO", "PROS",
            "VET", "RQ", "RAGE", "HYPE", "HYPED", "SNIPE", "ZERG", "LUCKY",

            // Ranked
            "IRON", "GOLD", "PLAT", "WOOD", "TOP1", "TOP10", "WORST", "BEST", "MID", "MIDS", "AVG", "OKAY",
            "MEH", "FINE", "SMURF", "ALT", "MAIN", "WR", "PB", "GOAT", "W", "L", "WW", "LL",
            "WWW", "LLL", "KDA", "DPS", "TANK", "SUPP", "LVL1", "LVL99", "XP", "OP", "0KD",

            // Chat
            "LOL", "LMAO", "ROFL", "KEK", "KEKW", "XD", "BRUH", "YIKES", "OMG", "OMW", "OTW", "IDK",
            "IDC", "SMH", "TBH", "IMO", "FYI", "BTW", "TLDR", "ASAP", "NVM", "GL", "HF", "GLHF",
            "NT", "NS", "TY", "THX", "PLS", "PLZ", "HELP", "SOS", "HMM", "UHH", "WHAT", "WHY",
            "HOW", "WHEN", "WHERE", "NO", "YES", "MAYBE", "SURE", "OK", "K", "GR8", "M8", "M8S",
            "W8", "L8R", "H8", "H8RS", "4AM", "3AM", "2AM",

            // Office
            "CEO", "CFO", "HR", "IT", "LEGAL", "SALES", "TAXES", "AUDIT", "MEMO", "LUNCH", "BREAK", "WFH",
            "EXCEL", "OOO", "EOD", "KPI", "ROI", "PTO",

            // Money
            "STONK", "HODL", "WHALE", "BROKE", "POOR", "RICH", "P2W", "DLC", "MTX", "NFT", "DEBT", "LOAN",
            "SALE", "FREE", "CHEAP",

            // Nerd
            "1337", "L33T", "NULL", "NAN", "404", "418", "500", "503", "007", "TRUE", "FALSE", "ERROR",
            "BUG", "BUGGY", "PATCH", "BETA", "ALPHA", "DEMO", "TRIAL", "DEV", "DEVS", "QA", "MOD", "MODS",
            "ADMIN", "SUDO", "ROOT", "DEBUG", "LOGS", "CACHE", "WASD", "ESC", "TAB", "SHIFT", "SPACE", "CAPS",
            "ENTER", "F5", "F12", "RNG", "META", "NERF", "BUFF",

            // Food and sleep
            "SNACK", "PIZZA", "TACO", "TACOS", "NACHO", "FRIES", "TEA", "WATER", "BREAD", "BEANS", "DONUT", "BAGEL",
            "SOUP", "RAMEN", "NAP", "NAPS", "TIRED", "ZZZ", "COMFY", "CHILL", "VIBES", "VIBE",

            // Silly
            "MEOW", "WOOF", "BARK", "MOO", "OINK", "BAA", "QUACK", "HONK", "HISS", "CAT", "CATS", "DOG",
            "DOGS", "BIRD", "BEES", "FISH", "CRAB", "GOOSE", "DUCK", "FROG", "LLAMA", "SLOTH", "GOOFY", "GOOFS",
            "CLOWN", "BOZO", "BOZOS", "DORK", "NERD", "NERDS", "GEEK", "DWEEB", "OOGA", "BOOGA", "BRAIN", "MOM",
            "MOMS", "DAD", "DADS", "BRO", "BROS", "DUDE", "DUDES", "LADS", "BOYS", "SQUAD", "CREW", "GANG",
            "TEAM", "UNCLE", "AUNT", "BABY",

            // Panic
            "PANIC", "SHAKY", "1HP", "0HP", "LOWHP", "OUCH", "HURT", "DEAD", "DYING", "ALIVE", "UHOH", "NOPE",
            "RUN", "MEDIC",
        };
    }
}
