using AmongUs.GameOptions;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using Reactor.Utilities;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using UnityEngine;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class GunGameEvents
{
    private static float lastMutationTime;

    [RegisterEvent(10000)]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (!@event.TriggeredByIntro)
        {
            return;
        }

        GunGameRole.ResetState();
        lastMutationTime = 0f;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.Data?.Role is GunGameRole)
            {
                continue;
            }

            if (player != null && player.TryGetModifier<GunGameModifier>(out var modifier))
            {
                player.RemoveModifier(modifier);
            }
        }
    }

    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        TryMutate(@event.Source);
    }

    private static void TryMutate(PlayerControl source)
    {
        if (source == null || !source.AmOwner)
        {
            return;
        }

        if (Time.time - lastMutationTime < 0.5f || !source.HasModifier<GunGameModifier>())
        {
            return;
        }

        var options = OptionGroupSingleton<GunGameOptions>.Instance;
        var killsNeeded = Mathf.Clamp((int)options.KillsNeededToChangeRole, 1, 5);
        var currentKills = GunGameRole.CurrentKillCount.GetValueOrDefault(source.PlayerId) + 1;
        GunGameRole.CurrentKillCount[source.PlayerId] = currentKills;

        if (currentKills < killsNeeded)
        {
            return;
        }

        lastMutationTime = Time.time;
        Coroutines.Start(CoMutateDelayed());
    }

    private static System.Collections.IEnumerator CoMutateDelayed()
    {
        yield return new WaitForSeconds(0.05f);
        GunGameRole.TriggerMutationLocal();
    }
}
