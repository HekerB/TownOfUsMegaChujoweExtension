using AmongUs.GameOptions;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Roles;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class HackerEvents
{

    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (!@event.TriggeredByIntro)
        {
            return;
        }

        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || PlayerControl.LocalPlayer == null)
        {
            return;
        }

        HackerSystem.ResetAll();
    }

    [RegisterEvent]
    public static void GameEndEventHandler(GameEndEvent @event)
    {
        HackerSystem.ResetAll();
    }

    [RegisterEvent]
    public static void StartMeetingEventHandler(StartMeetingEvent @event)
    {
        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer != null)
        {
            HackerRole.RpcHackerResetRound(PlayerControl.LocalPlayer);
        }
    }
}














