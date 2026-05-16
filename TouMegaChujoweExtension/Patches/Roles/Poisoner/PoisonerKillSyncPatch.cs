using HarmonyLib;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Poisoner;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetKillTimer))]
public static class PoisonerKillTimerSyncPatch
{
    private static float _lastKillTimer;

    [HarmonyPostfix]
    public static void Postfix(PlayerControl __instance, float time)
    {
        if (__instance != PlayerControl.LocalPlayer) return;
        if (!__instance.IsRole<PoisonerRole>()) return;

        if (time > 5f && time > _lastKillTimer + 1f)
        {
            PoisonerPoisonButton.SetOwnCooldown();
            PoisonerVineButton.SetOwnCooldown();
        }

        _lastKillTimer = time;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class PoisonerForceKillTimerPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(PlayerControl __instance)
    {
        if (__instance != PlayerControl.LocalPlayer) return;
        if (!__instance.IsRole<PoisonerRole>()) return;
        if (PoisonSystem.IsRemoteKill) return;
        if (!PoisonSystem.HasActivePoison && !PoisonSystem.IsVineActive) return;

        __instance.killTimer = Mathf.Max(__instance.killTimer, 10f);
    }
}


[HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
public static class PoisonerBlockKillClickPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool Prefix()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !local.IsRole<PoisonerRole>()) return true;
        if (PoisonSystem.IsRemoteKill) return true;
        if (PoisonSystem.HasActivePoison || PoisonSystem.IsVineActive) return false;
        return true;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
public static class PoisonerCheckMurderBlockPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool Prefix(PlayerControl __instance)
    {
        if (__instance != PlayerControl.LocalPlayer) return true;
        if (!__instance.IsRole<PoisonerRole>()) return true;
        if (PoisonSystem.IsRemoteKill) return true;
        if (PoisonSystem.HasActivePoison || PoisonSystem.IsVineActive) return false;
        return true;
    }
}
















