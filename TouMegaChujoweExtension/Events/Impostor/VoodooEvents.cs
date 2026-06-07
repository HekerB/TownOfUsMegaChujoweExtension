using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class VoodooEvents
{
    [RegisterEvent]
    public static void StartMeetingEventHandler(StartMeetingEvent @event)
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
                player.RpcAddModifier<VoodooMutedModifier>();
            }

            player.RpcRemoveModifier(scheduledCurse.UniqueId);
        }
    }

    [RegisterEvent]
    public static void EndMeetingEventHandler(EndMeetingEvent @event)
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

            if (player.HasModifier<VoodooMutedModifier>())
            {
                player.RpcRemoveModifier<VoodooMutedModifier>();
            }

        }
    }
}
