using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Options;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TouMegaChujoweExtension.Modules;

public enum DraftFaction
{
    Impostor,
    NeutralKilling,
    CrewOther
}

public static class DraftSystem
{
    // === STATE ===
    public static bool IsRunning { get; set; }
    public static bool DraftComplete { get; set; }
    public static List<byte> PickOrder { get; } = new();
    public static HashSet<ushort> AlreadyPicked { get; } = new();
    public static bool LocalPlayerPicked { get; set; }
    public static float PickTimer { get; set; }
    public static Dictionary<byte, ushort> DraftPicks { get; } = new();
    public static HashSet<byte> ImpostorPlayerIds { get; set; } = new();
    public static HashSet<byte> LastNeutralKillingIds { get; } = new();
    public static bool DraftActiveThisRound { get; set; }
    public static List<RoleBehaviour>? CurrentOfferedRoles { get; set; }
    public static RoleAlignment? SelectedAlignment { get; set; }

    // === FACTION ASSIGNMENTS ===
    public static Dictionary<byte, DraftFaction> PlayerFactions { get; } = new();

    // === DRAFT MODE CHECK ===
    public static bool IsEnabled => Patches.Draft.DraftGameModePatch.IsDraftMode();

    public static float TimeToChoose
    {
        get
        {
            try { return OptionGroupSingleton<DraftModeOptions>.Instance.TimeToChoose.Value; }
            catch { return 20f; }
        }
    }

    public static int RolesToShow
    {
        get
        {
            try { return (int)OptionGroupSingleton<DraftModeOptions>.Instance.RolesToShow.Value; }
            catch { return 3; }
        }
    }

    private static bool ShouldImpostorsPickFromAllClasses()
    {
        try
        {
            return OptionGroupSingleton<DraftModeOptions>.Instance.ImpostorsPickFromAllClasses.Value;
        }
        catch
        {
            return false;
        }
    }

    private static bool ShouldCrewmatesPickFromAllClasses()
    {
        try
        {
            return OptionGroupSingleton<DraftModeOptions>.Instance.CrewmatesPickFromAllClasses.Value;
        }
        catch
        {
            return false;
        }
    }

    // === FACTION ASSIGNMENT ===

    public static void AssignFactions(List<byte> allPlayerIds, HashSet<byte> impostorIds)
    {
        PlayerFactions.Clear();
        var options = OptionGroupSingleton<DraftModeOptions>.Instance;

        foreach (var id in impostorIds)
            PlayerFactions[id] = DraftFaction.Impostor;

        var remaining = allPlayerIds.Where(id => !impostorIds.Contains(id)).ToList();
        remaining.Shuffle();

        int GetSafeCount(float min, float max)
        {
            int minVal = Mathf.Min((int)min, (int)max);
            int maxVal = Mathf.Max((int)min, (int)max);
            return Random.Range(minVal, maxVal + 1);
        }

        int neutralKillingCount = GetSafeCount(options.MinNeutralKilling.Value, options.MaxNeutralKilling.Value);

        if (neutralKillingCount > remaining.Count)
            neutralKillingCount = remaining.Count;

        int idx = 0;

        // Apply Neutral Killing streak reduction
        var nkReductionEnabled = options.ReduceKillingStreak.Value;
        var nkBiasPercent = options.NKReductionChance.Value / 100f;
        var random = new System.Random();

        for (int i = 0; i < neutralKillingCount && remaining.Count > idx; i++)
        {
            int startIdx = idx;
            int num = -1;
            
            if (nkReductionEnabled && LastNeutralKillingIds.Count > 0)
            {
                // Try to find someone who wasn't NK last time
                var subPool = remaining.Skip(idx).ToList();
                var nonRecentNK = subPool.Where(id => !LastNeutralKillingIds.Contains(id)).ToList();
                
                if (nonRecentNK.Count > 0 && random.NextDouble() < nkBiasPercent)
                {
                    // Pick from non-recent NKs
                    byte chosenId = nonRecentNK[random.Next(nonRecentNK.Count)];
                    num = remaining.IndexOf(chosenId);
                }
            }

            if (num == -1)
            {
                // Normal random pick from remaining pool starting at idx
                num = random.Next(idx, remaining.Count);
            }

            // Swap chosen player to the current 'idx' position so they are assigned NK
            (remaining[idx], remaining[num]) = (remaining[num], remaining[idx]);
            PlayerFactions[remaining[idx]] = DraftFaction.NeutralKilling;
            idx++;
        }

        for (; idx < remaining.Count; idx++)
            PlayerFactions[remaining[idx]] = DraftFaction.CrewOther;
    }

