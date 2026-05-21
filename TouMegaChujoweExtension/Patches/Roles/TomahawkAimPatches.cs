using HarmonyLib;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using UnityEngine;
using MiraAPI.GameOptions;

namespace TouMegaChujoweExtension.Patches.Roles;

[HarmonyPatch]
public static class TomahawkAimPatches
{
    [HarmonyPatch(typeof(LogicOptions), nameof(LogicOptions.GetPlayerSpeedMod))]
    [HarmonyPostfix]
    public static void GetPlayerSpeedModPostfix(PlayerControl pc, ref float __result)
    {
        if (pc == null || pc != PlayerControl.LocalPlayer) return;

        var role = pc.GetRole<TomahawkRole>();
        if (role != null && role.IsAiming)
        {
            __result = 0f; // Freeze movement when aiming
        }
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPostfix]
    public static void CalculateLightRadiusPostfix(NetworkedPlayerInfo player, ref float __result)
    {
        if (player == null || player.Object == null || !player.Object.AmOwner) return;
        
        var role = player.Object.GetRole<TomahawkRole>();
        if (role != null && role.IsAiming)
        {
            __result = 25f; // See through walls
        }
    }
}
