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
/// Dropdown label: the game writes an inline atlas sprite plus the translated category name.
/// A custom category has neither, so it gets its name in its color.
/// </summary>
[HarmonyPatch(typeof(GameSettingsTab.CG_Class3211), nameof(GameSettingsTab.CG_Class3211.method_2))]
public static class ProfileIconLabelPatch
{
    public static bool Prefix(IconsData data, ref string __result)
    {
        if (!IconTable.Named.Contains(data.Category))
        {
            return true;
        }

        var color = "#" + UnityEngine.ColorUtility.ToHtmlStringRGBA(data.IconColor);
        __result = $"<color={color}>{data.Name}</color>";
        return false;
    }
}
