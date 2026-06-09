using HarmonyLib;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Crewmate;
using TouMegaChujoweExtension.Modifiers.Impostor;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Injector;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
public static class InjectorVisionPatch
{
    public static void Postfix(ShipStatus __instance, NetworkedPlayerInfo player, ref float __result)
    {
        if (player == null || player.IsDead || player.Object == null)
        {
            return;
        }

        var visionFactor = 1f;
        var hasVisionModifier = false;

        // Check for vision reduction modifiers (most severe takes priority)
        if (player.Object.HasModifier<VoodooBlindModifier>())
        {
            var mod = player.Object.GetModifier<VoodooBlindModifier>();
            if (mod != null)
            {
                visionFactor = mod.VisionPerc;
                hasVisionModifier = true;
            }
        }
        else if (player.Object.HasModifier<InjectedVeryLowVisionModifier>())
        {
            var mod = player.Object.GetModifier<InjectedVeryLowVisionModifier>();
            if (mod != null)
            {
                visionFactor = mod.VisionPerc;
                hasVisionModifier = true;
            }
        }
        else if (player.Object.HasModifier<InjectedLowVisionModifier>())
        {
            var mod = player.Object.GetModifier<InjectedLowVisionModifier>();
            if (mod != null)
            {
                visionFactor = mod.VisionPerc;
                hasVisionModifier = true;
            }
        }
        // Check for nausea (both speed and vision reduction)
        else if (player.Object.HasModifier<InjectedNauseaModifier>())
        {
            var mod = player.Object.GetModifier<InjectedNauseaModifier>();
            if (mod != null)
            {
                visionFactor = mod.VisionPerc;
                hasVisionModifier = true;
            }
        }

        // Check for vision boost modifier (applies multiplicatively if no reduction)
        if (!hasVisionModifier)
        {
            if (player.Object.HasModifier<InjectedVisionBoostModifier>())
            {
                var mod = player.Object.GetModifier<InjectedVisionBoostModifier>();
                if (mod != null)
                {
                    visionFactor = mod.VisionPerc;
                }
            }
            else if (player.Object.HasModifier<DoctorVisionBoostModifier>())
            {
                var mod = player.Object.GetModifier<DoctorVisionBoostModifier>();
                if (mod != null)
                {
                    visionFactor = mod.VisionPerc;
                }
            }
        }

        if (!Mathf.Approximately(visionFactor, 1f))
        {
            __result *= visionFactor;
        }
    }
}