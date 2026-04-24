using HarmonyLib;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Universal;

namespace TouMegaChujoweExtension.Patches.Drunk;

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
public static class DrunkMovementPatch
{
    public static void Postfix(PlayerPhysics __instance)
    {
        if (!__instance.AmOwner)
            return;

        if (__instance.myPlayer == null || __instance.myPlayer.Data == null || __instance.myPlayer.Data.IsDead)
            return;

        if (!__instance.myPlayer.HasModifier<DrunkModifier>())
            return;

        if (!__instance.myPlayer.CanMove)
            return;

        if (__instance.body != null)
        {
            __instance.body.velocity *= -1;
        }
    }
}