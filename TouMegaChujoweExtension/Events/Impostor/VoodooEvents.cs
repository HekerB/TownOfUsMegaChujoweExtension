using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class VoodooEvents
{
    [RegisterEvent]
    public static void StartMeetingEventHandler(StartMeetingEvent _)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || !player.TryGetModifier<VoodooScheduledCurseModifier>(out var scheduledCurse))
            {
                continue;
            }

            if (scheduledCurse.CurseType == VoodooEffect.Mute)
            {
                var meetings = (int)OptionGroupSingleton<VoodooMasterOptions>.Instance.MuteDuration;
                player.RpcAddModifier<VoodooMutedModifier>(meetings);
            }

            player.RpcRemoveModifier(scheduledCurse.UniqueId);
        }
    }

    [RegisterEvent]
    public static void EndMeetingEventHandler(EndMeetingEvent _)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null)
            {
                continue;
            }

            if (player.TryGetModifier<VoodooMutedModifier>(out var muted))
            {
                muted.MeetingsRemaining--;
                if (muted.MeetingsRemaining <= 0)
                {
                    player.RpcRemoveModifier<VoodooMutedModifier>();
                }
            }

        }
    }
}
