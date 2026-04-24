using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using TouMegaChujoweExtension.Modules;

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