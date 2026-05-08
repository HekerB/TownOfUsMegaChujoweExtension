using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Patches.Lawyer;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Events;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class LawyerEvents
{
    public static readonly Dictionary<byte, byte> ObjectedVoterOriginalVotes = [];

    public static void ClearObjectedVoters()
    {
        ObjectedVoterOriginalVotes.Clear();
    }

    public static void AddObjectedVoter(byte voterId, byte originalVote)
    {
        ObjectedVoterOriginalVotes[voterId] = originalVote;
    }

    public static bool IsObjectedVoter(byte voterId)
    {
        return ObjectedVoterOriginalVotes.ContainsKey(voterId);
    }

    public static bool TryGetOriginalVote(byte voterId, out byte originalVote)
    {
        return ObjectedVoterOriginalVotes.TryGetValue(voterId, out originalVote);
    }
    [RegisterEvent]
    public static void StartMeetingEventHandler(StartMeetingEvent @event)
    {
        ClearObjectedVoters();
        LawyerVoteBlockPatch.ClearVotes();
    }

    [RegisterEvent]
    public static void EjectionEventHandler(EjectionEvent @event)
    {
        var exiled = @event.ExileController?.initData?.networkedPlayer?.Object;
        if (exiled == null)
        {
            return;
        }

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || !player.IsRole<LawyerRole>())
            {
                continue;
            }

            var lawyer = player.GetRole<LawyerRole>();
            if (lawyer == null || lawyer.Client == null)
            {
                continue;
            }

            if (lawyer.Client.PlayerId == exiled.PlayerId)
            {
                lawyer.ClientVoted = true;

                if (OptionGroupSingleton<LawyerOptions>.Instance.GetVotedOutWithClient)
                {
                    DeathHandlerModifier.UpdateDeathHandlerImmediate(lawyer.Player,
                        TouLocale.Get("ExtensionLawyerDiedWithClient"),
                        DeathEventHandlers.CurrentRound,
                        DeathHandlerOverride.SetFalse,
                        lockInfo: DeathHandlerOverride.SetTrue);
                    lawyer.Player.Exiled();
                }

                lawyer.CheckClientDeath(exiled);
            }
        }
    }

    [RegisterEvent]
    public static void PlayerDeathEventHandler(PlayerDeathEvent @event)
    {
        var victim = @event.Player;
        if (victim == null)
        {
            return;
        }

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || !player.IsRole<LawyerRole>())
            {
                continue;
            }

            var lawyer = player.GetRole<LawyerRole>();
            if (lawyer == null || lawyer.Client == null)
            {
                continue;
            }

            if (lawyer.Client.PlayerId == victim.PlayerId)
            {
                lawyer.CheckClientDeath(victim);
            }
        }
    }
}
