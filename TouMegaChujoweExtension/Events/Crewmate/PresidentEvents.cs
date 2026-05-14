using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events;
using MiraAPI.Utilities;

namespace TouMegaChujoweExtension.Events.Crewmate;

public static class PresidentEvents
{
    [RegisterEvent]
    public static void OnMeetingStart()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player.Data.Role is not PresidentRole presidentRole)
            {
                continue;
            }

            var voteData = player.GetVoteData();
            if (voteData == null)
            {
                continue;
            }

            var totalVotes = 1 + presidentRole.VoteBank;
            voteData.SetRemainingVotes(totalVotes);
        }
    }

    [RegisterEvent]
    public static void OnMeetingEnd()
    {
        if (PlayerControl.AllPlayerControls == null) return;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.Data?.Role is PresidentRole presidentRole)
            {
                try
                {
                    presidentRole.OnMeetingEnd();
                }
                catch
                {
                    // zzz
                    // zz.
                }
            }
        }
    }
}