using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events;

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