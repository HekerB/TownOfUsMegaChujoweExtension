using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.Events;
using System.Collections.Generic;

namespace TouMegaChujoweExtension.Patches.Roles.President;

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














