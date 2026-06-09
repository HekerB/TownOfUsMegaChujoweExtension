using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class JokerEvents
{
    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (!@event.TriggeredByIntro) return;

        JokerCloneSystem.ClearAll();
    }
}