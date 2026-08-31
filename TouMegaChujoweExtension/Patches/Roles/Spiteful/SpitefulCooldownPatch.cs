using HarmonyLib;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using TownOfUs.Utilities;
using UnityEngine;

namespace Spiteful
{
    /// <summary>
    /// Universal cooldown scaling for all Mira/ToU buttons.
    /// Ensures multiplier applies at round start and on timer changes.
    /// </summary>
    /// <remarks>
    /// One kind of patch per method. All three of these carried [HarmonyPrefix] AND [HarmonyPostfix],
    /// which registers the same body twice and runs it on both sides of every call - so the button
    /// built for a Spiteful player could have its cooldown multiplied once before CreateButton and
    /// again after it, and FixedUpdateHandler did two passes a frame instead of one. Which side each
    /// belongs on is not a matter of taste either: the timer setter has to be a prefix because it
    /// rewrites the value being assigned, and the button one has to be a postfix because there is no
    /// button to scale until the original has made it.
    /// </remarks>
    [HarmonyPatch]
    public static class SpitefulCooldownPatch
    {
        [HarmonyPatch(typeof(CustomActionButton), "set_Timer")]
        [HarmonyPrefix]
        private static void TimerSetterPrefix(CustomActionButton __instance, ref float value)
        {
            // Only scale upward assignments
            if (value <= __instance.Timer) return;

            var player = PlayerControl.LocalPlayer;
            if (player == null || player.HasDied() || !player.AmOwner) return;

            var mod = player.GetModifier<SpitefulEffectModifier>();
            if (mod?.EffectType != SpitefulEffectType.IncreasedCooldowns) return;

            float multiplier = mod.CooldownMultiplier;

            // Avoid double scaling
            if (__instance.Timer > 0f && value <= __instance.Timer * multiplier * 1.01f) return;

            value *= multiplier;
        }

        [HarmonyPatch(typeof(CustomActionButton), nameof(CustomActionButton.CreateButton))]
        [HarmonyPostfix]
        private static void CreateButtonPostfix(CustomActionButton __instance)
        {
            var player = PlayerControl.LocalPlayer;
            if (player == null || player.HasDied() || !player.AmOwner) return;

            var mod = player.GetModifier<SpitefulEffectModifier>();
            if (mod?.EffectType != SpitefulEffectType.IncreasedCooldowns) return;

            float multiplier = mod.CooldownMultiplier;

            // Only scale if timer is positive and not already scaled
            if (__instance.Timer > 0f && __instance.Timer <= (__instance.InitialCooldown * multiplier) * 1.01f)
                __instance.Timer *= multiplier;
        }

        [HarmonyPatch(typeof(CustomActionButton), nameof(CustomActionButton.FixedUpdateHandler))]
        [HarmonyPrefix]
        private static void FixedUpdateHandlerPrefix(CustomActionButton __instance)
        {
            if (__instance.Button == null) return;
            if (__instance.Timer <= 0f) return;

            var player = PlayerControl.LocalPlayer;
            if (player == null || player.HasDied() || !player.AmOwner) return;

            var mod = player.GetModifier<SpitefulEffectModifier>();
            if (mod?.EffectType != SpitefulEffectType.IncreasedCooldowns) return;

            float multiplier = mod.CooldownMultiplier;
            float baseCooldown = __instance.Cooldown;
            float scaledCooldown = baseCooldown * multiplier;

            if (baseCooldown > 0f && 
                __instance.Timer >= (baseCooldown * 0.99f) && 
                __instance.Timer <= (baseCooldown * 1.01f) &&
                __instance.Timer < (scaledCooldown * 0.99f))
            {
                __instance.Timer = scaledCooldown;
            }
            else if (__instance.Timer > 0f && __instance.Timer < (scaledCooldown * 0.99f))
            {
                __instance.Timer *= multiplier;
            }
        }
    }
}











