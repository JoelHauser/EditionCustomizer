using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;

namespace ChangeIcons.Client;

/// <summary>
/// Dogtags carry the dead PMC's name; it gets that PMC's name colors (no icon), the same look the
/// name has on the death screen, in the kill list and on the flea market.
/// </summary>
public static class Dogtags
{
    public static BotLooks.Look LookFor(DogtagComponent dogtag) =>
        dogtag != null && !string.IsNullOrEmpty(dogtag.Nickname) && dogtag.Side is EPlayerSide.Bear or EPlayerSide.Usec
            ? BotLooks.For(dogtag.Nickname)
            : null;
}

/// <summary>The dogtag's tile in a grid, whose caption is the name once it's been examined.</summary>
[HarmonyPatch(typeof(GridItemView), nameof(GridItemView.UpdateItemName))]
public static class DogtagTilePatch
{
    public static void Postfix(GridItemView __instance)
    {
        var caption = __instance.Caption;
        if (caption == null)
        {
            return;
        }

        // Tiles are reused for other items: always set or clear. Unexamined tags read "???".
        var look = __instance.Item != null && __instance.Examined
            ? Dogtags.LookFor(__instance.Item.GetItemComponent<DogtagComponent>())
            : null;
        NameColorizer.AttachStyle(caption, look?.Style, caption.text);
    }
}

/// <summary>The inspect window's Nickname line. Only dogtags have that attribute.</summary>
[HarmonyPatch(typeof(CompactCharacteristicPanel), nameof(CompactCharacteristicPanel.SetValues))]
public static class DogtagInspectPatch
{
    public static void Postfix(CompactCharacteristicPanel __instance, ItemAttribute ___ItemAttribute)
    {
        var label = __instance.ValueText;
        if (label == null)
        {
            return;
        }

        // Panels are reused for every attribute: always set or clear
        BotLooks.Look look = null;
        if (___ItemAttribute?.Id is EItemAttributeId id && id == EItemAttributeId.Nickname && label.gameObject.activeInHierarchy)
        {
            look = BotLooks.For(___ItemAttribute.StringValue());
        }

        NameColorizer.AttachStyle(label, look?.Style, label.text);
    }
}

/// <summary>
/// "Your item was bought by NAME": a flea sale message's buyer gets that name's colors (still --
/// a message is text, so the colors go in as rich text). The buyer comes from the message's own
/// data, not from searching the text.
/// </summary>
[HarmonyPatch]
public static class FleaBuyerMessagePatch
{
    public static System.Reflection.MethodBase TargetMethod() =>
        System.Linq.Enumerable.First(AccessTools.GetDeclaredMethods(typeof(ChatShared.DialogueChatMessage)),
            m => m.Name == nameof(ChatShared.DialogueChatMessage.ParsedText) && m.GetParameters().Length == 2);

    public static void Postfix(ChatShared.DialogueChatMessage __instance, ChatShared.EViewRule viewRule, ref string __result)
    {
        // Only where the game itself puts rich text (its hyperlinks): the message body. Other views,
        // such as previews, may show tags as text
        if ((viewRule & ChatShared.EViewRule.AddHyperlink) == 0)
        {
            return;
        }

        var buyer = __instance.systemData?.buyerNickname;
        if (string.IsNullOrEmpty(buyer) || string.IsNullOrEmpty(__result) || !__result.Contains(buyer))
        {
            return;
        }

        var look = BotLooks.For(buyer);
        if (look?.Style != null)
        {
            __result = __result.Replace(buyer, look.Style.RichText(buyer));
        }
    }
}
