using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using TownOfUs.Modules;
using TownOfUs.Roles.Crewmate;

namespace TouMegaChujoweExtension.Events.Crewmate;

public static class OfficerEvents
{
    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        var source = @event.Source;
        if (source.Data.Role is OfficerRole && GameHistory.PlayerStats.TryGetValue(source.PlayerId, out var stats))
        {
            stats.CorrectKills += 1;
        }
    }
}