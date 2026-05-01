using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Options;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Bodyguard;

[HarmonyPatch]
public static class BodyguardNamePatch
{

    [HarmonyPatch(typeof(PlayerRoleTextExtensions), nameof(PlayerRoleTextExtensions.UpdateTargetColor), typeof(Color), typeof(PlayerControl), typeof(bool))]
    [HarmonyPostfix]
    public static void UpdateTargetColorPostfix(ref Color __result, PlayerControl player, bool hidden)
    {
        var local = PlayerControl.LocalPlayer;
        if (player == null || local == null || local.Data == null)
            return;

        // === Green name on who attacked (After backlash) ===
        if (local.Data.Role is BodyguardRole bgRole
            && bgRole.LastAttacker != null
            && bgRole.LastAttacker.PlayerId == player.PlayerId
            && (bgRole.BacklashReady || bgRole.KillModeActive) 
            && OptionGroupSingleton<BodyguardOptions>.Instance.GreenNameOnAttacker)
        {
            __result = new Color32(0, 100, 255, 255); // Blue color matching Σ
        }
    }
}
