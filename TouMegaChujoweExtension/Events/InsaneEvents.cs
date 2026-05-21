using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Events;
using MiraAPI.Modifiers;
using MiraAPI.Modifiers.ModifierDisplay;
using TouMegaChujoweExtension.Modifiers;
using TownOfUs.Extensions;

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

    [RegisterEvent]
    public static void CompleteTaskEventHandler(CompleteTaskEvent @event)
    {
        if (@event.Player != null && @event.Player.AmOwner && @event.Player.HasModifier<InsaneModifier>())
        {
            var modifier = @event.Player.GetModifier<InsaneModifier>();
            if (modifier != null && modifier.CompletedAllTasks())
            {
                // Force HUD reset to show the newly revealed modifier
                if (HudManager.InstanceExists)
                {
                    HudManager.Instance.SetHudActive(false);
                    HudManager.Instance.SetHudActive(true);

                    var modsTab = ModifierDisplayComponent.Instance;
                    if (modsTab != null && !modsTab.IsOpen)
                    {
                        modsTab.ToggleTab();
                    }
                }
            }
        }
    }
}
