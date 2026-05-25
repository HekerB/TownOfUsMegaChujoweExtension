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

public static class LonerEvents
{
    private static float lastMutationTime;

    [RegisterEvent(10000)]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (!@event.TriggeredByIntro)
        {
            return;
        }

        LonerRole.ResetState();
        lastMutationTime = 0f;
        LonerRole.RegisterExistingImpostorRoles();

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.Data?.Role is LonerRole)
            {
                continue;
            }

            if (player != null && player.TryGetModifier<LonerModifier>(out var modifier))
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

        var options = OptionGroupSingleton<LonerOptions>.Instance;
        if (!options.ChangeRoleAfterKills ||
            Time.time - lastMutationTime < 0.5f ||
            !source.HasModifier<LonerModifier>() ||
            !LonerRole.HasRecruited(source) ||
            LonerRole.HasMutated(source))
        {
            return;
        }

        var killsNeeded = Mathf.Clamp((int)options.KillsNeededToChangeRole.Value, 1, 5);
        var currentKills = LonerRole.CurrentKillCount.GetValueOrDefault(source.PlayerId) + 1;
        LonerRole.CurrentKillCount[source.PlayerId] = currentKills;

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
        LonerRole.TriggerMutationLocal();
    }
}
