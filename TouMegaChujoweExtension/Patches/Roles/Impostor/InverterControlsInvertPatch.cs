using HarmonyLib;
using UnityEngine;
using TouMegaChujoweExtension.Modifiers.Impostor;
using MiraAPI.Modifiers;
using System.Linq;
using TouMegaChujoweExtension.Modifiers.Game;

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

        var roleblocked = __instance.myPlayer.GetModifiers<TouMegaChujoweExtension.Modifiers.Crewmate.RoleblockedModifier>().FirstOrDefault();
        if (__instance.myPlayer.HasModifier<InverterDisorientedModifier>() ||
            __instance.myPlayer.HasModifier<DrunkModifier>() ||
            __instance.myPlayer.HasModifier<InjectedInvertedControlsModifier>() ||
            (roleblocked != null && roleblocked.InvertControls))
        {
            direction = -direction;
        }
    }
}
