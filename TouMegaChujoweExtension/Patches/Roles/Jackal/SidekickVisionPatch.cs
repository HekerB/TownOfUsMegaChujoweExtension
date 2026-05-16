using HarmonyLib;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Neutral;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Jackal;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
public static class SidekickVisionPatch
{
    [HarmonyPostfix]
    public static void Postfix(ShipStatus __instance, NetworkedPlayerInfo player, ref float __result)
    {
        if (player == null || player.IsDead || player.Object == null) return;

        if (player.Object.TryGetModifier<SidekickModifier>(out _))
        {
            // Give sidekicks Impostor vision
            __result = __instance.MaxLightRadius * GameOptionsManager.Instance.currentNormalGameOptions.ImpostorLightMod;
        }
    }
}
