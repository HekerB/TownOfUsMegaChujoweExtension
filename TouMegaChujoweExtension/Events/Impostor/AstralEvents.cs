using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TownOfUs.Extensions;
using MiraAPI.Modifiers;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class AstralEvents
{
    private static readonly System.Collections.Generic.HashSet<byte> _killInProgress = new();

    [RegisterEvent]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        var killer = @event.Source;
        if (killer == null || killer.Data == null || _killInProgress.Contains(killer.PlayerId)) return;

        if (killer.Data.Role is AstralRole && killer.HasModifier<AstralPhaseModifier>() && killer != @event.Target)
        {
            @event.Cancel();
            _killInProgress.Add(killer.PlayerId);
            killer.RpcSpecialMurder(@event.Target, causeOfDeath: "AstralVoid");
        }
    }

    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        var killer = @event.Source;
        if (killer == null || killer.Data == null) return;

        _killInProgress.Remove(killer.PlayerId);

        if (killer.Data.Role is AstralRole astral)
        {
            if (killer.HasModifier<AstralPhaseModifier>())
            {
                astral.KillMadeDuringPhase = true;
            }
        }
    }
}
