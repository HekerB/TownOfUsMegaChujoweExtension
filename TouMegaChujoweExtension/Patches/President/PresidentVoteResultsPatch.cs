using System.Collections.Generic;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting.Voting;
using TouMegaChujoweExtension.Roles.Crewmate;

namespace TouMegaChujoweExtension.Events.Crewmate;

public static class PresidentVoteResultsHandler
{
    [RegisterEvent]
    public static void OnPopulateResults(PopulateResultsEvent @event)
    {
        var presidentPlayers = new HashSet<byte>();
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player.Data.Role is PresidentRole)
            {
                presidentPlayers.Add(player.PlayerId);
            }
        }

        if (presidentPlayers.Count == 0)
        {
            return;
        }
    }
}
