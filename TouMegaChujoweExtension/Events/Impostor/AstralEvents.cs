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

            bool isProtected = GardenerSystem.IsInAnyGarden(@event.Target) ||
                               @event.Target.HasModifier<DoctorShieldModifier>() ||
                               @event.Target.HasModifier<BodyguardShieldModifier>() ||
                               @event.Target.HasModifier<TownOfUs.Modifiers.Crewmate.MedicShieldModifier>() ||
                               @event.Target.HasModifier<TownOfUs.Modifiers.Crewmate.WardenFortifiedModifier>() ||
                               @event.Target.HasModifier<TownOfUs.Modifiers.Crewmate.MagicMirrorModifier>() ||
                               @event.Target.HasModifier<TownOfUs.Modifiers.FirstDeadShield>() ||
                               @event.Target.HasModifier<TownOfUs.Modifiers.Crewmate.ClericBarrierModifier>() ||
                               @event.Target.HasModifier<TownOfUs.Modifiers.Neutral.GuardianAngelProtectModifier>() ||
                               @event.Target.HasModifier<BaseShieldModifier>() ||
                               @event.Target.HasModifier<InvulnerabilityModifier>() ||
                               @event.Target.HasModifier<VeteranAlertModifier>() ||
                               @event.Target.HasModifier<JackalShieldModifier>();

            if (isProtected)
            {
                @event.Cancel();
                return;
            }

            @event.Cancel();
            _killInProgress.Add(killer.PlayerId);
            killer.RpcSpecialMurder(@event.Target, causeOfDeath: "AstralVoid");

            var afterMurderEvent = new AfterMurderEvent(killer, @event.Target, null);
            MiraEventManager.InvokeEvent(afterMurderEvent);
        }
    }

    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        var killer = @event.Source;
        if (killer == null || killer.Data == null) return;

        _killInProgress.Remove(killer.PlayerId);

        if (killer.Data.Role is AstralRole astral && killer.HasModifier<AstralPhaseModifier>())
        {
            astral.KillMadeDuringPhase = true;
        }
    }
}

