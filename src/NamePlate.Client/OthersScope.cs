using System;
using System.Collections.Generic;
using EFT;
using EFT.UI;
using ChatShared;
using EFT.UI.Chat;
using HarmonyLib;

namespace NamePlate.Client;

/// <summary>
/// Whether a name on screen is yours.
/// </summary>
public static class LocalPlayer
{
    public static bool IsYou(string name)
    {
        try
        {
            return Check(name);
        }
        catch (Exception e)
        {
            // It runs inside the game's own methods: never let it throw. Treat as you, as before
            // the session exists.
            Safe.Report("checking whether a name is yours", e);
            return true;
        }
    }

    private static bool Check(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        var session = TarkovApplication.Exist(out var app) ? app.GetClientBackEndSession() : null;
        var profile = session?.Profile;

        // Said once, so a log shows straight away whether this could find you at all
        if (!_reported)
        {
            _reported = true;
            Plugin.Log.LogInfo(profile == null
                ? "Couldn't find your profile yet: names are treated as yours until it can"
                : $"Your profile: {profile.Nickname}. Changed game icons apply to that name only");
        }

        // No session yet: can't tell, so keep the old behaviour (your look)
        if (profile == null)
        {
            return true;
        }

        // Streamer mode shows a stand-in for your name; that is still you. Your scav too, if asked.
        return name == profile.Nickname || name == profile.GetCorrectedNickname()
            || (Plugin.Settings.ScavLook && session.ProfileOfPet?.Nickname is { Length: > 0 } scav
                && string.Equals(name, scav, StringComparison.OrdinalIgnoreCase));
    }

    private static bool _reported;

    /// <summary>What a scope replaced, for Leave to put back.</summary>
    public struct Scope
    {
        public bool Others;
        public string Name;
    }

    /// <summary>
    /// Runs the game's drawing of someone else with the game's own icons, or with the look they
    /// share (Fika) when <paramref name="name"/> says who it is. Scopes nest (a name panel inside
    /// a chat row), so this returns what was set before, for Leave to put back.
    /// </summary>
    public static Scope Enter(bool someoneElse, string name = null)
    {
        var before = new Scope { Others = IconTable.OthersScope, Name = IconTable.OthersName };
        IconTable.OthersScope = before.Others || someoneElse;
        if (someoneElse)
        {
            IconTable.OthersName = name;
        }

        return before;
    }

    public static Exception Leave(Exception exception, Scope before)
    {
        IconTable.OthersScope = before.Others;
        IconTable.OthersName = before.Name;
        return exception;
    }
}

/// <summary>
/// Messages tab, the conversation list: traders, system, chat bots, friends. A row is always the
/// other side of a conversation, never you. (Deciding by the row's Profile went wrong for traders:
/// the trader branch only puts the trader in Profile partway through, after this has run.)
/// </summary>
[HarmonyPatch(typeof(DialogueView), nameof(DialogueView.SetValuesByDialogueType))]
public static class DialogueViewScopePatch
{
    public static void Prefix(out LocalPlayer.Scope __state) => __state = LocalPlayer.Enter(true);

    public static Exception Finalizer(Exception __exception, LocalPlayer.Scope __state) => LocalPlayer.Leave(__exception, __state);
}

/// <summary>Messages tab, the messages in a conversation.</summary>
[HarmonyPatch(typeof(MessageView), nameof(MessageView.Show))]
public static class MessageViewScopePatch
{
    public static void Prefix(UpdatableChatMember chatMember, out LocalPlayer.Scope __state) =>
        __state = LocalPlayer.Enter(!LocalPlayer.IsYou(chatMember?.Info?.Nickname), chatMember?.Info?.Nickname);

    public static Exception Finalizer(Exception __exception, LocalPlayer.Scope __state) => LocalPlayer.Leave(__exception, __state);
}

/// <summary>A conversation's member list.</summary>
[HarmonyPatch(typeof(ChatMember), nameof(ChatMember.Show))]
public static class ChatMemberScopePatch
{
    public static void Prefix(UpdatableChatMember member, UpdatableChatMember playerMember, out LocalPlayer.Scope __state) =>
        __state = LocalPlayer.Enter(member != playerMember && !LocalPlayer.IsYou(member?.Info?.Nickname), member?.Info?.Nickname);

    public static Exception Finalizer(Exception __exception, LocalPlayer.Scope __state) => LocalPlayer.Leave(__exception, __state);
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
    public static void Prefix(bool isMyOffer, EFT.UI.Ragfair.Offer.Merchant merchant, out LocalPlayer.Scope __state) =>
        __state = LocalPlayer.Enter(!isMyOffer, merchant?.Nickname);

