using HarmonyLib;
using MiraAPI.Hud;
using TownOfUs.Utilities;
using TownOfUs.Extensions;

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
            {
                var button = CustomButtonSingleton<KamikazeSuicideButton>.Instance;
                if (button != null)
                {
                    button.Timer = button.Cooldown;
                }
            }
        }

        _lastKillTimer = time;
    }
}



[HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
public static class RcXdBlockKillClickPatch
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !local.IsRole<RcXdRole>()) return true;
        var role = local.GetRole<RcXdRole>();
        if (role?.ActiveCar != null && role.ActiveCar.IsDriving) return false;
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
        if (!__instance.IsRole<RcXdRole>()) return true;
        var role = __instance.GetRole<RcXdRole>();
        if (role?.ActiveCar != null && role.ActiveCar.IsDriving) return false;
        return true;
    }
}