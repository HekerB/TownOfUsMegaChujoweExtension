using HarmonyLib;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Extensions;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Sentinel;

[HarmonyPatch]
public static class SentinelPatches
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    [HarmonyPostfix]
    public static void PostfixMurderPlayer(PlayerControl __instance, PlayerControl target)
    {
        if (__instance == null || target == null) return;
        
        SentinelSystem.HandleKill(__instance, target);
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    [HarmonyPostfix]
    public static void PostfixFixedUpdate(PlayerControl __instance)
    {
        if (__instance == null || !__instance.AmOwner) return;
        
        SentinelSystem.Update();
    }

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
    [HarmonyPostfix]
    public static void PostfixExileBegin()
    {
        SentinelSystem.Reset();
    }
}
