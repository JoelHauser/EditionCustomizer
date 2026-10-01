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
