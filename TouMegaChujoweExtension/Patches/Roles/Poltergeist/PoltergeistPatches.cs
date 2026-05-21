using HarmonyLib;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Extensions;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles.Poltergeist;

[HarmonyPatch]
public static class PoltergeistPatches
{
    [HarmonyPatch(typeof(ReportButton), nameof(ReportButton.DoClick))]
    [HarmonyPrefix]
    public static bool ReportButtonDoClickPrefix()
    {
        var local = PlayerControl.LocalPlayer;
        if (local != null && local.IsRole<PoltergeistRole>()) return false;
        return true;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdReportDeadBody))]
    [HarmonyPrefix]
    public static bool CmdReportBodyPrefix(PlayerControl __instance)
    {
        if (__instance != null && __instance.IsRole<PoltergeistRole>()) return false;
        return true;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    [HarmonyPrefix]
    public static bool ReportDeadBodyPrefix(PlayerControl __instance)
    {
        if (__instance != null && __instance.IsRole<PoltergeistRole>()) return false;
        return true;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.OnClick))]
    [HarmonyPrefix]
    public static bool OnClickPrefix(PlayerControl __instance)
    {
        if (__instance == PlayerControl.LocalPlayer)
        {
            return false; // Cannot click yourself!
        }
        return true;
    }
}
