using System;
using System.Collections.Generic;

namespace NamePlate.Client;

/// <summary>
/// Patches catch their own errors: an exception escaping a patch breaks the game's method it sits
/// on (a screen, a chat row, a raid's player list), and with other mods (Fika) in play there are
/// more ways for an unexpected value to arrive. Each place is reported once, then stays quiet.
/// </summary>
public static class Safe
{
    private static readonly HashSet<string> Reported = new();

    public static void Report(string where, Exception e)
    {
        if (Reported.Add(where))
        {
            Plugin.Log.LogError($"Error while {where} (the game carries on without it; reported once): {e}");
        }
    }
}
