using TouMegaChujoweExtension.Utilities;
using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events;
using MiraAPI.Utilities;

namespace TouMegaChujoweExtension.Events.Crewmate;

public static class PresidentEvents
{
    [RegisterEvent]
    public static void OnMeetingStart(StartMeetingEvent @event)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player.Data.Role is not PresidentRole presidentRole)
            {
                continue;
            }

            // TryGetVoteData: GetVoteData throws when a player has no vote data, so the null check
            // that used to be here could never fire and the throw escaped the loop instead.
            if (!player.TryGetVoteData(out var voteData))
            {
                continue;
            }

            var totalVotes = 1 + presidentRole.VoteBank;
            voteData.SetRemainingVotes(totalVotes);
        }
    }

    [RegisterEvent]
    public static void OnMeetingEnd(EndMeetingEvent @event)
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