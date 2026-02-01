using HarmonyLib;
using MiraAPI.Modifiers;
using TouMiraRolesExtension.Modifiers.Universal;
using TouMiraRolesExtension.Options.Modifiers;
using TownOfUs.Utilities;

namespace TouMiraRolesExtension.Patches.Spiteful;

[HarmonyPatch(typeof(Extensions), nameof(Extensions.GetKillCooldown))]
public static class SpitefulCooldownPatch
{
    public static void Postfix(PlayerControl player, ref float __result)
    {
        if (player == null || player.HasDied())
        {
            return;
        }

        if (player.HasModifier<SpitefulEffectModifier>())
        {
            var mod = player.GetModifier<SpitefulEffectModifier>();
            if (mod != null && mod.EffectType == SpitefulEffectType.IncreasedCooldowns)
            {
                __result *= (1f + mod.ImpactMultiplier);
            }
        }
    }
}