    // === GET ALIGNMENTS FOR FACTION ===

    private static List<RoleAlignment> GetAlignmentsForFaction(DraftFaction faction)
    {
        var alignments = new List<RoleAlignment>();

        switch (faction)
        {
            case DraftFaction.Impostor:
                alignments.Add(RoleAlignment.ImpostorConcealing);
                alignments.Add(RoleAlignment.ImpostorKilling);
                alignments.Add(RoleAlignment.ImpostorPower);
                alignments.Add(RoleAlignment.ImpostorSupport);
                break;
            case DraftFaction.NeutralKilling:
                alignments.Add(RoleAlignment.NeutralKilling);
                break;
            case DraftFaction.CrewOther:
                alignments.Add(RoleAlignment.CrewmateInvestigative);
                alignments.Add(RoleAlignment.CrewmateKilling);
                alignments.Add(RoleAlignment.CrewmateProtective);
                alignments.Add(RoleAlignment.CrewmatePower);
                alignments.Add(RoleAlignment.CrewmateSupport);
                break;
        }

        return alignments;
    }

    // === ROLE POOL ===

    private static List<RoleBehaviour> GetRolesForAlignment(RoleAlignment alignment)
    {
        var result = new List<RoleBehaviour>();
        foreach (var role in MiscUtils.GetRegisteredRoles(alignment))
        {
            if (role.IsDead) continue;
            if (!CustomRoleUtils.CanSpawnOnCurrentMode(role)) continue;

            var assignData = MiscUtils.GetAssignData(role.Role);
            if (assignData.Chance <= 0 || assignData.Count <= 0) continue;

            if (AlreadyPicked.Contains((ushort)role.Role) &&
                role.Role != RoleTypes.Crewmate &&
                role.Role != RoleTypes.Impostor)
                continue;

            if (result.Any(r => r.Role == role.Role)) continue;
            result.Add(role);
        }
        return result;
    }

    private static List<RoleBehaviour> GetRolesForAlignments(List<RoleAlignment> alignments)
    {
        var result = new List<RoleBehaviour>();
        foreach (var alignment in alignments)
        {
            foreach (var role in GetRolesForAlignment(alignment))
            {
                if (result.Any(r => r.Role == role.Role)) continue;
                result.Add(role);
            }
        }
        return result;
    }

    private static IEnumerable<T> WeightedShuffle<T>(IEnumerable<T> items, System.Func<T, float> weightSelector)
    {
        var pool = items.ToList();
        var result = new List<T>();

        while (pool.Count > 0)
        {
            float totalWeight = pool.Sum(weightSelector);
            if (totalWeight <= 0)
            {
                for (int i = pool.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (pool[i], pool[j]) = (pool[j], pool[i]);
                }
                result.AddRange(pool);
                break;
            }

            float r = Random.Range(0f, totalWeight);
            float current = 0f;
            T selected = pool.First();

            foreach (var item in pool)
            {
                current += weightSelector(item);
                if (r <= current)
                {
                    selected = item;
                    break;
                }
            }

            result.Add(selected);
            pool.Remove(selected);
        }

        return result;
    }

    private static IEnumerable<RoleBehaviour> OrderRoles(IEnumerable<RoleBehaviour> roles)
    {
        bool respectChances;
        try { respectChances = OptionGroupSingleton<DraftModeOptions>.Instance.RespectRoleChances.Value; }
        catch { respectChances = false; }

        if (!respectChances)
            return roles.OrderBy(_ => Random.Range(0f, 1f));

        return WeightedShuffle(roles, r => Mathf.Max(0.1f, MiscUtils.GetAssignData(r.Role).Chance));
    }

