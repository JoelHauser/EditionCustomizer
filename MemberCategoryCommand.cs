using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Dialogue.Commando.SptCommands;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services.Commerce;

namespace NamePlate;

[Injectable]
public class MemberCategoryCommand(ProfileHelper profileHelper, SaveServer saveServer, MailSendService mailSendService) : ISptCommand
{
    // These turn the account into a service one and break the profile
    private const MemberCategory Blocked =
        MemberCategory.Trader
        | MemberCategory.Group
        | MemberCategory.System
        | MemberCategory.ChatModerator
        | MemberCategory.ChatModeratorWithPermanentBan
        | MemberCategory.UnitTest;

    private static readonly MemberCategory Allowed = Enum.GetValues<MemberCategory>().Aggregate((a, b) => a | b) & ~Blocked;

    public string Command => "membercategory";

    public string CommandHelp =>
        "spt membercategory\n========\nChanges the icons on your account.\n\n"
        + "\tspt membercategory\n\t\tWhat you have now\n\n"
        + "\tspt membercategory list\n\t\tWhat you can add\n\n"
        + "\tspt membercategory [number | names]\n\t\tEx: spt membercategory 1026\n\t\tEx: spt membercategory unheard+uniqueid";

    public async ValueTask<string> PerformAction(UserDialogInfo commandHandler, MongoId sessionId, SendMessageRequest request)
    {
        var args = string.Join(' ', request.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(2));
        var profile = profileHelper.GetFullProfile(sessionId);
        var info = profile.CharacterData?.PmcData?.Info;

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
            reply = "You can add:\n" + string.Join("\n", Enum.GetValues<MemberCategory>().Where(c => (c & ~Allowed) == 0).Select(c => $"{(int)c} = {c}"));
        }
        else if (!Enum.TryParse<MemberCategory>(args.Replace(" ", "").Replace('+', ','), true, out var value) || (value & ~Allowed) != 0)
        {
            reply = "That can't be set. Type 'spt membercategory list' to see what you can add.";
        }
        else
        {
            info.MemberCategory = value;

            // If the shown icon is gone, go back to the one that comes with the game edition
            var selected = info.SelectedMemberCategory ?? MemberCategory.Default;
            if ((value & selected) != selected)
            {
                info.SelectedMemberCategory = profile.ProfileInfo?.Edition?.ToLowerInvariant() switch
                {
                    "unheard" => MemberCategory.Unheard | MemberCategory.UniqueId,
                    "edge of darkness" => MemberCategory.UniqueId,
                    _ => MemberCategory.Default,
                };
            }

            await saveServer.SaveProfileAsync(sessionId);
            reply = $"Done! You now have {Describe(value)}. Fully restart the game to see it.";
        }

        mailSendService.SendUserMessageToPlayer(sessionId, commandHandler, reply);
        return request.DialogId;
    }

    private static string Describe(MemberCategory value)
    {
        var names = Enum.GetValues<MemberCategory>().Where(c => c != MemberCategory.Default && value.HasFlag(c));
        return $"{(int)value} ({(value == MemberCategory.Default ? "Default" : string.Join(" + ", names))})";
    }
}
