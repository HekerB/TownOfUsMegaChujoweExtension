using System.Linq;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class ApocalypseEvents
{
    [RegisterEvent(10000)]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (!@event.TriggeredByIntro)
        {
            return;
        }

        BerserkerRole.PendingWarAnnouncement = false;

        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        var chance = OptionGroupSingleton<BerserkerOptions>.Instance.InstantWarChance;
        if (chance <= 0f || UnityEngine.Random.Range(0f, 100f) >= chance)
        {
            return;
        }

        foreach (var berserker in PlayerControl.AllPlayerControls.ToArray()
                     .Where(x => x != null && !x.HasDied() && x.Data?.Role is BerserkerRole))
        {
            BerserkerRole.RpcTransformToWar(berserker);
        }
    }

    [RegisterEvent]
    public static void StartMeetingEventHandler(StartMeetingEvent _)
    {
        BerserkerRole.ShowPendingWarAnnouncement();
    }

    [RegisterEvent(100)]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        if (@event.Source == null || @event.Target == null || @event.IsCancelled)
        {
            return;
        }

        if (ApocalypseUtils.AreAllied(@event.Source, @event.Target))
        {
            @event.Cancel();
        }
    }
}
