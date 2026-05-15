using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Utilities;
using TownOfUs.Networking;
using UnityEngine;
using System.Collections;
using Reactor.Utilities;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class ZombieEvents
{
    private static int _meetingCount;

    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (@event.TriggeredByIntro)
        {
            _meetingCount = 0;
        }
    }

    [RegisterEvent]
    public static void StartMeetingEventHandler(StartMeetingEvent @event)
    {
        _meetingCount++;
        Coroutines.Start(CoMonitorMeetingEnd());
    }

    private static IEnumerator CoMonitorMeetingEnd()
    {
        while (MeetingHud.Instance != null)
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) yield break;

        var options = OptionGroupSingleton<ZombieOptions>.Instance;
        
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied() || player.Data.Role is not ZombieRole role) continue;

            // Check how many meetings since they became a zombie? 
            // The user said "po konfigurowalnej ilości rund... zombie umiera po meetingu".
            // If they are a zombie for 2 meetings, they rot.
            
            role.MeetingCount++;
            if (role.MeetingCount >= options.MeetingsUntilDeath)
            {
                CustomTouMurderRpcs.RpcSpecialMurder(player, player, causeOfDeath: "Rotten");
            }
        }
    }
}
