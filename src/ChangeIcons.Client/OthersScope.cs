using System;
using EFT;
using EFT.UI;
using ChatShared;
using EFT.UI.Chat;
using HarmonyLib;

namespace ChangeIcons.Client;

/// <summary>
/// Whether a name on screen is yours.
/// </summary>
public static class LocalPlayer
{
    public static bool IsYou(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        var profile = TarkovApplication.Exist(out var app) ? app.GetClientBackEndSession()?.Profile : null;

        // No session yet: can't tell, so keep the old behaviour (your look)
        if (profile == null)
        {
            return true;
        }

        // Streamer mode shows a stand-in for your name; that is still you
        return name == profile.Nickname || name == profile.GetCorrectedNickname();
    }

    /// <summary>Runs the game's drawing of someone else with the game's own icons.</summary>
    public static void Enter(bool someoneElse) => IconTable.OthersScope = someoneElse;

    public static Exception Leave(Exception exception)
    {
        IconTable.OthersScope = false;
        return exception;
    }
}

/// <summary>Messages tab, the conversation list: traders, system, chat bots, friends.</summary>
[HarmonyPatch(typeof(DialogueView), nameof(DialogueView.SetValuesByDialogueType))]
public static class DialogueViewScopePatch
{
    public static void Prefix(UpdatableChatDialogue ____dialogue) =>
        LocalPlayer.Enter(!LocalPlayer.IsYou(____dialogue?.Profile?.Info?.Nickname));

    public static Exception Finalizer(Exception __exception) => LocalPlayer.Leave(__exception);
}

/// <summary>Messages tab, the messages in a conversation.</summary>
[HarmonyPatch(typeof(MessageView), nameof(MessageView.Show))]
public static class MessageViewScopePatch
{
    public static void Prefix(UpdatableChatMember chatMember) =>
        LocalPlayer.Enter(!LocalPlayer.IsYou(chatMember?.Info?.Nickname));

    public static Exception Finalizer(Exception __exception) => LocalPlayer.Leave(__exception);
}

/// <summary>A conversation's member list.</summary>
[HarmonyPatch(typeof(ChatMember), nameof(ChatMember.Show))]
public static class ChatMemberScopePatch
{
    public static void Prefix(UpdatableChatMember member, UpdatableChatMember playerMember) =>
        LocalPlayer.Enter(member != playerMember && !LocalPlayer.IsYou(member?.Info?.Nickname));

    public static Exception Finalizer(Exception __exception) => LocalPlayer.Leave(__exception);
}

/// <summary>
/// Flea market sellers. Not your offers: the game's own look, and for a player-style seller (not
/// a trader) its PMC bot look on top -- the same name-based look a PMC of that name has in raid,
/// so the market's sellers vary instead of all wearing your changed Standard icon.
/// </summary>
[HarmonyPatch(typeof(EFT.UI.Ragfair.MerchantInfoView), nameof(EFT.UI.Ragfair.MerchantInfoView.Show),
    typeof(EFT.UI.Ragfair.RagFair), typeof(EFT.UI.Ragfair.Offer.Merchant), typeof(bool))]
public static class FleaSellerPatch
{
    public static void Prefix(bool isMyOffer) => LocalPlayer.Enter(!isMyOffer);

    public static void Postfix(EFT.UI.Ragfair.MerchantInfoView __instance, EFT.UI.Ragfair.Offer.Merchant merchant, bool isMyOffer)
    {
        if (isMyOffer || merchant == null)
        {
            return;
        }

        // Rows are reused as the list scrolls: always set or clear
        var look = (merchant.MemberType & EMemberCategory.Trader) == 0 ? BotLooks.For(merchant.Nickname) : null;
        NameColorizer.AttachStyle(__instance._merchantName, look?.Style, merchant.CorrectedNickname);

        var image = __instance._specialIcon != null ? __instance._specialIcon._icon : null;
        if (look?.Icon != null && image != null)
        {
            image.sprite = look.Icon;
        }
    }

    public static Exception Finalizer(Exception __exception) => LocalPlayer.Leave(__exception);
}
