using HarmonyLib;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches;

[HarmonyPatch]
public static class HackerJamPatches
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.AreCommsAffected))]
    [HarmonyPrefix]
    [HarmonyPostfix]
    public static void PlayerControlAreCommsAffectedPostfix(ref bool __result)
    {
        if (__result)
        {
            return;
        }

        if (HackerSystem.IsJammed)
        {
            __result = true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    [HarmonyPrefix]
    public static bool ReportDeadBodyPrefix()
    {
        return !HackerSystem.IsJammed;
    }

    [HarmonyPatch(typeof(ReportButton), nameof(ReportButton.DoClick))]
    [HarmonyPrefix]
    public static bool ReportButtonDoClickPrefix()
    {
        return !HackerSystem.IsJammed;
    }

    [HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Update))]
    [HarmonyPostfix]
    public static void EmergencyMinigameUpdatePostfix(EmergencyMinigame __instance)
    {
        if (HackerSystem.IsJammed)
        {
            __instance.StatusText.text = TouLocale.GetParsed("ExtensionRoleHackerJamActivated", "SYSTEM JAMMED");
            __instance.NumberText.text = string.Empty;
            __instance.ButtonActive = false;
            __instance.state = 3; // Blocked state
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudManagerUpdatePostfix(HudManager __instance)
    {
        if (__instance == null || __instance.ReportButton == null) return;

        if (HackerSystem.IsJammed)
        {
            var color = Palette.DisabledGrey;
            __instance.ReportButton.graphic.color = new Color(color.r, color.g, color.b, 0.5f);
        }
    }
}
