using HarmonyLib;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers;
using TownOfUs.Modifiers;

namespace TouMegaChujoweExtension.Patches.Doppelganger;

[HarmonyPatch(typeof(FirstDeadShield), nameof(FirstDeadShield.Update))]
public static class DoppelgangerFirstDeadShieldPatch
{
    [HarmonyPostfix]
    public static void Postfix(FirstDeadShield __instance)
    {
        if (MeetingHud.Instance || __instance.FirstRoundShield == null)
        {
            return;
        }

        // If the player has DoppelgangerDisguiseModifier, match the target's shield state
        if (__instance.Player.TryGetModifier<DoppelgangerDisguiseModifier>(out var disguise) && disguise.Target != null)
        {
            var showAsTarget = disguise.Target.HasModifier<FirstDeadShield>();
            if (!showAsTarget && __instance.FirstRoundShield.activeSelf)
            {
                __instance.FirstRoundShield.SetActive(false);
            }
        }
    }
}
