using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TouMegaChujoweExtension.Modifiers.Impostor;
using UnityEngine;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class GunGameEvents
{
    private static float _lastMutationTime;

    private static void TryMutate(PlayerControl source)
    {
        if (source == null || !source.AmOwner) return;
        if (Time.time - _lastMutationTime < 0.5f) return; // Prevent double mutation

        // If they are a GunGame player but currently in a mutated role, check if they should still mutate
        if (!source.HasModifier<GunGameModifier>()) return;

        // If they are vanilla Impostor (final chain stage), no more mutations
        if (GunGameRole.CurrentChainIndex.TryGetValue(source.PlayerId, out int idx))
        {
            // Check if using chain and past the chain length + 1 (vanilla Impostor = no more mutations)
            var options = OptionGroupSingleton<GunGameOptions>.Instance;
            if (options.UseLethalChain && source.Data?.Role?.Role == RoleTypes.Impostor && idx > GunGameRole.Chain.Count)
            {
                return; // Final stage, no more mutations
            }
        }

        _lastMutationTime = Time.time;
        Reactor.Utilities.Coroutines.Start(CoMutateDelayed());
    }

    private static System.Collections.IEnumerator CoMutateDelayed()
    {
        yield return new WaitForSeconds(0.05f);
        GunGameRole.TriggerMutationLocal();
    }

    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        if (@event == null || @event.Source == null) return;
        TryMutate(@event.Source);
    }
}
