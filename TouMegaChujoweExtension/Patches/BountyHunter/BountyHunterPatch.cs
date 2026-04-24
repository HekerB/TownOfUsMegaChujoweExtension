using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Networking;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Patches.BountyHunter;

public static class BountyHunterHelper
{
    public static bool IsBountyHunter(this PlayerControl player)
    {
        if (player == null || player.Data == null || player.Data.Role == null)
            return false;
        return player.Data.Role is BountyHunterRole;
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
public static class BountyHunterMeetingPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        var bh = PlayerControl.LocalPlayer;
        if (bh == null || !bh.IsBountyHunter()) return;
        if (BountyHunterSystem.HasWon) return;
        if (!BountyHunterSystem.Hunting) return;

        var opts = OptionGroupSingleton<BountyHunterOptions>.Instance;
        if (opts.DiesIfTargetNotKilled && !BountyHunterSystem.TargetKilledThisRound
            && BountyHunterSystem.CurrentTarget != null)
        {
            bh.RpcCustomMurder(bh);
            BountyHunterSystem.ClearArrowModifiers();
        }

        BountyHunterSystem.TargetKilledThisRound = false;
    }
}

[HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
public static class BountyHunterMeetingEndPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        var bh = PlayerControl.LocalPlayer;
        if (bh == null || !bh.IsBountyHunter()) return;
        if (bh.Data.IsDead || BountyHunterSystem.HasWon) return;
        if (!BountyHunterSystem.Hunting) return;

        BountyHunterSystem.CurrentTarget = null;
        BountyHunterSystem.LastTargetPlayerId = null;
        BountyHunterSystem.AssignNewTarget(bh);
    }
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class BountyHunterHudUpdatePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (PlayerControl.LocalPlayer == null) return;
        if (!PlayerControl.LocalPlayer.IsBountyHunter()) return;

        var player = PlayerControl.LocalPlayer;

        if (player.Data == null || player.Data.IsDead)
        {
            BountyHunterSystem.ClearArrowModifiers();
            return;
        }

        if (!ShipStatus.Instance) return;
        if (BountyHunterSystem.HasWon) return;
        if (MeetingHud.Instance || ExileController.Instance) return;

        if (!BountyHunterSystem.IntroFinished)
        {
            if (Object.FindObjectOfType<IntroCutscene>() != null)
                return;

            BountyHunterSystem.IntroFinished = true;
            BountyHunterSystem.IntroFinishTime = Mathf.Max(Time.time, 0.001f);
            return;
        }

        if (!BountyHunterSystem.Hunting)
        {
            if (BountyHunterSystem.IntroFinishTime <= 0f)
                return;

            if (Time.time - BountyHunterSystem.IntroFinishTime < 10f)
                return;

            BountyHunterSystem.Hunting = true;
            BountyHunterSystem.AssignNewTarget(player);
            return;
        }

        if (!BountyHunterSystem.HasWon &&
            (BountyHunterSystem.CurrentTarget == null ||
             BountyHunterSystem.CurrentTarget.Data == null ||
             BountyHunterSystem.CurrentTarget.Data.IsDead ||
             BountyHunterSystem.CurrentTarget.Data.Disconnected))
        {
            BountyHunterSystem.AssignNewTarget(player);
        }
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class BountyHunterGameEndPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        BountyHunterSystem.Reset();
    }
}