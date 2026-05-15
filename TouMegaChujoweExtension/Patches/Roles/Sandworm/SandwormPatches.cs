using HarmonyLib;
using TouMegaChujoweExtension.Roles.Impostor;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Modules;
using MiraAPI.GameOptions;
using AmongUs.GameOptions;
using TownOfUs;
using TownOfUs.Extensions;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Impostor;

[HarmonyPatch]
public static class SandwormPatches
{
    [HarmonyPatch(typeof(LogicOptions), nameof(LogicOptions.GetPlayerSpeedMod))]
    [HarmonyPostfix]
    public static void GetPlayerSpeedModPostfix(PlayerControl pc, ref float __result)
    {
        if (pc != null && pc.Data.Role is SandwormRole role && role.IsUnderground)
        {
            __result *= OptionGroupSingleton<SandwormOptions>.Instance.UndergroundSpeed;
        }
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.OnEnable))]
    [HarmonyPrefix]
    public static void ShipStatusOnEnablePrefix()
    {
        SandwormSystem.Reset();
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPrefix]
    public static void MeetingHudStartPrefix()
    {
        // Ensure all sandworms are visible again and digging is cancelled before resetting system
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc != null && pc.Data?.Role is SandwormRole role)
            {
                role.IsUnderground = false;
                pc.Visible = true;
            }
        }

        SandwormSystem.Reset();
    }
}
