using HarmonyLib;
using System;
using TownOfUs.Modifiers;
using TownOfUs.Patches;
using MiraAPI.Modifiers;

namespace TouMegaChujoweExtension.Patches.BugFixes;

[HarmonyPatch(typeof(TouRoleManagerPatches), "AssignRoles")]
[HarmonyPatch(typeof(TouRoleManagerPatches), "AssignRolesFromRoleList")]
public static class DeathHandlerInitializationPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        UnityEngine.Debug.Log("[TOUMCE] AssignRoles Postfix triggered - ensuring all players have DeathHandlerModifier.");
        EnsureModifierOnAllPlayers();
    }

    public static void EnsureModifierOnAllPlayers()
    {
        if (PlayerControl.AllPlayerControls == null) return;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.Pointer != IntPtr.Zero)
            {
                if (!player.HasModifier<DeathHandlerModifier>())
                {
                    try
                    {
                        player.AddModifier<DeathHandlerModifier>();
                        UnityEngine.Debug.Log($"[TOUMCE] Initialized DeathHandlerModifier for player {player.Data?.PlayerName} (ID: {player.PlayerId})");
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[TOUMCE] Failed to add DeathHandlerModifier to player {player.PlayerId}: {ex}");
                    }
                }
            }
        }
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
public static class DeathHandlerShipStatusStartPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        UnityEngine.Debug.Log("[TOUMCE] ShipStatus.Start Postfix triggered - ensuring all players have DeathHandlerModifier.");
        DeathHandlerInitializationPatch.EnsureModifierOnAllPlayers();
    }
}
