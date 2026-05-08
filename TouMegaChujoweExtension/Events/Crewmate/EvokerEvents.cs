using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Events.Crewmate;

public static class EvokerEvents
{
    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (@event.TriggeredByIntro)
        {
            EvokerSystem.VerifiedPlayers.Clear();
        }
    }
}
