using AmongUs.GameOptions;
using HarmonyLib;
using MiraAPI.Utilities;
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
    DraftSystem.LastNeutralKillingIds.Clear();
    foreach (var (playerId, roleId) in DraftSystem.DraftPicks)
    {
        var player = MiscUtils.PlayerById(playerId);
        if (player == null || player.Data == null || player.Data.Disconnected)
        {
            // Info($"[Draft] Skipping role assignment for player {playerId} (disconnected/null).");
            continue;
        }

        // Record for NK streak reduction
        if (DraftSystem.PlayerFactions.TryGetValue(playerId, out var faction) && faction == DraftFaction.NeutralKilling)
        {
            DraftSystem.LastNeutralKillingIds.Add(playerId);
        }
        
        player.RpcSetRole((RoleTypes)roleId);
    }


    // Assign Crewmate to any player not in draft picks (e.g. spectators)
    // This ensures they get a valid base role so TownOfUs can convert them to SpectatorRole
    foreach (var player in PlayerControl.AllPlayerControls)
    {
        if (player == null || player.Data == null || player.Data.Disconnected)
            continue;

        if (!DraftSystem.DraftPicks.ContainsKey(player.PlayerId))
        {
            player.RpcSetRole(RoleTypes.Crewmate);
        }
    }

    DraftSystem.DraftComplete = false;
    DraftSystem.DraftActiveThisRound = false;
}
}