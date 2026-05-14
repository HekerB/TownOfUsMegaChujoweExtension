using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Usables;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TownOfUs.Utilities;

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

        ventableMod.CooldownTimer = OptionGroupSingleton<VentableModifierOptions>.Instance.VentCooldown.Value;
    }

    [RegisterEvent]
    public static void RoundStartEventHandler()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied())
            {
                continue;
            }

            if (player.TryGetModifier<VentableModifier>(out var ventableMod))
            {
                ventableMod.CooldownTimer = OptionGroupSingleton<VentableModifierOptions>.Instance.VentCooldown.Value;
            }
        }
    }
}