    public static List<RoleBehaviour> SelectRolesToOffer(bool isImpostor)
    {
        var myId = PlayerControl.LocalPlayer?.PlayerId ?? 255;
        DraftFaction faction;

        if (PlayerFactions.TryGetValue(myId, out var assignedFaction))
            faction = assignedFaction;
        else if (isImpostor)
            faction = DraftFaction.Impostor;
        else
            faction = DraftFaction.CrewOther;

        var enabledAlignments = GetAlignmentsForFaction(faction);
        if (enabledAlignments.Count == 0) return new List<RoleBehaviour>();

        bool respectChances;
        try { respectChances = OptionGroupSingleton<DraftModeOptions>.Instance.RespectRoleChances.Value; }
        catch { respectChances = false; }
        var roleCount = RolesToShow;

        // Unified Crewmate/Neutral Mix Logic
        if (faction == DraftFaction.CrewOther && !ShouldCrewmatesPickFromAllClasses())
        {
            var options = OptionGroupSingleton<DraftModeOptions>.Instance;
            var crewRoles = GetRolesForAlignments(enabledAlignments);

            // Count how many neutrals have already been picked
            int currentNeutrals = 0;
            foreach (var pick in DraftPicks.Values)
            {
                var role = RoleManager.Instance.GetRole((RoleTypes)pick);
                if (role != null)
                {
                    bool isNeutral = MiscUtils.GetRegisteredRoles(RoleAlignment.NeutralBenign).Any(r => r.Role == role.Role) ||
                                     MiscUtils.GetRegisteredRoles(RoleAlignment.NeutralEvil).Any(r => r.Role == role.Role) ||
                                     MiscUtils.GetRegisteredRoles(RoleAlignment.NeutralOutlier).Any(r => r.Role == role.Role);
                    if (isNeutral) currentNeutrals++;
                }
            }

            int globalMin = (int)options.MinOtherNeutrals.Value;
            int globalMax = (int)options.MaxOtherNeutrals.Value;
            int remainingPlayers = PickOrder.Count;
            
            // If we MUST have more neutrals and we are running out of players, force neutrals
            bool forceNeutrals = (globalMin - currentNeutrals) >= remainingPlayers;
            int neutralSlotsAvailable = Mathf.Max(0, globalMax - currentNeutrals);
            
            int wantedNeutrals;
            if (forceNeutrals)
            {
                wantedNeutrals = roleCount; // Offer as many as possible
            }
            else
            {
                int minP = (int)options.MinOtherNeutralsPerChoice.Value;
                int maxP = (int)options.MaxOtherNeutralsPerChoice.Value;
                wantedNeutrals = (Random.Range(0f, 100f) < 30f) ? maxP : minP;
            }

            // Constrain wantedNeutrals by global limit and pool size
            wantedNeutrals = Mathf.Min(wantedNeutrals, neutralSlotsAvailable);
            wantedNeutrals = Mathf.Min(wantedNeutrals, roleCount);

            var finalPool = new List<RoleBehaviour>();
            
            if (wantedNeutrals > 0)
            {
                var benignPool = OrderRoles(GetRolesForAlignment(RoleAlignment.NeutralBenign)).ToList();
                var evilPool = OrderRoles(GetRolesForAlignment(RoleAlignment.NeutralEvil)).ToList();
                var outlierPool = OrderRoles(GetRolesForAlignment(RoleAlignment.NeutralOutlier)).ToList();
                
                // Mix in neutrals
                int nShown = 0;
                int bIdx = 0, eIdx = 0, oIdx = 0;
                while (nShown < wantedNeutrals)
                {
                    bool added = false;
                    if (bIdx < benignPool.Count && nShown < wantedNeutrals) { finalPool.Add(benignPool[bIdx++]); nShown++; added = true; }
                    if (eIdx < evilPool.Count && nShown < wantedNeutrals) { finalPool.Add(evilPool[eIdx++]); nShown++; added = true; }
                    if (oIdx < outlierPool.Count && nShown < wantedNeutrals) { finalPool.Add(outlierPool[oIdx++]); nShown++; added = true; }
                    if (!added) break;
                }
            }

            // If we are forcing neutrals, and couldn't find enough, we should still not offer Crew
            if (forceNeutrals && finalPool.Count > 0)
            {
                SelectedAlignment = null;
                return finalPool.OrderBy(_ => Random.Range(0f, 1f)).ToList();
            }

            // Fill remaining with Crewmates
            var chosenCrew = OrderRoles(crewRoles).Take(roleCount - finalPool.Count).ToList();
            finalPool.AddRange(chosenCrew);

            // Pad if still empty
            if (finalPool.Count < roleCount)
            {
                var fallback = crewRoles.Where(r => !finalPool.Contains(r)).ToList();
                finalPool.AddRange(OrderRoles(fallback).Take(roleCount - finalPool.Count));
            }

            SelectedAlignment = null;
            return finalPool.OrderBy(_ => Random.Range(0f, 1f)).ToList();
        }

        // Guaranteed 100% Roles Logic
        if (respectChances)
        {
            var guaranteedRoles = GetRolesForAlignments(enabledAlignments)
                .Where(r => MiscUtils.GetAssignData(r.Role).Chance >= 100)
                .ToList();

            if (guaranteedRoles.Count > 0)
            {
                SelectedAlignment = null;
                
                if (guaranteedRoles.Count >= roleCount)
                {
                    return guaranteedRoles.OrderBy(_ => Random.Range(0f, 1f)).Take(roleCount).ToList();
                }
                else
                {
                    var finalRoles = new List<RoleBehaviour>(guaranteedRoles);
                    var otherRoles = GetRolesForAlignments(enabledAlignments)
                        .Where(x => !guaranteedRoles.Contains(x))
                        .ToList();
                        
                    var padRoles = OrderRoles(otherRoles)
                        .Take(roleCount - finalRoles.Count)
                        .ToList();
                        
                    finalRoles.AddRange(padRoles);
                    return finalRoles.OrderBy(_ => Random.Range(0f, 1f)).ToList();
                }
            }
        }

        // Impostor special flow - all classes
        if (faction == DraftFaction.Impostor && ShouldImpostorsPickFromAllClasses())
        {
            var allImpRoles = GetRolesForAlignments(enabledAlignments);
            if (allImpRoles.Count == 0)
                return new List<RoleBehaviour>();

            SelectedAlignment = null;

            if (allImpRoles.Count <= roleCount)
                return allImpRoles.OrderBy(_ => Random.Range(0f, 1f)).ToList();

            return OrderRoles(allImpRoles)
                .Take(roleCount)
                .OrderBy(_ => Random.Range(0f, 1f))
                .ToList();
        }

        // Crewmate special flow - all classes
        if (faction == DraftFaction.CrewOther && ShouldCrewmatesPickFromAllClasses())
        {
            var allCrewRoles = GetRolesForAlignments(enabledAlignments);
            if (allCrewRoles.Count == 0)
                return new List<RoleBehaviour>();

            SelectedAlignment = null;

            if (allCrewRoles.Count <= roleCount)
                return allCrewRoles.OrderBy(_ => Random.Range(0f, 1f)).ToList();

            return OrderRoles(allCrewRoles)
                .Take(roleCount)
                .OrderBy(_ => Random.Range(0f, 1f))
                .ToList();
        }

        // Standard flow
        var shuffledAlignments = enabledAlignments.OrderBy(_ => Random.Range(0f, 1f)).ToList();
        
        if (respectChances)
        {
            shuffledAlignments = WeightedShuffle(enabledAlignments, a => 
            {
                var rolesInA = GetRolesForAlignment(a);
                if (rolesInA.Count == 0) return 0f;
                return rolesInA.Sum(r => Mathf.Max(0.1f, MiscUtils.GetAssignData(r.Role).Chance));
            }).ToList();
        }
        
        List<RoleBehaviour> rolesFromAlignment = null;
        RoleAlignment chosenAlignment = default;

        foreach (var alignment in shuffledAlignments)
        {
            var roles = GetRolesForAlignment(alignment);
            if (roles.Count > 0)
            {
                rolesFromAlignment = roles;
                chosenAlignment = alignment;
                break;
            }
        }

        if (rolesFromAlignment == null || rolesFromAlignment.Count == 0)
            return new List<RoleBehaviour>();

        SelectedAlignment = chosenAlignment;

        if (rolesFromAlignment.Count <= roleCount)
            return rolesFromAlignment;

        return OrderRoles(rolesFromAlignment)
            .Take(roleCount)
            .OrderBy(_ => Random.Range(0f, 1f))
            .ToList();
    }

