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

    /// <summary>
    /// Runs the game's drawing of someone else with the game's own icons. Scopes nest (a name
    /// panel inside a chat row), so this returns what was set before, for Leave to put back.
    /// </summary>
    public static bool Enter(bool someoneElse)
    {
        var before = IconTable.OthersScope;
        IconTable.OthersScope = before || someoneElse;
        return before;
    }

    public static Exception Leave(Exception exception, bool before = false)
    {
        IconTable.OthersScope = before;
        return exception;
    }
}

/// <summary>Messages tab, the conversation list: traders, system, chat bots, friends.</summary>
[HarmonyPatch(typeof(DialogueView), nameof(DialogueView.SetValuesByDialogueType))]
public static class DialogueViewScopePatch
{
    public static void Prefix(UpdatableChatDialogue ____dialogue, out bool __state) =>
        __state = LocalPlayer.Enter(!LocalPlayer.IsYou(____dialogue?.Profile?.Info?.Nickname));

    public static Exception Finalizer(Exception __exception, bool __state) => LocalPlayer.Leave(__exception, __state);
}

/// <summary>Messages tab, the messages in a conversation.</summary>
[HarmonyPatch(typeof(MessageView), nameof(MessageView.Show))]
public static class MessageViewScopePatch
{
    public static void Prefix(UpdatableChatMember chatMember, out bool __state) =>
        __state = LocalPlayer.Enter(!LocalPlayer.IsYou(chatMember?.Info?.Nickname));

    public static Exception Finalizer(Exception __exception, bool __state) => LocalPlayer.Leave(__exception, __state);
}

/// <summary>A conversation's member list.</summary>
[HarmonyPatch(typeof(ChatMember), nameof(ChatMember.Show))]
public static class ChatMemberScopePatch
{
    public static void Prefix(UpdatableChatMember member, UpdatableChatMember playerMember, out bool __state) =>
        __state = LocalPlayer.Enter(member != playerMember && !LocalPlayer.IsYou(member?.Info?.Nickname));

    public static Exception Finalizer(Exception __exception, bool __state) => LocalPlayer.Leave(__exception, __state);
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
    public static void Prefix(bool isMyOffer, out bool __state) => __state = LocalPlayer.Enter(!isMyOffer);

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

    public static Exception Finalizer(Exception __exception, bool __state) => LocalPlayer.Leave(__exception, __state);
}

// ---------------------------------------------------------------- other players (Fika co-op)

/// <summary>
/// Every icon drawn with a name: lobby and group panels, the party list, group member views.
/// Another player's name gets the game's own look.
/// </summary>
[HarmonyPatch(typeof(ChatSpecialIcon), nameof(ChatSpecialIcon.Show), typeof(EMemberCategory), typeof(string), typeof(bool), typeof(int))]
public static class NamedIconScopePatch
{
    public static void Prefix(string playerName, out bool __state) =>
        __state = LocalPlayer.Enter(playerName != null && !LocalPlayer.IsYou(playerName));

    public static Exception Finalizer(Exception __exception, bool __state) => LocalPlayer.Leave(__exception, __state);
}

/// <summary>Friend requests: always another player.</summary>
[HarmonyPatch(typeof(FriendsInvitationView), nameof(FriendsInvitationView.Show))]
public static class FriendRequestScopePatch
{
    public static void Prefix(out bool __state) => __state = LocalPlayer.Enter(true);

    public static Exception Finalizer(Exception __exception, bool __state) => LocalPlayer.Leave(__exception, __state);
}

/// <summary>A group invite: from another player.</summary>
[HarmonyPatch(typeof(GroupInviteWindow), nameof(GroupInviteWindow.ParseInvite))]
public static class GroupInviteScopePatch
{
    public static void Prefix(out bool __state) => __state = LocalPlayer.Enter(true);

    public static Exception Finalizer(Exception __exception, bool __state) => LocalPlayer.Leave(__exception, __state);
}

/// <summary>Looking at a group member's equipment.</summary>
[HarmonyPatch(typeof(PlayerEquipmentWindow), nameof(PlayerEquipmentWindow.Show))]
public static class EquipmentWindowScopePatch
{
    public static void Prefix(GroupPlayer groupPlayer, out bool __state) =>
        __state = LocalPlayer.Enter(!LocalPlayer.IsYou(groupPlayer?.Info?.Nickname));

    public static Exception Finalizer(Exception __exception, bool __state) => LocalPlayer.Leave(__exception, __state);
}

/// <summary>A raid invite.</summary>
[HarmonyPatch(typeof(RaidInviteWindow), nameof(RaidInviteWindow.Show))]
public static class RaidInviteScopePatch
{
    public static void Prefix(GroupPlayer player, out bool __state) =>
        __state = LocalPlayer.Enter(!LocalPlayer.IsYou(player?.Info?.Nickname));

    public static Exception Finalizer(Exception __exception, bool __state) => LocalPlayer.Leave(__exception, __state);
}

/// <summary>
/// The profile window, yours or another player's. Show has two overloads; the Profile one hands
/// off to this 7-parameter one, so patching it covers both. Naming the method without its
/// parameters would be ambiguous, and Harmony would then refuse every patch in the plugin.
/// </summary>
[HarmonyPatch]
public static class ProfileWindowScopePatch
{
    public static System.Reflection.MethodBase TargetMethod() =>
        System.Linq.Enumerable.First(AccessTools.GetDeclaredMethods(typeof(InventoryPlayerModelWithStatsWindow)),
            m => m.Name == nameof(InventoryPlayerModelWithStatsWindow.Show) && m.GetParameters().Length == 7);

    public static void Prefix(IProfileDataContainer profileDataContainer, out bool __state) =>
        __state = LocalPlayer.Enter(profileDataContainer != null && !LocalPlayer.IsYou(profileDataContainer.Nickname));

    public static Exception Finalizer(Exception __exception, bool __state) => LocalPlayer.Leave(__exception, __state);
}
