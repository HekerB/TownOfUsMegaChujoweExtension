using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class WraithEvents
{
    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (@event.TriggeredByIntro)
        {
            WraithLanternSystem.ClearAll();
        }
    }
}












