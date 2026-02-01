using HarmonyLib;
using MiraAPI.Modifiers;
using TouMiraRolesExtension.Modifiers.Universal;
using TouMiraRolesExtension.Options.Modifiers;

namespace TouMiraRolesExtension.Patches.Spiteful;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
public static class SpitefulVisionPatch
{
    public static void Postfix(ShipStatus __instance, NetworkedPlayerInfo player, ref float __result)
    {
        if (player == null || player.IsDead || player.Object == null)
        {
            return;
        }

        if (player.Object.HasModifier<SpitefulEffectModifier>())
        {
            var mod = player.Object.GetModifier<SpitefulEffectModifier>();
            if (mod != null && mod.EffectType == SpitefulEffectType.LowerVision)
            {
                __result *= 0.75f;
            }
        }
    }
}