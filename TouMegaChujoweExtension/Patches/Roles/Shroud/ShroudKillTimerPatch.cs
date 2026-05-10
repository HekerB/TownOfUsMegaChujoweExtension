using HarmonyLib;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Shroud;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class ShroudKillTimerPatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerControl __instance)
    {
        if (__instance != PlayerControl.LocalPlayer) return;
        if (__instance.Data?.Role is not ShroudRole) return;
        
        // Shroud is a Neutral Killing role, but the vanilla system doesn't decrement killTimer for neutrals.
        // We manually decrement it here so that the custom buttons (which sync with killTimer) can function.
        if (__instance.killTimer > 0)
        {
            __instance.killTimer -= Time.fixedDeltaTime;
        }
    }
}
