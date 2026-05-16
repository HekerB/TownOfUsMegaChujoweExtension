using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.Events;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using TownOfUs.Modifiers.Impostor;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Events.Crewmate;

public static class PresidentAbstainEvents
{
    [RegisterEvent(999)]
    public static void OnBeforeVote(BeforeVoteEvent @event)
    {
        if (PlayerControl.LocalPlayer?.Data.Role is not PresidentRole president)
        {
            return;
        }

        var meeting = MeetingHud.Instance;
        if (meeting == null)
        {
            return;
        }

        // If blackmailed and active: block Abstain, block Skip, block all voting
        // BlackmailedModifier.FixedUpdate handles the forced Confirm(252) automatically
        if (president.IsBlackmailActive())
        {
            // Block abstain button
            if (@event.VoteArea == president.AbstainButton)
            {
                @event.Cancel();
                return;
            }

            // Block skip button
            if (@event.VoteArea == meeting.SkipVoteButton)
            {
                @event.Cancel();
                return;
            }

            // Block all player votes too - blackmail silences completely
            // (BlackmailedModifier will force the vote via FixedUpdate)
            @event.Cancel();
            return;
        }

        // Handle Abstain button click
        if (@event.VoteArea == president.AbstainButton)
        {
            if (president.HasAbstained)
            {
                @event.Cancel();
                return;
            }

            if (!president.SelectingAbstain)
            {
                president.SelectingAbstain = true;
                meeting.SkipVoteButton.ClearButtons();
            }
            return;
        }

        // Handle Skip button click - clear abstain state
        if (@event.VoteArea == meeting.SkipVoteButton)
        {
            if (president.SelectingAbstain)
            {
                president.SelectingAbstain = false;
                president.AbstainButton?.ClearButtons();
            }
            return;
        }

        // Handle any player vote area click - clear abstain state
        if (president.SelectingAbstain)
        {
            president.SelectingAbstain = false;
            president.AbstainButton?.ClearButtons();
        }
    }

    [RegisterEvent]
    public static void OnMeetingSelect(MeetingSelectEvent @event)
    {
        if (PlayerControl.LocalPlayer?.Data.Role is not PresidentRole president)
        {
            return;
        }

        // If blackmailed and active, block all selections
        if (president.IsBlackmailActive())
        {
            @event.AllowSelect = false;
            return;
        }

        if (@event.TargetId == PresidentRole.AbstainTargetId)
        {
            if (!president.HasAbstained && @event.VoteData.VotesRemaining > 0)
            {
                @event.AllowSelect = true;
            }
            else
            {
                @event.AllowSelect = false;
            }
        }
    }
}