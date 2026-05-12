using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Networking;
using Object = UnityEngine.Object;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.BountyHunter;

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
        if (bh == null || bh.Data?.Role is not BountyHunterRole role) return;
        if (role.HasWon) return;
        if (!role.Hunting) return;


        role.TargetKilledThisRound = false;
    }
}

[HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
public static class BountyHunterMeetingEndPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        var bh = PlayerControl.LocalPlayer;
        if (bh == null || bh.Data?.Role is not BountyHunterRole role) return;
        if (bh.Data.IsDead || role.HasWon) return;
        if (!role.Hunting) return;

        role.CurrentTarget = null;
        role.LastTargetPlayerId = null;
        role.AssignNewTarget();
    }
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class BountyHunterHudUpdatePatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (PlayerControl.LocalPlayer == null) return;
        if (PlayerControl.LocalPlayer.Data?.Role is not BountyHunterRole role) return;

        var player = PlayerControl.LocalPlayer;

        if (player.Data == null || player.Data.IsDead)
        {
            role.ClearArrowModifiers();
            return;
        }

        if (!ShipStatus.Instance) return;
        if (role.HasWon) return;
        if (MeetingHud.Instance || ExileController.Instance) return;

        if (!role.IntroFinished)
        {
            if (Object.FindObjectOfType<IntroCutscene>() != null)
                return;

            role.IntroFinished = true;
            role.IntroFinishTime = Mathf.Max(Time.time, 0.001f);
            return;
        }

        if (!role.Hunting)
        {
            if (role.IntroFinishTime <= 0f)
                return;

            if (Time.time - role.IntroFinishTime < 10f)
                return;

            role.Hunting = true;
            role.AssignNewTarget();
            return;
        }

        if (!role.HasWon &&
            (role.CurrentTarget == null ||
             role.CurrentTarget.Data == null ||
             role.CurrentTarget.Data.IsDead ||
             role.CurrentTarget.Data.Disconnected))
        {
            role.AssignNewTarget();
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















