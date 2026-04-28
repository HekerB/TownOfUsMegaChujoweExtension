using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Usables;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Crewmate;
using TouMegaChujoweExtension.Options.Modifiers;

namespace TouMegaChujoweExtension.Events.Modifiers;

public static class VentableEvents
{
    [RegisterEvent]
    public static void ExitVentEventHandler(ExitVentEvent @event)
    {
        var player = @event.Player;
        var vent = @event.Vent;

        if (vent == null || !player.TryGetModifier<VentableModifier>(out var ventableMod))
        {
            return;
        }

        --ventableMod.VentsRemaining;
        ventableMod.CooldownTimer = OptionGroupSingleton<VentableModifierOptions>.Instance.VentCooldown.Value;
    }
}