    public static RoleBehaviour? PickRandomRole(bool isImpostor, List<RoleBehaviour>? excludeOffered = null)
    {
        var myId = PlayerControl.LocalPlayer?.PlayerId ?? 255;
        
        DraftFaction faction;
        if (!PlayerFactions.TryGetValue(myId, out faction))
            faction = isImpostor ? DraftFaction.Impostor : DraftFaction.CrewOther;

        // OtherNeutral was removed, so we fallback to normal logic
        var enabledAlignments = GetAlignmentsForFaction(faction);
        bool respectChances;
        try { respectChances = OptionGroupSingleton<DraftModeOptions>.Instance.RespectRoleChances.Value; }
        catch { respectChances = false; }

        if (respectChances)
        {
            var guaranteedRoles = GetRolesForAlignments(enabledAlignments)
                .Where(r => MiscUtils.GetAssignData(r.Role).Chance >= 100)
                .ToList();

            if (excludeOffered != null && excludeOffered.Count > 0)
            {
                var offeredIds = excludeOffered.Select(r => r.Role).ToHashSet();
                guaranteedRoles = guaranteedRoles.Where(r => !offeredIds.Contains(r.Role)).ToList();
            }

            if (guaranteedRoles.Count > 0)
                return guaranteedRoles.OrderBy(_ => Random.Range(0f, 1f)).First();
        }

        // Impostor all-classes random pick
        if (isImpostor && ShouldImpostorsPickFromAllClasses())
        {
            DraftFaction impFaction;

            if (PlayerFactions.TryGetValue(myId, out var assignedFaction))
                impFaction = assignedFaction;
            else
                impFaction = DraftFaction.Impostor;

            if (impFaction == DraftFaction.Impostor)
            {
                var alignments = GetAlignmentsForFaction(DraftFaction.Impostor);
                var pool = GetRolesForAlignments(alignments);

                if (excludeOffered != null && excludeOffered.Count > 0)
                {
                    var offeredIds = excludeOffered.Select(r => r.Role).ToHashSet();
                    var notOffered = pool.Where(r => !offeredIds.Contains(r.Role)).ToList();
                    if (notOffered.Count > 0)
                        return OrderRoles(notOffered).First();
                }

                if (pool.Count > 0)
                    return OrderRoles(pool).First();
            }
        }

        // Crewmate all-classes random pick
        if (!isImpostor && ShouldCrewmatesPickFromAllClasses())
        {
            DraftFaction crewFaction;

            if (PlayerFactions.TryGetValue(myId, out var assignedFaction))
                crewFaction = assignedFaction;
            else
                crewFaction = DraftFaction.CrewOther;

            if (crewFaction == DraftFaction.CrewOther)
            {
                var alignments = GetAlignmentsForFaction(DraftFaction.CrewOther);
                var pool = GetRolesForAlignments(alignments);

                if (excludeOffered != null && excludeOffered.Count > 0)
                {
                    var offeredIds = excludeOffered.Select(r => r.Role).ToHashSet();
                    var notOffered = pool.Where(r => !offeredIds.Contains(r.Role)).ToList();
                    if (notOffered.Count > 0)
                        return OrderRoles(notOffered).First();
                }

                if (pool.Count > 0)
                    return OrderRoles(pool).First();
            }
        }

        // Standard flow
        if (SelectedAlignment.HasValue)
        {
            var pool = GetRolesForAlignment(SelectedAlignment.Value);
            if (excludeOffered != null && excludeOffered.Count > 0)
            {
                var offeredIds = excludeOffered.Select(r => r.Role).ToHashSet();
                var notOffered = pool.Where(r => !offeredIds.Contains(r.Role)).ToList();
                if (notOffered.Count > 0)
                    return OrderRoles(notOffered).First();
            }

            if (pool.Count > 0)
                return OrderRoles(pool).First();
        }

        return isImpostor
            ? RoleManager.Instance.GetRole(RoleTypes.Impostor)
            : RoleManager.Instance.GetRole(RoleTypes.Crewmate);
    }

