using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Hud;
using TouMegaChujoweExtension.Buttons.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Buttons;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class OutlawEvents
{
    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (@event.TriggeredByIntro) return;

        var button = CustomButtonSingleton<OutlawKillButton>.Instance;
        if (button != null)
        {
            button.ResetState();
            button.ResetCooldownAndOrEffect();
        }
    }

    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        if (!@event.Source.AmOwner || !@event.Source.IsRole<OutlawRole>()) return;

        var button = CustomButtonSingleton<OutlawKillButton>.Instance;
        if (button != null)
        {
            button.HandleSuccessfulKill();
        }
    }
}