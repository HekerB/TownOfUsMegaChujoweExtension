using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers;

namespace TouMegaChujoweExtension.Events;

public static class InsaneEvents
{
    [RegisterEvent]
    public static void GameEndEventHandler(GameEndEvent @event)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null)
            {
                continue;
            }

            if (player.TryGetModifier<InsaneModifier>(out var mod))
            {
                player.RemoveModifier(mod);
            }
        }
    }
}