    // === LIFECYCLE ===

    public static void Reset()
    {
        IsRunning = false;
        DraftComplete = false;
        DraftActiveThisRound = false;
        PickOrder.Clear();
        AlreadyPicked.Clear();
        DraftPicks.Clear();
        ImpostorPlayerIds.Clear();
        PlayerFactions.Clear();
        LocalPlayerPicked = false;
        PickTimer = 0f;
        CurrentOfferedRoles = null;
        SelectedAlignment = null;
    }

    public static void GeneratePickOrder(List<byte> validPlayerIds = null)
    {
        PickOrder.Clear();

        List<byte> players;
        if (validPlayerIds != null && validPlayerIds.Count > 0)
        {
            players = new List<byte>(validPlayerIds);
        }
        else
        {
            players = new List<byte>();
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player != null && player.Data != null && !player.Data.Disconnected &&
                    !TownOfUs.Roles.Other.SpectatorRole.TrackedSpectators.Contains(player.Data.PlayerName))
                    players.Add(player.PlayerId);
            }
        }

        if (players.Count == 0) return;

        // --- SYSTEM OF THIRDS LOGIC ---
        
        // 1. Categorize players
        var specialPlayers = players.Where(id => 
            PlayerFactions.ContainsKey(id) && 
            PlayerFactions[id] != DraftFaction.CrewOther).ToList();
        
        var crewPlayers = players.Where(id => 
            !PlayerFactions.ContainsKey(id) || 
            PlayerFactions[id] == DraftFaction.CrewOther).ToList();

        specialPlayers.Shuffle();
        crewPlayers.Shuffle();

        // 2. Divide into 3 buckets
        int count = players.Count;
        int bucketSize = count / 3;
        int remainder = count % 3;

        int[] bucketSizes = new int[3];
        bucketSizes[0] = bucketSize + (remainder > 0 ? 1 : 0);
        bucketSizes[1] = bucketSize + (remainder > 1 ? 1 : 0);
        bucketSizes[2] = bucketSize;

        List<byte>[] buckets = new List<byte>[3] { new(), new(), new() };

        // 3. Distribute Special Players evenly
        int specialIdx = 0;
        int bIdx = 0;
        while (specialIdx < specialPlayers.Count)
        {
            buckets[bIdx].Add(specialPlayers[specialIdx++]);
            bIdx = (bIdx + 1) % 3;
        }

        // 4. Fill with Crewmates
        int crewIdx = 0;
        for (int i = 0; i < 3; i++)
        {
            while (buckets[i].Count < bucketSizes[i] && crewIdx < crewPlayers.Count)
            {
                buckets[i].Add(crewPlayers[crewIdx++]);
            }
        }

        // 5. Shuffle each bucket and combine
        for (int i = 0; i < 3; i++)
        {
            buckets[i].Shuffle();
            PickOrder.AddRange(buckets[i]);
        }
    }

    public static void RegisterPick(byte playerId, ushort roleId)
    {
        AlreadyPicked.Add(roleId);
        DraftPicks[playerId] = roleId;
        PickOrder.Remove(playerId);
        PickTimer = 0f;

        if (playerId == PlayerControl.LocalPlayer?.PlayerId)
        {
            LocalPlayerPicked = true;
            CurrentOfferedRoles = null;
            SelectedAlignment = null;
        }
    }

    public static byte? CurrentPicker => PickOrder.Count > 0 ? PickOrder[0] : null;

    public static bool IsMyTurn =>
        CurrentPicker.HasValue &&
        PlayerControl.LocalPlayer != null &&
        CurrentPicker.Value == PlayerControl.LocalPlayer.PlayerId;

    public static int TurnsUntilMyPick
    {
        get
        {
            if (PlayerControl.LocalPlayer == null) return -1;
            return PickOrder.IndexOf(PlayerControl.LocalPlayer.PlayerId);
        }
    }

    public static void Shuffle<T>(this List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
