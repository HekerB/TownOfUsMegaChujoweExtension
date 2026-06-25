using AmongUs.GameOptions;
using HarmonyLib;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using TownOfUs.Patches;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Patches.Roles.Jackal;
using TouMegaChujoweExtension.Roles.Classic.Impostor;

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
        System.Console.WriteLine(DraftSystem.DraftComplete);
        if (!AmongUsClient.Instance.AmHost) return true;
        if (!DraftSystem.DraftComplete) return true;
        System.Console.WriteLine("HERE");
        ApplyDraftRoles();
        return false;
    }

    private static void ApplyDraftRoles()
    {
        DraftSystem.LastNeutralKillingIds.Clear();
        DraftSystem.LastImpostorIds.Clear();
        var effectivePicks = BuildEffectiveDraftPicks();
        DraftSystem.SetPreviousGameRoles(effectivePicks.Values);

        foreach (var (playerId, roleId) in effectivePicks)
        {
            var player = MiscUtils.PlayerById(playerId);
            if (player == null || player.Data == null || player.Data.Disconnected)
            {

                continue;
            }


            if (DraftSystem.PlayerFactions.TryGetValue(playerId, out var faction) && faction == DraftFaction.NeutralKilling)
            {
                DraftSystem.LastNeutralKillingIds.Add(playerId);
            }
            else if (IsImpostorRole(roleId))
            {
                DraftSystem.LastImpostorIds.Add(playerId);
            }

            player.RpcSetRole((RoleTypes)roleId);
        }

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null || player.Data.Disconnected)
                continue;

            if (!effectivePicks.ContainsKey(player.PlayerId))
            {
                player.RpcSetRole(RoleTypes.Crewmate);
            }
        }
        JackalStartPatch.ExecuteAssignment();
        DraftSystem.DraftComplete = false;
        DraftSystem.DraftActiveThisRound = false;
    }

    private static Dictionary<byte, ushort> BuildEffectiveDraftPicks()
    {
        var effectivePicks = DraftSystem.DraftPicks.ToDictionary(static pick => pick.Key, static pick => pick.Value);
        var lonerRoleId = RoleId.Get<LonerRole>();

        if (!effectivePicks.ContainsValue(lonerRoleId))
        {
            return effectivePicks;
        }

        var allowedImpostorSlots = GetAllowedLonerImpostorSlots();
        var keptImpostorSlots = 1;

        foreach (var (playerId, roleId) in DraftSystem.DraftPicks)
        {
            if (roleId == lonerRoleId || !IsImpostorRole(roleId))
            {
                continue;
            }

            if (keptImpostorSlots < allowedImpostorSlots)
            {
                keptImpostorSlots++;
                continue;
            }

            effectivePicks[playerId] = (ushort)RoleTypes.Crewmate;
            DraftSystem.PlayerFactions[playerId] = DraftFaction.CrewOther;
            DraftSystem.ImpostorPlayerIds.Remove(playerId);
        }

        DraftSystem.LonerReducedImpostorSlot = true;
        return effectivePicks;
    }

    private static int GetAllowedLonerImpostorSlots()
    {
        try
        {
            return Math.Max(1, GameOptionsManager.Instance.currentNormalGameOptions.NumImpostors - 1);
        }
        catch
        {
            return 1;
        }
    }

    private static bool IsImpostorRole(ushort roleId)
    {
        if ((RoleTypes)roleId == RoleTypes.Impostor)
        {
            return true;
        }

        try
        {
            return RoleManager.Instance.GetRole((RoleTypes)roleId)?.IsImpostor() == true;
        }
        catch
        {
            return false;
        }
    }
}
