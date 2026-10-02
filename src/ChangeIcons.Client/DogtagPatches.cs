using System;
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
/// "Your item was bought by NAME": a flea sale message's buyer gets that name's colors, moving
/// ones included. The message label has rich text off, so the text isn't touched: the colorizer
/// colors just the buyer's characters in it.
///
/// SPT's server writes the buyer straight into the message text and sends no systemData, and its
/// mail service sends a sale as MessageWithItems, not FleamarketMessage. So the name is read back
/// out of the text with the game's own template for that message ("5bdabfb886f7743e152e867e 0":
/// "Your {soldItem} {itemCount} items were bought by {buyerNickname}."), or an English
/// "bought by NAME." if the server and the game are in different languages.
/// </summary>
[HarmonyPatch(typeof(EFT.UI.Chat.MessageView), nameof(EFT.UI.Chat.MessageView.SetSenderMessage))]
public static class FleaBuyerMessagePatch
{
    private const string TemplateKey = "5bdabfb886f7743e152e867e 0";
    private static readonly System.Text.RegularExpressions.Regex English =
        new(@"bought by (?<buyer>.+?)\.?\s*$", System.Text.RegularExpressions.RegexOptions.Compiled);
    private static System.Text.RegularExpressions.Regex _fromTemplate;

    public static void Postfix(EFT.UI.Chat.MessageView __instance, ChatShared.DialogueChatMessage message)
    {
        var label = __instance._senderMessage != null ? __instance._senderMessage._textMessage : null;
        if (label == null)
        {
            return;
        }

        // Rows are reused for every message: always set or clear
        var text = label.text;
        var start = -1;
        var length = 0;
        var buyer = message?.systemData?.buyerNickname;
        if (!string.IsNullOrEmpty(buyer) && !string.IsNullOrEmpty(text))
        {
            start = text.LastIndexOf(buyer, StringComparison.Ordinal);
            length = buyer.Length;
        }
        else if (message != null && !string.IsNullOrEmpty(text)
                 && message.Type is ChatShared.EMessageType.FleamarketMessage or ChatShared.EMessageType.MessageWithItems
                 && FindBuyer(text, out start, out length))
        {
            buyer = text.Substring(start, length);
        }

        var look = start >= 0 && length > 0 ? BotLooks.For(buyer) : null;
        NameColorizer.AttachStyle(label, look?.Style, text, start, length);
    }

    private static bool FindBuyer(string text, out int start, out int length)
    {
        var match = Template()?.Match(text);
        if (match is not { Success: true })
        {
            match = English.Match(text);
        }

        var group = match.Success ? match.Groups["buyer"] : null;
        start = group?.Index ?? -1;
        length = group?.Length ?? 0;
        return group is { Success: true, Length: > 0 };
    }

    // The template turned into a pattern: {buyerNickname} captured, the other tags anything
    private static System.Text.RegularExpressions.Regex Template()
    {
        if (_fromTemplate != null)
        {
            return _fromTemplate;
        }

        var template = TemplateKey.Localized();

        // Locales not loaded yet: the key comes back as itself. Try again next time.
        if (string.IsNullOrEmpty(template) || !template.Contains("{buyerNickname}"))
        {
            return null;
        }

        var pattern = System.Text.RegularExpressions.Regex.Escape(template.Trim())
            .Replace(@"\{buyerNickname}", "(?<buyer>.+?)");
        pattern = System.Text.RegularExpressions.Regex.Replace(pattern, @"\\\{[^}]*\}", ".*?");
        _fromTemplate = new System.Text.RegularExpressions.Regex("^" + pattern + @"\s*$", System.Text.RegularExpressions.RegexOptions.Compiled);
        return _fromTemplate;
    }
}