    public static void Postfix(EFT.UI.Ragfair.MerchantInfoView __instance, EFT.UI.Ragfair.Offer.Merchant merchant, bool isMyOffer)
    {
        try
        {
            // Rows are reused as the list scrolls (yours, other sellers', traders'): always set or clear
            var look = !isMyOffer && merchant != null && (merchant.MemberType & EMemberCategory.Trader) == 0
                ? BotLooks.For(merchant.Nickname, null, merchant.Id.ToString())
                : null;
            NameColorizer.AttachStyle(__instance._merchantName, look?.Tagged, merchant?.CorrectedNickname);
            BotLooks.ShowIcon(__instance._specialIcon != null ? __instance._specialIcon._icon : null, look);

            // Last, once the tag and any bold or small caps are on: the label is as wide as the name
            SellerName.Fit(__instance._merchantName);
        }
        catch (System.Exception e)
        {
            Safe.Report("styling a flea seller", e);
        }
    }

    public static Exception Finalizer(Exception __exception, LocalPlayer.Scope __state) => LocalPlayer.Leave(__exception, __state);
}

/// <summary>The same row showing a trader's offer: put the label back as the game made it.</summary>
[HarmonyPatch(typeof(EFT.UI.Ragfair.MerchantInfoView), nameof(EFT.UI.Ragfair.MerchantInfoView.Show), typeof(Profile.TraderInfo))]
public static class FleaTraderRowPatch
{
    public static void Postfix(EFT.UI.Ragfair.MerchantInfoView __instance)
    {
        try
        {
            NameColorizer.AttachStyle(__instance._merchantName, null);
            SellerName.Restore(__instance._merchantName);
        }
        catch (System.Exception e)
        {
            Safe.Report("resetting a flea trader row", e);
        }
    }
}

/// <summary>
/// The flea seller's name sits in a fixed 73 px box, centered, beside the member icon, and the row
/// centers icon and box together. A short name left a gap after the icon, a long one wrapped over
/// the rating (or, kept on one line, spilled over the icon). Sized to the name instead, up to the
/// room there is and cut short with "..." past it, the pair sits together in the middle.
/// </summary>
public static class SellerName
{
    // The row is 170 wide; the icon 19 and the spacing 5 take the rest
    private const float MaxWidth = 140f;

    private struct Settings
    {
        public UnityEngine.Vector2 Size;
        public TMPro.TextAlignmentOptions Alignment;
        public bool Wrap;
        public TMPro.TextOverflowModes Overflow;
        public bool AutoSize;
        public bool AutoContainer;
        public float FontSize;
    }

    // The label as the game made it, to start from each time and to put back for traders
    private static readonly Dictionary<TMPro.TMP_Text, Settings> Original = new();

    public static void Fit(TMPro.TMP_Text label)
    {
        if (label == null)
        {
            return;
        }

        var rect = label.rectTransform;
        if (!Original.TryGetValue(label, out var game))
        {
            game = new Settings
            {
                Size = rect.sizeDelta, Alignment = label.alignment, Wrap = label.enableWordWrapping, Overflow = label.overflowMode,
                AutoSize = label.enableAutoSizing, AutoContainer = label.autoSizeTextContainer, FontSize = label.fontSize,
            };
            Original[label] = game;
        }

        // First the game's own label, so the font comes out the size the game gives this name
        Apply(label, game);
        label.ForceMeshUpdate(true, true);
        var fontSize = label.fontSize;

        // Then held at that size on one line, left aligned, and the box exactly as wide as the
        // name: TextMeshPro can't resize or recenter it afterwards, and the row centers icon and
        // name together, so there's no gap and no overlap
        label.enableAutoSizing = false;
        label.autoSizeTextContainer = false;
        label.fontSize = fontSize;
        label.enableWordWrapping = false;
        label.overflowMode = TMPro.TextOverflowModes.Ellipsis;
        label.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        var preferred = label.GetPreferredValues(label.text).x;
        var width = UnityEngine.Mathf.Clamp(UnityEngine.Mathf.Ceil(preferred) + 2f, 2f, MaxWidth);
        rect.sizeDelta = new UnityEngine.Vector2(width, game.Size.y);

        // The row sizes its children from what they prefer (it gave a long name 151 and squeezed the
        // icon), so the cap goes in as the label's preferred width
        var element = label.GetComponent<UnityEngine.UI.LayoutElement>() ?? label.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        element.preferredWidth = width;
        label.ForceMeshUpdate(true, true);
        Relayout(rect);
    }

