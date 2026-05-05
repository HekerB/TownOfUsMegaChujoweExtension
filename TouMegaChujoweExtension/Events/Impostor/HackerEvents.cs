using AmongUs.GameOptions;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.GameOptions;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class HackerEvents
{
    private static bool IsHackerRole(PlayerControl? player)
    {
        if (player == null || player.Data?.Role == null)
        {
            return false;
        }

        if (player.Data.Role is HackerRole)
        {
            return true;
        }

        try
        {
            return player.Data.Role.Role == (RoleTypes)RoleId.Get<HackerRole>();
        }
        catch
        {
            return false;
        }
    }

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
