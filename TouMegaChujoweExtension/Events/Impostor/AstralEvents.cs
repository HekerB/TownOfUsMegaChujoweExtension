using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TownOfUs.Extensions;
using MiraAPI.Modifiers;
using System.Collections.Generic;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Modifiers.Crewmate;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class AstralEvents
{
    private static readonly HashSet<byte> _killInProgress = [];

    [RegisterEvent]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        var killer = @event.Source;
        if (killer == null || killer.Data == null || !killer.AmOwner || _killInProgress.Contains(killer.PlayerId)) return;

        if (killer.Data.Role is AstralRole && killer.HasModifier<AstralPhaseModifier>() && killer != @event.Target)
        {
            if (@event.IsCancelled)
            {
                @event.Cancel();
                return;
            }

            @event.Cancel();
            _killInProgress.Add(killer.PlayerId);
            try
            {
                killer.RpcSpecialMurder(@event.Target, causeOfDeath: "AstralVoid");
            }
            finally
            {
                _killInProgress.Remove(killer.PlayerId);
            }
        }
    }

    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        var killer = @event.Source;
        if (killer == null || killer.Data == null) return;

        if (killer.Data.Role is AstralRole astral && killer.HasModifier<AstralPhaseModifier>())
        {
            if (@event.Target != null && @event.Target.HasDied())
            {
                astral.KillMadeDuringPhase = true;
            }
        }
    }
}