    public static void Restore(TMPro.TMP_Text label)
    {
        if (label == null || !Original.TryGetValue(label, out var game))
        {
            return;
        }

        Apply(label, game);
        if (label.GetComponent<UnityEngine.UI.LayoutElement>() is { } element)
        {
            element.preferredWidth = -1;
        }

        Relayout(label.rectTransform);
    }

    private static void Apply(TMPro.TMP_Text label, Settings game)
    {
        label.rectTransform.sizeDelta = game.Size;
        label.alignment = game.Alignment;
        label.enableWordWrapping = game.Wrap;
        label.overflowMode = game.Overflow;
        label.autoSizeTextContainer = game.AutoContainer;
        label.enableAutoSizing = game.AutoSize;
        label.fontSize = game.FontSize;
    }

    // The row lays its children out by their sizes: tell it this one changed
    private static void Relayout(UnityEngine.RectTransform rect)
    {
        if (rect.parent is UnityEngine.RectTransform parent)
        {
            UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(parent);
        }
    }
}

// ---------------------------------------------------------------- other players (Fika co-op)

/// <summary>
/// Every icon drawn with a name: lobby and group panels, the party list, group member views.
/// Another player's name gets the game's own look.
/// </summary>
[HarmonyPatch(typeof(ChatSpecialIcon), nameof(ChatSpecialIcon.Show), typeof(EMemberCategory), typeof(string), typeof(bool), typeof(int))]
public static class NamedIconScopePatch
{
    public static void Prefix(string playerName, out LocalPlayer.Scope __state) =>
        __state = LocalPlayer.Enter(playerName != null && !LocalPlayer.IsYou(playerName), playerName);

    public static Exception Finalizer(Exception __exception, LocalPlayer.Scope __state) => LocalPlayer.Leave(__exception, __state);
}

/// <summary>Friend requests: always another player.</summary>
[HarmonyPatch(typeof(FriendsInvitationView), nameof(FriendsInvitationView.Show))]
public static class FriendRequestScopePatch
{
    public static void Prefix(UpdatableFriendsInvitation invitation, out LocalPlayer.Scope __state) =>
        __state = LocalPlayer.Enter(true, invitation?.Profile?.Info?.Nickname);

    public static Exception Finalizer(Exception __exception, LocalPlayer.Scope __state) => LocalPlayer.Leave(__exception, __state);
}

/// <summary>A group invite: from another player.</summary>
[HarmonyPatch(typeof(GroupInviteWindow), nameof(GroupInviteWindow.ParseInvite))]
public static class GroupInviteScopePatch
{
    public static void Prefix(GroupInvite invite, out LocalPlayer.Scope __state) =>
        __state = LocalPlayer.Enter(true, invite?.FromProfile?.Info?.Nickname);

    public static Exception Finalizer(Exception __exception, LocalPlayer.Scope __state) => LocalPlayer.Leave(__exception, __state);
}

/// <summary>Looking at a group member's equipment.</summary>
[HarmonyPatch(typeof(PlayerEquipmentWindow), nameof(PlayerEquipmentWindow.Show))]
public static class EquipmentWindowScopePatch
{
    public static void Prefix(GroupPlayer groupPlayer, out LocalPlayer.Scope __state) =>
        __state = LocalPlayer.Enter(!LocalPlayer.IsYou(groupPlayer?.Info?.Nickname), groupPlayer?.Info?.Nickname);

    public static Exception Finalizer(Exception __exception, LocalPlayer.Scope __state) => LocalPlayer.Leave(__exception, __state);
}

/// <summary>A raid invite.</summary>
[HarmonyPatch(typeof(RaidInviteWindow), nameof(RaidInviteWindow.Show))]
public static class RaidInviteScopePatch
{
    public static void Prefix(GroupPlayer player, out LocalPlayer.Scope __state) =>
        __state = LocalPlayer.Enter(!LocalPlayer.IsYou(player?.Info?.Nickname), player?.Info?.Nickname);

    public static Exception Finalizer(Exception __exception, LocalPlayer.Scope __state) => LocalPlayer.Leave(__exception, __state);
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

    public static void Prefix(IProfileDataContainer profileDataContainer, out LocalPlayer.Scope __state) =>
        __state = LocalPlayer.Enter(profileDataContainer != null && !LocalPlayer.IsYou(profileDataContainer.Nickname), profileDataContainer?.Nickname);

    public static Exception Finalizer(Exception __exception, LocalPlayer.Scope __state) => LocalPlayer.Leave(__exception, __state);
}
