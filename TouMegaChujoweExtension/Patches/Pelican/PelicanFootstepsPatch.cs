using HarmonyLib;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Modifiers.Crewmate;

namespace TouMegaChujoweExtension.Patches.Pelican;

[HarmonyPatch(typeof(FootstepsModifier), nameof(FootstepsModifier.OnActivate))]
public static class PelicanFootstepsPatch
{
    [HarmonyPrefix]
    public static bool Prefix(FootstepsModifier __instance)
    {
        try
        {
            if (__instance.Player != null && PelicanSystem.IsSwallowed(__instance.Player.PlayerId))
                return false;
        }
        catch { }

        return true;
    }
}
