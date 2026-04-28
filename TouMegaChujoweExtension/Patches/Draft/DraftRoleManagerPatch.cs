using AmongUs.GameOptions;
using HarmonyLib;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Patches;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Draft;

[HarmonyPatch]
public static class DraftRoleManagerPatch
{
    [HarmonyPatch(typeof(TouRoleManagerPatches), "AssignRoles")]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool BlockNormalRandomRoles()
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (!DraftSystem.DraftComplete) return true;

        ApplyDraftRoles();
        return false; 
    }   
    [HarmonyPatch(typeof(TouRoleManagerPatches), "AssignRolesFromRoleList")]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool BlockRoleListRandomRoles()
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (!DraftSystem.DraftComplete) return true;

        ApplyDraftRoles();
        return false;
    }

private static void ApplyDraftRoles()
{
    foreach (var (playerId, roleId) in DraftSystem.DraftPicks)
    {
        var player = MiscUtils.PlayerById(playerId);
        if (player == null || player.Data == null || player.Data.Disconnected)
        {
            // Info($"[Draft] Skipping role assignment for player {playerId} (disconnected/null).");
            continue;
        }
        
        player.RpcSetRole((RoleTypes)roleId);
    }

    DraftSystem.DraftComplete = false;
    DraftSystem.DraftActiveThisRound = false;
}
}
