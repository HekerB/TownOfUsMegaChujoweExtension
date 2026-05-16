using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Hud;

namespace TouMegaChujoweExtension.Events.Crewmate;

public static class TrapperEvents
{
    [RegisterEvent]
    public static void EjectionEventHandler(EjectionEvent @event)
    {
        VentTrapSystem.DecrementRoundsAndRemoveExpired();
    }
}