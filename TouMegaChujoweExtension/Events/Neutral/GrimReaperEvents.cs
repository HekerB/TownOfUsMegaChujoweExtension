using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Events;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TownOfUs.Extensions;
using MiraAPI.Modifiers;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class GrimReaperEvents
{
    [RegisterEvent]
    public static void OnMeetingStart(StartMeetingEvent @event)
    {
        GrimReaperSystem.OnMeetingStart();
    }

    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (@event.TriggeredByIntro)
        {
            GrimReaperSystem.ClearAll();
        }
    }

    [RegisterEvent]
    public static void OnPlayerDeath(PlayerDeathEvent @event)
    {
        if (@event.Player == null) return;
        if (@event.DeathReason == DeathReason.Exile) return;

        if (GrimReaperSystem.HasReaperInGame() && @event.Player.HasModifier<GrimReaperMarkedModifier>())
        {
            GrimReaperSystem.SpawnSoul(@event.Player.PlayerId, @event.Player.transform.position);
        }
    }
}
