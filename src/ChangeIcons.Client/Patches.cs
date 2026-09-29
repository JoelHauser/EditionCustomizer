using System.Linq;
using EFT;
using EFT.UI;
using EFT.UI.Settings;
using HarmonyLib;
using IconsData = EFT.UI.ChatSpecialIconSettings.IconsData;

namespace ChangeIcons.Client;

/// <summary>
/// Every icon and nickname color the game draws is looked up here.
/// </summary>
[HarmonyPatch(typeof(ChatSpecialIconSettings), nameof(ChatSpecialIconSettings.GetDataByMemberCategory))]
public static class GetDataByMemberCategoryPatch
{
    // The game's lookup only walks its own enum values, and since Is() is HasFlag, Default (0)
    // matches every category. A custom one would always come back as the Default icon.
    public static bool Prefix(ChatSpecialIconSettings __instance, EMemberCategory category, ref IconsData __result)
    {
        IconTable.Apply(__instance);

        foreach (var row in __instance.IconsSettings)
        {
            if (IconTable.IsCustom(row.Category) && (category & row.Category) == row.Category)
            {
                __result = row;
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Nickname labels next to an icon: the game sets one color, this adds the multi-color ones.
/// </summary>
[HarmonyPatch(typeof(ChatSpecialIcon), nameof(ChatSpecialIcon.Show), typeof(EMemberCategory), typeof(string), typeof(bool), typeof(int))]
public static class ChatSpecialIconShowPatch
{
    public static void Postfix(ChatSpecialIcon __instance, EMemberCategory category, bool isNameColored)
    {
        var label = __instance._specialLabel;
        if (label == null)
        {
            return;
        }

        // Show returns early without coloring when there's no icon image
        var row = __instance._icon != null && isNameColored
            ? EFTHardSettings.Instance.ChatSpecialIconSettings.GetDataByMemberCategory(category)
            : null;
        NameColorizer.Attach(label, row?.Category);
    }
}

/// <summary>
/// The settings dropdown reads the table directly, without the lookup above.
/// </summary>
[HarmonyPatch(typeof(GameSettingsTab), nameof(GameSettingsTab.ShowProfileIcons))]
public static class ShowProfileIconsPatch
{
    public static void Prefix(GameSettingsTab __instance)
    {
        IconTable.Apply(__instance._iconsSettings);
    }
}

/// <summary>
/// Dropdown label: the game writes an inline atlas sprite plus the translated category name in
/// one color. This writes our name and colors; a custom category has no atlas sprite, so it
/// gets none.
/// </summary>
[HarmonyPatch(typeof(GameSettingsTab.CG_Class3211), nameof(GameSettingsTab.CG_Class3211.method_2))]
public static class ProfileIconLabelPatch
{
    public static bool Prefix(IconsData data, ref string __result)
    {
        var style = IconTable.StyleFor(data.Category);
        var named = IconTable.Named.Contains(data.Category);
        if (!named && style == null)
        {
            return true;
        }

        var text = named ? data.Name : data.Category.Localized(EStringCase.None);
        var body = style != null
            ? style.RichText(text)
            : $"<color=#{UnityEngine.ColorUtility.ToHtmlStringRGBA(data.IconColor)}>{text}</color>";
        var sprite = IconTable.IsCustom(data.Category)
            ? ""
            : $"<sprite index={ChatSpecialIconSettings.GetAtlasIconId(data.Category)} color=white>";
        __result = sprite + body;
        return false;
    }
}

/// <summary>
/// "Killed by" on the death screen: a PMC bot's own icon and colors, from its name.
/// </summary>
[HarmonyPatch]
public static class DeathScreenPatch
{
    public static System.Reflection.MethodBase TargetMethod() =>
        AccessTools.GetDeclaredMethods(typeof(EFT.UI.SessionEnd.SessionResultExitStatus))
            .First(m => m.Name == nameof(EFT.UI.SessionEnd.SessionResultExitStatus.Show) && m.GetParameters().Length == 7);

    public static void Postfix(EFT.UI.SessionEnd.SessionResultExitStatus __instance, Profile activeProfile)
    {
        var panel = __instance._killerNamePanel;
        var aggressor = activeProfile?.EftStats?.Aggressor;
        if (panel == null || aggressor == null)
        {
            return;
        }

        var look = BotLooks.IsPmcBot(aggressor.Role) ? BotLooks.For(aggressor.Name) : null;
        if (look != null)
        {
            BotLooks.ApplyTo(panel, look);
            return;
        }

        // The panel is reused raid after raid: give the name back what its category gives it
        // (one of your styled icons, or plain), not the last bot's colors
        var shown = aggressor.Side != EPlayerSide.Savage ? aggressor.Category : EMemberCategory.Default;
        var row = EFTHardSettings.Instance.ChatSpecialIconSettings.GetDataByMemberCategory(shown);
        NameColorizer.Attach(panel._name, row?.Category);
    }
}

/// <summary>
/// The kill list after a raid: PMC bots you killed get their colors (the list has no icons).
/// </summary>
[HarmonyPatch(typeof(EFT.UI.SessionEnd.KillListVictim), nameof(EFT.UI.SessionEnd.KillListVictim.Show))]
public static class KillListPatch
{
    public static void Postfix(EFT.UI.SessionEnd.KillListVictim __instance, VictimStats victim, bool knownName)
    {
        // Rows are reused, so a row without a look is put back to plain
        var look = knownName && victim != null && BotLooks.IsPmcBot(victim.Role) ? BotLooks.For(victim.Name) : null;
        NameColorizer.AttachStyle(__instance._name, look?.Style);
    }
}
