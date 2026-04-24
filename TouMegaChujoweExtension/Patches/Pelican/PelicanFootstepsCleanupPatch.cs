using HarmonyLib;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Modifiers.Crewmate;

namespace TouMegaChujoweExtension.Patches.Pelican;

[HarmonyPatch(typeof(FootstepsModifier), nameof(FootstepsModifier.FixedUpdate))]
public static class PelicanFootstepsCleanupPatch
{
    [HarmonyPrefix]
    public static bool Prefix(FootstepsModifier __instance)
    {
        if (__instance == null || __instance.Player == null || __instance.Player.Data == null)
            return true;

        if (PelicanSystem.IsSwallowed(__instance.Player.PlayerId))
            return false;

        return true;
    }
}