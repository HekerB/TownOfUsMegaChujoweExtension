using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Utilities;

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
                player.RpcAddModifier<VoodooMutedModifier>(1);
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

            if (player.TryGetModifier<VoodooTargetLockModifier>(out var targetLock))
            {
                targetLock.MeetingsRemaining--;
                if (targetLock.MeetingsRemaining <= 0)
                {
                    player.RpcRemoveModifier(targetLock.UniqueId);
                }
            }
        }
    }

    [RegisterEvent]
    public static void PlayerDeathEventHandler(MiraAPI.Events.Vanilla.Player.PlayerDeathEvent @event)
    {
        var victim = @event.Player;
        if (victim == null)
        {
            return;
        }

        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        RemoveMuteFromDeadPlayer(victim);

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied())
            {
                continue;
            }

            if (player.TryGetModifier<VoodooTargetLockModifier>(out var targetLock))
            {
                if (targetLock.TargetId == victim.PlayerId)
                {
                    player.RpcRemoveModifier(targetLock.UniqueId);
                }
            }
        }
    }

    [RegisterEvent]
    public static void OnEjection(EjectionEvent @event)
    {
        var exiled = @event.ExileController?.initData?.networkedPlayer?.Object;
        if (exiled == null)
        {
            return;
        }

        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        RemoveMuteFromDeadPlayer(exiled);

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.HasDied())
            {
                continue;
            }

            if (player.TryGetModifier<VoodooTargetLockModifier>(out var targetLock))
            {
                if (targetLock.TargetId == exiled.PlayerId)
                {
                    player.RpcRemoveModifier(targetLock.UniqueId);
                }
            }
        }
    }

    private static void RemoveMuteFromDeadPlayer(PlayerControl player)
    {
        if (player.HasModifier<VoodooMutedModifier>())
        {
            player.RpcRemoveModifier<VoodooMutedModifier>();
        }

        foreach (var scheduledMute in player.GetModifiers<VoodooScheduledCurseModifier>()
                     .Where(curse => curse.CurseType == VoodooEffect.Mute)
                     .ToArray())
        {
            player.RpcRemoveModifier(scheduledMute.UniqueId);
        }
    }
}
