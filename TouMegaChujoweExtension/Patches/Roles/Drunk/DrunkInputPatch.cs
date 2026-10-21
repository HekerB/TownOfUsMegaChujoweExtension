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
                        __instance.myPlayer.HasModifier<WastedModifier>() ||
                        __instance.myPlayer.HasModifier<InjectedInvertedControlsModifier>() ||
                        (roleblocked != null && roleblocked.InvertControls);

        // TOU-Mira 1.7.3's DrunkPhysicsPatch flips the same velocity for its own Drunk and Barkeeper roleblock;
        // flipping again here would hand that player their normal controls back.
        if (hasInvert && __instance.body != null && !TouInvertsControls(__instance.myPlayer))
        {
            __instance.body.velocity *= -1f;
        }
    }

    private static bool TouInvertsControls(PlayerControl player)
    {
        return player.HasModifier<TownOfUs.Modifiers.Game.Universal.DrunkModifier>() ||
               player.GetModifiers<TownOfUs.Modifiers.Other.RoleblockedModifier>().Any(x => x.InvertControls);
    }
}
