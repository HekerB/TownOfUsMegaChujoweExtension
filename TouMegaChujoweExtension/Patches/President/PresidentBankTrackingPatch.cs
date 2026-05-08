using System.Collections.Generic;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Crewmate;

namespace TouMegaChujoweExtension.Events.Crewmate;

public static class PresidentBankTracking
{
    private const byte BlackmailVoteId = 252;
    private static readonly Dictionary<byte, int> VotesUsedThisMeeting = new();

    [RegisterEvent]
    public static void OnMeetingStart(StartMeetingEvent @event)
    {
        VotesUsedThisMeeting.Clear();
    }

    [RegisterEvent]
    public static void OnHandleVote(HandleVoteEvent @event)
    {
        var player = @event.VoteData.Owner;
        if (player?.Data.Role is not PresidentRole)
        {
            return;
        }

        // Don't count abstain or blackmail forced votes as "used votes from bank"
        if (@event.TargetId == PresidentRole.AbstainTargetId || @event.TargetId == BlackmailVoteId)
        {
            return;
        }

        if (!VotesUsedThisMeeting.TryAdd(player.PlayerId, 1))
        {
            VotesUsedThisMeeting[player.PlayerId]++;
        }
    }

    [RegisterEvent]
    public static void OnVotingComplete(VotingCompleteEvent @event)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player.Data.Role is not PresidentRole presidentRole)
            {
                continue;
            }

            if (!VotesUsedThisMeeting.TryGetValue(player.PlayerId, out var votesUsed))
            {
                votesUsed = 0;
            }

            // First vote is free, extra votes cost from bank
            var bankUsed = System.Math.Max(0, votesUsed - 1);
            presidentRole.VoteBank = System.Math.Max(0, presidentRole.VoteBank - bankUsed);
        }

        VotesUsedThisMeeting.Clear();
    }
}
