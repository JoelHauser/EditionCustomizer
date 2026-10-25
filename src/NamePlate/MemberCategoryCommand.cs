using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Dialogue.Commando.SptCommands;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services.Commerce;

namespace NamePlate;

[Injectable]
public class MemberCategoryCommand(
    ProfileHelper profileHelper,
    SaveServer saveServer,
    MailSendService mailSendService,
    TemplateTable templateTable,
    IReadOnlyList<SptMod> loadedMods) : ISptCommand
{
    // These turn the account into a service one and break the profile
    private const MemberCategory Blocked =
        MemberCategory.Trader
        | MemberCategory.Group
        | MemberCategory.System
        | MemberCategory.ChatModerator
        | MemberCategory.ChatModeratorWithPermanentBan
        | MemberCategory.UnitTest;

    private static readonly MemberCategory Builtin = Enum.GetValues<MemberCategory>().Aggregate((a, b) => a | b);

    // Free flags above the game's own (2048, 4096, ...), drawn by the NamePlate.Client plugin
    private const MemberCategory Custom = (MemberCategory)0x7FFFF800;

    private static readonly MemberCategory Allowed = (Builtin & ~Blocked) | Custom;

    public string Command => CommandName.For(loadedMods);

    public string CommandHelp =>
        $"spt {Command}\n========\nChanges the icons on your account.\n\n"
        + $"\tspt {Command}\n\t\tWhat you have now\n\n"
        + $"\tspt {Command} list\n\t\tWhat you can add\n\n"
        + $"\tspt {Command} [number | names]\n\t\tEx: spt {Command} 1026\n\t\tEx: spt {Command} unheard+uniqueid\n\t\tEx: spt {Command} unheard+uniqueid+2048\n\n"
        + $"\tspt {Command} default\n\t\tBack to what your game edition comes with";

    public async ValueTask<string> PerformAction(UserDialogInfo commandHandler, MongoId sessionId, SendMessageRequest request)
    {
        var args = string.Join(' ', request.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(2));
        var fullProfile = profileHelper.GetFullProfile(sessionId);
        var info = fullProfile?.CharacterData?.PmcData?.Info;

        string reply;
        if (info is null)
        {
            reply = "Couldn't find your character.";
        }
        else if (args == "")
        {
            reply = $"You have {Describe(info.MemberCategory ?? MemberCategory.Default)}.";
        }
        else if (args == "list")
        {
            reply = "You can add:\n" + string.Join("\n", Enum.GetValues<MemberCategory>().Where(c => (c & ~Allowed) == 0).Select(c => $"{(int)c} = {c}"))
                + "\n2048, 4096, 8192, ... = your own icons from icons.json";
        }
        else if (args.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            // What SPT gives a new character of this edition (Unheard 1026, Edge of Darkness 2, ...).
            // A plain 0 is still "spt membercategory 0".
            var (edition, selected) = EditionDefault(fullProfile!.ProfileInfo?.Edition, info.Side);
            info.MemberCategory = edition;
            info.SelectedMemberCategory = selected;
            await saveServer.SaveProfileAsync(sessionId);
            reply = $"Done! You're back to what your edition comes with: {Describe(edition)}. Fully restart the game to see it.";
        }
        else if (!TryParse(args, out var value) || (value & ~Allowed) != 0)
        {
            reply = $"That can't be set. Type 'spt {Command} list' to see what you can add.";
        }
        else
        {
            var previous = info.MemberCategory ?? MemberCategory.Default;
            info.MemberCategory = value;

            // Show a custom icon you just added; otherwise the shown icon has to be one you
            // have: Unheard first, then Edge of Darkness
            var added = value & ~previous & Custom;
            var selected = info.SelectedMemberCategory ?? MemberCategory.Default;
            if (added != 0)
            {
                info.SelectedMemberCategory = LowestFlag(added);
            }
            else if ((value & selected) != selected)
            {
                info.SelectedMemberCategory =
                    (value & Custom) != 0 ? LowestFlag(value & Custom)
                    : value.HasFlag(MemberCategory.Unheard) ? MemberCategory.Unheard
                    : value.HasFlag(MemberCategory.UniqueId) ? MemberCategory.UniqueId
                    : MemberCategory.Default;
            }

            await saveServer.SaveProfileAsync(sessionId);
            reply = $"Done! You now have {Describe(value)}. Fully restart the game to see it.";
        }

        mailSendService.SendUserMessageToPlayer(sessionId, commandHandler, reply);
        return request.DialogId;
    }

    // "unheard+uniqueid+2048": names and numbers, joined with + or spaces
    private static bool TryParse(string args, out MemberCategory value)
    {
        value = MemberCategory.Default;
        foreach (var part in args.Split(['+', ',', ' '], StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(part, out var number))
            {
                value |= (MemberCategory)number;
            }
            else if (Enum.TryParse<MemberCategory>(part, true, out var named))
            {
                value |= named;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    // The edition's profile template, the side the character plays; Default if either is unknown
    private (MemberCategory Owned, MemberCategory Selected) EditionDefault(string? edition, string? side)
    {
        var templates = templateTable.Profiles ?? [];
        var template = edition == null ? null : templates.FirstOrDefault(t => t.Key.Equals(edition, StringComparison.OrdinalIgnoreCase)).Value;
        var character = (side?.Equals("Bear", StringComparison.OrdinalIgnoreCase) == true ? template?.Bear : template?.Usec)?.Character?.Info;
        var owned = (character?.MemberCategory ?? MemberCategory.Default) & Allowed;
        var selected = character?.SelectedMemberCategory ?? MemberCategory.Default;
        return (owned, (owned & selected) == selected ? selected : MemberCategory.Default);
    }

    private static MemberCategory LowestFlag(MemberCategory value) => (MemberCategory)((int)value & -(int)value);

    private static string Describe(MemberCategory value)
    {
        var names = Enum.GetValues<MemberCategory>().Where(c => c != MemberCategory.Default && value.HasFlag(c)).Select(c => c.ToString())
            .Concat(Enumerable.Range(11, 20).Select(bit => 1 << bit).Where(bit => ((int)value & bit) != 0).Select(bit => $"Custom {bit}"));
        return $"{(int)value} ({(value == MemberCategory.Default ? "Default" : string.Join(" + ", names))})";
    }
}
