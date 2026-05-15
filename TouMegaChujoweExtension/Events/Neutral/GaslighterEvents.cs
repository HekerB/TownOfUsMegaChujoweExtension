using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Voting;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Networking;
using TownOfUs.Utilities;
using UnityEngine;
using System.Collections;
using System.Linq;
using Reactor.Utilities;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class GaslighterEvents
{
    private static int _meetingCount = 0;

    public static int GetMeetingCount() => _meetingCount;

    [RegisterEvent]
    public static void StartMeetingEventHandler(StartMeetingEvent @event)
    {
        _meetingCount++;
        
        // Update all Gaslighters' meeting count
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.Data.Role is GaslighterRole role)
            {
                role.MeetingCount = _meetingCount;
            }
        }

        Coroutines.Start(CoMonitorMeetingEnd());
    }

    private static IEnumerator CoMonitorMeetingEnd()
    {
        while (MeetingHud.Instance != null)
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) yield break;

        // Process Cursed players
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied()) continue;

            var curseMod = player.GetModifier<GaslighterCursedModifier>();
            if (curseMod == null) continue;

            // If the Gaslighter is still alive and not voted out
            var gaslighter = MiscUtils.PlayerById(curseMod.GaslighterId);
            if (gaslighter != null && !gaslighter.HasDied())
            {
                // Kill the cursed player
                CustomTouMurderRpcs.RpcSpecialMurder(gaslighter, player, causeOfDeath: "Cursed");
            }
            
            // Remove curse
            player.RemoveModifier(curseMod);
        }
    }

    [RegisterEvent]
    public static void ProcessVotesEventHandler(ProcessVotesEvent @event)
    {
        var votes = @event.Votes.ToList();

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            var mods = player.GetModifiers<GaslighterKnightedModifier>()?.ToList();
            if (mods == null || mods.Count == 0) continue;

            var vote = votes.FirstOrDefault(v => v.Voter == player.PlayerId);
            if (vote == default) continue;

            // Add 1 extra vote per modifier
            for (var i = 0; i < mods.Count; i++)
            {
                votes.Add(new CustomVote(vote.Voter, vote.Suspect));
            }
        }

        @event.ExiledPlayer = VotingUtils.GetExiled(votes, out _);
    }

    [RegisterEvent]
    public static void HandleVoteEvent(HandleVoteEvent @event)
    {
        if (!@event.VoteData.Owner.HasModifier<GaslighterKnightedModifier>()) return;

        @event.VoteData.SetRemainingVotes(0);

        // 1 base + 1 extra = 2 total
        for (var i = 0; i < 2; i++)
        {
            @event.VoteData.VoteForPlayer(@event.TargetId);
        }

        @event.Cancel();
    }
}
