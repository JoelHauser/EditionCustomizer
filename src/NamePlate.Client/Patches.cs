using System.Linq;
using EFT;
using EFT.UI;
using EFT.UI.Settings;
using HarmonyLib;
using IconsData = EFT.UI.ChatSpecialIconSettings.IconsData;

namespace NamePlate.Client;

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
        try
        {
            IconTable.Apply(__instance);

            if (IconTable.OthersScope)
            {
                __result = IconTable.OriginalFor(__instance, category);
                return false;
            }

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
        catch (System.Exception e)
        {
            Safe.Report("looking up a member icon", e);
            return true;
        }
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
        try
        {
            var label = __instance._specialLabel;
            if (IconTable.OthersScope)
            {
                NameColorizer.Attach(label, null);
                IconAnimator.Attach(__instance._icon, null);
                return;
            }

            // Show returns early without coloring when there's no icon image
            var row = __instance._icon != null
                ? EFTHardSettings.Instance.ChatSpecialIconSettings.GetDataByMemberCategory(category)
                : null;
            NameColorizer.Attach(label, isNameColored ? row?.Category : null);
            IconAnimator.Attach(__instance._icon, row == null ? null : IconTable.MotionFor(row.Category));

            // The prestige badge: hidden, or a picture of your own in its place
            var prestige = row == null ? null : IconTable.PrestigeFor(row.Category);
            var badge = __instance._iconPrestige;
            if (prestige != null && badge != null)
            {
                if (prestige.Equals("hide", System.StringComparison.OrdinalIgnoreCase))
                {
                    badge.gameObject.SetActive(false);
                }
                else if (IconTable.FileSprite(prestige) is { } picture)
                {
                    badge.sprite = picture;
                    badge.gameObject.SetActive(true);
                }
            }
        }
        catch (System.Exception e)
        {
            Safe.Report("styling a name and icon", e);
        }
    }
}

/// <summary>
/// A name panel (main menu, death screen, raid countdown): the colors go on whichever of its two
/// labels shows the nickname. Normally the big one; a mod can rearrange the panel, as the deploy
/// screen does on the countdown, putting the map in the big line and the name in the small one.
/// </summary>
[HarmonyPatch(typeof(PlayerNamePanel), nameof(PlayerNamePanel.Set), typeof(bool), typeof(EMemberCategory), typeof(string), typeof(int), typeof(int))]
public static class PlayerNamePanelPatch
{
    // Someone else's panel (the death screen's killer) is drawn with the game's own icons
    public static void Prefix(string nickname, out bool __state) => __state = LocalPlayer.Enter(!LocalPlayer.IsYou(nickname));

    public static void Postfix(PlayerNamePanel __instance, bool showDetails, EMemberCategory category, string nickname)
    {
        try
        {
            if (IconTable.OthersScope)
            {
                NameColorizer.Attach(__instance._name, null);
                NameColorizer.Attach(__instance._description, null);
                return;
            }

            var row = EFTHardSettings.Instance.ChatSpecialIconSettings.GetDataByMemberCategory(showDetails ? category : EMemberCategory.Default);
            NameColorizer.Attach(__instance._name, row?.Category, nickname);
            NameColorizer.Attach(__instance._description, row?.Category, nickname);
        }
        catch (System.Exception e)
        {
            Safe.Report("styling a name panel", e);
        }
    }

    public static System.Exception Finalizer(System.Exception __exception, bool __state) => LocalPlayer.Leave(__exception, __state);
}

/// <summary>
/// The settings dropdown reads the table directly, without the lookup above.
/// </summary>
[HarmonyPatch(typeof(GameSettingsTab), nameof(GameSettingsTab.ShowProfileIcons))]
public static class ShowProfileIconsPatch
{
    public static void Prefix(GameSettingsTab __instance)
    {
        try
        {
            IconTable.Apply(__instance._iconsSettings);
        }
        catch (System.Exception e)
        {
            Safe.Report("preparing the settings dropdown", e);
        }
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
        try
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
        catch (System.Exception e)
        {
            Safe.Report("writing a settings dropdown label", e);
            return true;
        }
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
        try
        {
            var panel = __instance._killerNamePanel;
            var aggressor = activeProfile?.EftStats?.Aggressor;
            if (panel == null || aggressor == null)
            {
                return;
            }

            // Anyone else was already put back to the game's own look by PlayerNamePanelPatch,
            // which also clears the last raid's bot colors off this reused panel
            var look = BotLooks.IsPmcBot(aggressor.Role) ? BotLooks.For(aggressor.Name, BotLooks.SideOf(aggressor.Side), aggressor.ProfileId) : null;
            if (look != null)
            {
                BotLooks.ApplyTo(panel, look, panel._name != null ? panel._name.text : null);
            }
        }
        catch (System.Exception e)
        {
            Safe.Report("styling the death screen's killer", e);
        }
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
        try
        {
            // Rows are reused, so a row without a look is put back to plain
            var look = knownName && victim != null && BotLooks.IsPmcBot(victim.Role) ? BotLooks.For(victim.Name, BotLooks.SideOf(victim.Side), victim.ProfileId) : null;
            NameColorizer.AttachStyle(__instance._name, look?.Tagged);
        }
        catch (System.Exception e)
        {
            Safe.Report("styling the kill list", e);
        }
    }
}
