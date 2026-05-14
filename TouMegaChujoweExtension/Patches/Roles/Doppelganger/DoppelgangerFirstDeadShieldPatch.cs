using HarmonyLib;
using MiraAPI.Modifiers;
using TownOfUs.Modifiers;
using TouMegaChujoweExtension.Modifiers.Neutral;

namespace TouMegaChujoweExtension.Patches.Roles.Doppelganger;

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

        // Ukrywamy tarczę, jeśli Doppelganger jest w przebraniu (ma DoppelgangerDisguiseModifier)
        if (__instance.Player.HasModifier<DoppelgangerDisguiseModifier>() && __instance.FirstRoundShield.activeSelf)
        {
            __instance.FirstRoundShield.SetActive(false);
        }
    }
}
