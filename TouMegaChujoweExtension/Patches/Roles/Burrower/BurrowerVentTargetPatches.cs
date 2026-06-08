using HarmonyLib;
using MiraAPI.Utilities;
using TownOfUs.Buttons.Crewmate;
using TownOfUs.Roles.Crewmate;

namespace TouMegaChujoweExtension.Patches.Roles.Burrower;

[HarmonyPatch]
public static class BurrowerVentTargetPatches
{
    [HarmonyPatch(typeof(PlumberBlockButton), nameof(PlumberBlockButton.GetTarget))]
    [HarmonyPostfix]
    public static void PlumberBlockButtonGetTargetPostfix(PlumberBlockButton __instance, ref Vent? __result)
    {
        if (__result != null && !BurrowerSystem.IsBurrowerVent(__result))
        {
            return;
        }

        __result = BurrowerSystem.GetClosestUsableMapVent(
            PlayerControl.LocalPlayer,
            __instance.Distance,
            candidate => !VentOccupancySystem.IsBlocked(candidate.Id));
    }

    [HarmonyPatch(typeof(PlumberBlockButton), nameof(PlumberBlockButton.IsTargetValid))]
    [HarmonyPostfix]
    public static void PlumberBlockButtonIsTargetValidPostfix(Vent? target, ref bool __result)
    {
        if (target != null && BurrowerSystem.IsBurrowerVent(target))
        {
            __result = false;
        }
    }

    [HarmonyPatch(typeof(PlumberRole), nameof(PlumberRole.RpcPlumberBlockVent))]
    [HarmonyPrefix]
    public static bool RpcPlumberBlockVentPrefix(PlayerControl player, int ventId)
    {
        var vent = Helpers.GetVentById(ventId);
        return !BurrowerSystem.IsBurrowerVent(vent);
    }
}
