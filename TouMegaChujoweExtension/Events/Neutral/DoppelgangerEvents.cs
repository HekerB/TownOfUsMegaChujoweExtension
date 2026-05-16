using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TownOfUs.Assets;
using TownOfUs.Events;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class DoppelgangerEvents
{
    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        var killer = @event.Source;
        var victim = @event.Target;

        if (killer == null || victim == null || !killer.IsRole<DoppelgangerRole>())
        {
            return;
        }

        if (killer.PlayerId == victim.PlayerId || MeetingHud.Instance || ExileController.Instance)
        {
            return;
        }

        var role = killer.GetRole<DoppelgangerRole>();
        if (role == null || role.RemainingIdentityThefts == 0)
        {
            return;
        }

        if (role.RemainingIdentityThefts > 0)
        {
            role.RemainingIdentityThefts--;
        }

        // Already disguised - update target locally (event runs on all clients)
        if (killer.TryGetModifier<DoppelgangerDisguiseModifier>(out var existing))
        {
            existing.UpdateTarget(victim);

            // Hide First Dead Shield visual if the killer has it
            if (killer.TryGetModifier<FirstDeadShield>(out var s1) && s1.FirstRoundShield != null)
            {
                s1.FirstRoundShield.SetActive(false);
            }

            if (killer.AmOwner)
            {
                TouAudio.PlaySound(TouAudio.MimicSound);
            }
            return;
        }

        // First disguise - only owner sends RPC
        if (!killer.AmOwner)
        {
            return;
        }

        TouAudio.PlaySound(TouAudio.MimicSound);
        killer.RpcAddModifier<DoppelgangerDisguiseModifier>(victim);

        if (killer.TryGetModifier<FirstDeadShield>(out var s2) && s2.FirstRoundShield != null)
        {
            s2.FirstRoundShield.SetActive(false);
        }
    }

    [RegisterEvent]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        var source = @event.Source;
        var target = @event.Target;

        if (source == null || target == null || !source.IsRole<DoppelgangerRole>() || @event.IsCancelled)
        {
            return;
        }

        if (source.PlayerId == target.PlayerId)
        {
            return;
        }

        if (source.AmOwner)
        {
            DeathHandlerModifier.UpdateDeathHandlerImmediate(
                target,
                TouLocale.Get("DiedToDoppelganger"),
                DeathEventHandlers.CurrentRound,
                (!MeetingHud.Instance && !ExileController.Instance)
                    ? DeathHandlerOverride.SetTrue
                    : DeathHandlerOverride.SetFalse,
                TouLocale.GetParsed("DiedByStringBasic")
                    .Replace("<player>", source.Data.PlayerName),
                lockInfo: DeathHandlerOverride.SetTrue
            );
        }
    }

    [RegisterEvent]
    public static void RoundStartHandler(RoundStartEvent @event)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || !player.IsRole<DoppelgangerRole>())
            {
                continue;
            }

            if (player.TryGetModifier<DoppelgangerDisguiseModifier>(out var disguise))
            {
                player.RemoveModifier(disguise);
            }

            var role = player.GetRole<DoppelgangerRole>();
            if (role != null)
            {
                var maxSteals = (int)OptionGroupSingleton<DoppelgangerOptions>.Instance.MaxSteals;
                role.RemainingIdentityThefts = maxSteals == 0 ? -1 : maxSteals;
            }
        }
    }
}

















