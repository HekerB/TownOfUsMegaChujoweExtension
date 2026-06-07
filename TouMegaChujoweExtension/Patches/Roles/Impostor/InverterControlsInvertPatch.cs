using HarmonyLib;
using UnityEngine;
using TouMegaChujoweExtension.Modifiers.Impostor;
using MiraAPI.Modifiers;

namespace TouMegaChujoweExtension.Patches.Roles.Impostor;

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.SetNormalizedVelocity))]
public static class InverterControlsInvertPatch
{
    [HarmonyPrefix]
    public static void Prefix(PlayerPhysics __instance, ref Vector2 direction)
    {
        if (__instance == null || __instance.myPlayer == null || !__instance.myPlayer.AmOwner)
        {
            return;
        }

        if (__instance.myPlayer.HasModifier<InverterDisorientedModifier>() || __instance.myPlayer.HasModifier<InjectedInvertedControlsModifier>())
        {
            direction = -direction;
        }
    }
}
