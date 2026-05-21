using HarmonyLib;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Extensions;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles;

[HarmonyPatch]
public static class PossessorPatches
{
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap))]
    [HarmonyPrefix]
    public static bool ShowSabotageMapPrefix()
    {
        var local = PlayerControl.LocalPlayer;
        if (local != null && local.IsRole<PossessorRole>()) return false;
        return true;
    }

    [HarmonyPatch(typeof(ReportButton), nameof(ReportButton.DoClick))]
    [HarmonyPrefix]
    public static bool ReportButtonDoClickPrefix()
    {
        var local = PlayerControl.LocalPlayer;
        if (local != null && local.IsRole<PossessorRole>()) return false;
        return true;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdReportDeadBody))]
    [HarmonyPrefix]
    public static bool CmdReportBodyPrefix(PlayerControl __instance)
    {
        if (__instance != null && __instance.IsRole<PossessorRole>()) return false;
        return true;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    [HarmonyPrefix]
    public static bool ReportDeadBodyPrefix(PlayerControl __instance)
    {
        if (__instance != null && __instance.IsRole<PossessorRole>()) return false;
        return true;
    }
}
