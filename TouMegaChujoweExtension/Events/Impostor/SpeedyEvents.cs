using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Hud;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TouMegaChujoweExtension.Buttons.Classic.Impostor;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class SpeedyEvents
{
    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        var killer = @event.Source;
        if (killer == null || killer.Data == null) return;

        if (killer.Data.Role is SpeedyRole speedy)
        {
            if (speedy.KillsCount == 0 && killer.AmOwner)
            {
                var button = CustomButtonSingleton<SpeedyAccelerateButton>.Instance;
                if (button != null)
                {
                    button.Timer = button.Cooldown;
                }
            }
            speedy.KillsCount++;
        }
    }
}
