using HarmonyLib;
using System.Linq;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Game;
using TouMegaChujoweExtension.Modifiers.Impostor;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Drunk;

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
public static class DrunkMovementPatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerPhysics __instance)
    {
        if (__instance == null || __instance.myPlayer == null || !__instance.myPlayer.AmOwner)
            return;

        if (__instance.myPlayer.Data == null || __instance.myPlayer.Data.IsDead)
            return;

        if (!__instance.myPlayer.CanMove)
            return;

        var roleblocked = __instance.myPlayer.GetModifiers<TouMegaChujoweExtension.Modifiers.Crewmate.RoleblockedModifier>().FirstOrDefault();
        var hasInvert = __instance.myPlayer.HasModifier<InverterDisorientedModifier>() ||
                        __instance.myPlayer.HasModifier<DrunkModifier>() ||
                        __instance.myPlayer.HasModifier<InjectedInvertedControlsModifier>() ||
                        (roleblocked != null && roleblocked.InvertControls);

        if (hasInvert && __instance.body != null)
        {
            __instance.body.velocity *= -1f;
        }
    }
}
