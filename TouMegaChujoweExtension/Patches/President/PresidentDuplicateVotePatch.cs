using System.Reflection;
using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using MiraAPI.Voting;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Modifiers.Impostor;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.President;

[HarmonyPatch]
public static class PresidentDuplicateVotePatch
{
    /// <summary>
    /// The target ID used by BlackmailedModifier for forced votes.
    /// </summary>
    private const byte BlackmailVoteId = 252;

    [HarmonyTargetMethod]
    public static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(VotingUtils), "HandleVote");
    }

    [HarmonyPrefix]
    public static bool Prefix(PlayerVoteData voteData, byte suspectIdx, out bool cancelVote)
    {
        cancelVote = false;

        if (voteData.Owner?.Data.Role is not PresidentRole presidentRole)
        {
            return true;
        }

        // === BLACKMAIL FORCED VOTE (252) ===
        if (suspectIdx == BlackmailVoteId)
        {
            voteData.SetRemainingVotes(0);
            cancelVote = false;
            return false;
        }

        // === ABSTAIN (250) ===
        if (suspectIdx == PresidentRole.AbstainTargetId)
        {
            presidentRole.DoAbstain();
            voteData.SetRemainingVotes(0);
            cancelVote = false;
            return false;
        }

        // === NORMAL VOTE ===
        var @event = new HandleVoteEvent(voteData, suspectIdx);
        MiraEventManager.InvokeEvent(@event);

        cancelVote = @event.PreventVote;

        if (@event.IsCancelled)
        {
            return false;
        }

        if (voteData.VotesRemaining <= 0)
        {
            cancelVote = true;
            return false;
        }

        voteData.DecreaseRemainingVotes(1);
        voteData.VoteForPlayer(suspectIdx);

        // Mark that president has voted on a player - hide Abstain button
        presidentRole.HasVotedOnPlayer = true;

        cancelVote = false;
        return false;
    }
}

public static class PresidentSelectHandler
{
    [RegisterEvent]
    public static void OnMeetingSelect(MeetingSelectEvent @event)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer?.Data.Role is not PresidentRole)
        {
            return;
        }

        if (@event.TargetId != PresidentRole.AbstainTargetId && @event.VoteData.VotesRemaining > 0)
        {
            @event.AllowSelect = true;
        }
    }
}