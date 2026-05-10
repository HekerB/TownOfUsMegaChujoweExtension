using HarmonyLib;
using MiraAPI.Hud;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles.RcXd;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetKillTimer))]
public static class RcXdKillTimerSyncPatch
{
    private static float _lastKillTimer;

    [HarmonyPostfix]
    public static void Postfix(PlayerControl __instance, float time)
    {
        if (__instance != PlayerControl.LocalPlayer) return;
        if (time > 5f && time > _lastKillTimer + 1f)
        {
            if (__instance.IsRole<RcXdRole>())
                RcXdDeployButton.SetOwnCooldown();
            else if (__instance.IsRole<KamikazeRole>())
                CustomButtonSingleton<KamikazeSuicideButton>.Instance.Timer = CustomButtonSingleton<KamikazeSuicideButton>.Instance.Cooldown;
        }

        _lastKillTimer = time;
    }
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class RcXdKillButtonBlockPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Low)]
    public static void Postfix(HudManager __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null || local.Data.IsDead) return;
        if (local.Data.Role is not RcXdRole role) return;
        if (role.ActiveCar == null || !role.ActiveCar.IsDriving) return;

        var killButton = __instance.KillButton;
        if (killButton == null) return;

        killButton.SetDisabled();
        killButton.graphic.color = Palette.DisabledClear;
        killButton.graphic.material.SetFloat("_Desat", 1f);
    }
}


[HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
public static class RcXdBlockKillClickPatch
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data?.Role is not RcXdRole role) return true;
        if (role.ActiveCar != null && role.ActiveCar.IsDriving) return false;
        return true;
    }
}


[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
public static class RcXdCheckMurderBlockPatch
{
    [HarmonyPrefix]
    public static bool Prefix(PlayerControl __instance)
    {
        if (__instance != PlayerControl.LocalPlayer) return true;
        if (__instance.Data?.Role is not RcXdRole role) return true;
        if (role.ActiveCar != null && role.ActiveCar.IsDriving) return false;
        return true;
    }
}















