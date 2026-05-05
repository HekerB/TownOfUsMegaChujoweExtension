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
    NeutralBenign,
    NeutralEvil,
    NeutralKilling,
    RandomNeutral,
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

        int neutralBenignCount = GetSafeCount(options.MinNeutralBenign.Value, options.MaxNeutralBenign.Value);
        int neutralEvilCount = GetSafeCount(options.MinNeutralEvil.Value, options.MaxNeutralEvil.Value);
        int neutralKillingCount = GetSafeCount(options.MinNeutralKilling.Value, options.MaxNeutralKilling.Value);
        int randomNeutralCount = GetSafeCount(options.MinRandomNeutral.Value, options.MaxRandomNeutral.Value);

        int totalSpecial = neutralBenignCount + neutralEvilCount + neutralKillingCount + randomNeutralCount;
        if (totalSpecial > remaining.Count)
        {
            float ratio = (float)remaining.Count / totalSpecial;
            neutralBenignCount = Mathf.FloorToInt(neutralBenignCount * ratio);
            neutralEvilCount = Mathf.FloorToInt(neutralEvilCount * ratio);
            neutralKillingCount = Mathf.FloorToInt(neutralKillingCount * ratio);
            randomNeutralCount = Mathf.FloorToInt(randomNeutralCount * ratio);
        }

        int idx = 0;
        for (int i = 0; i < neutralBenignCount && idx < remaining.Count; i++, idx++)
            PlayerFactions[remaining[idx]] = DraftFaction.NeutralBenign;

        for (int i = 0; i < neutralEvilCount && idx < remaining.Count; i++, idx++)
            PlayerFactions[remaining[idx]] = DraftFaction.NeutralEvil;

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

        for (int i = 0; i < randomNeutralCount && idx < remaining.Count; i++, idx++)
            PlayerFactions[remaining[idx]] = DraftFaction.RandomNeutral;

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
            case DraftFaction.NeutralBenign:
                alignments.Add(RoleAlignment.NeutralBenign);
                break;
            case DraftFaction.NeutralEvil:
                alignments.Add(RoleAlignment.NeutralEvil);
                break;
            case DraftFaction.NeutralKilling:
                alignments.Add(RoleAlignment.NeutralKilling);
                break;
            case DraftFaction.RandomNeutral:
                alignments.Add(RoleAlignment.NeutralBenign);
                alignments.Add(RoleAlignment.NeutralEvil);
                alignments.Add(RoleAlignment.NeutralOutlier);
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

        // Merge Neutrals with Crew Logic
        if (faction == DraftFaction.CrewOther && !ShouldCrewmatesPickFromAllClasses())
        {
            var options = OptionGroupSingleton<DraftModeOptions>.Instance;
            if (options.MergeNeutralsWithCrew.Value && Random.Range(0f, 100f) < options.NeutralMergeChance.Value)
            {
                var crewRoles = GetRolesForAlignments(enabledAlignments);
                var neutralAlignments = new List<RoleAlignment> 
                { 
                    RoleAlignment.NeutralBenign, 
                    RoleAlignment.NeutralEvil,
                    RoleAlignment.NeutralOutlier
                };
                var neutralRoles = GetRolesForAlignments(neutralAlignments);

                if (crewRoles.Count > 0 && neutralRoles.Count > 0)
                {
                    SelectedAlignment = null;

                    // Determine how many neutrals to show (at least 1, up to MaxNeutralsInMerge or roleCount-1)
                    int maxAllowed = (int)options.MaxNeutralsInMerge.Value;
                    int wantedNeutrals = Random.Range(1, maxAllowed + 1);
                    wantedNeutrals = Mathf.Min(wantedNeutrals, neutralRoles.Count);
                    wantedNeutrals = Mathf.Min(wantedNeutrals, roleCount - 1); 

                    var chosenNeutrals = (respectChances ? OrderRoles(neutralRoles) : neutralRoles.OrderBy(_ => Random.Range(0f, 1f)))
                        .Take(wantedNeutrals).ToList();
                    
                    var chosenCrew = (respectChances ? OrderRoles(crewRoles) : crewRoles.OrderBy(_ => Random.Range(0f, 1f)))
                        .Take(roleCount - chosenNeutrals.Count).ToList();

                    var finalPool = chosenNeutrals.Concat(chosenCrew).ToList();
                    
                    // If we still need more roles (e.g. pools were small), pad from either
                    if (finalPool.Count < roleCount)
                    {
                        var remainingPool = crewRoles.Concat(neutralRoles).Where(r => !finalPool.Contains(r)).ToList();
                        var pad = (respectChances ? OrderRoles(remainingPool) : remainingPool.OrderBy(_ => Random.Range(0f, 1f)))
                            .Take(roleCount - finalPool.Count);
                        finalPool.AddRange(pad);
                    }

                    return finalPool.OrderBy(_ => Random.Range(0f, 1f)).ToList();
                }
            }
        }

        // RandomNeutral
        if (faction == DraftFaction.RandomNeutral)
        {
            var benignPool = GetRolesForAlignment(RoleAlignment.NeutralBenign);
            var evilPool = GetRolesForAlignment(RoleAlignment.NeutralEvil);
            var outlierPool = GetRolesForAlignment(RoleAlignment.NeutralOutlier);

            benignPool = OrderRoles(benignPool).ToList();
            
            var nonBenignPool = evilPool.Concat(outlierPool).ToList();
            nonBenignPool = OrderRoles(nonBenignPool).ToList();

            int wantedBenign = Mathf.Max(1, Mathf.CeilToInt(roleCount * 2f / 3f));
            int wantedNonBenign = roleCount - wantedBenign;

            var result = new List<RoleBehaviour>();

            int actualBenign = Mathf.Min(wantedBenign, benignPool.Count);
            for (int i = 0; i < actualBenign; i++)
                result.Add(benignPool[i]);

            int actualNonBenign = Mathf.Min(wantedNonBenign, nonBenignPool.Count);
            for (int i = 0; i < actualNonBenign; i++)
                result.Add(nonBenignPool[i]);

            int missing = roleCount - result.Count;
            if (missing > 0 && actualBenign < benignPool.Count)
            {
                for (int i = actualBenign; i < benignPool.Count && missing > 0; i++, missing--)
                    result.Add(benignPool[i]);
            }

            if (missing > 0 && actualNonBenign < nonBenignPool.Count)
            {
                for (int i = actualNonBenign; i < nonBenignPool.Count && missing > 0; i++, missing--)
                    result.Add(nonBenignPool[i]);
            }

            SelectedAlignment = null;

            if (result.Count == 0) return new List<RoleBehaviour>();

            return result.OrderBy(_ => Random.Range(0f, 1f)).ToList();
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

        // RandomNeutral
        if (faction == DraftFaction.RandomNeutral)
        {
            var bp = GetRolesForAlignment(RoleAlignment.NeutralBenign);
            var ep = GetRolesForAlignment(RoleAlignment.NeutralEvil);
            var op = GetRolesForAlignment(RoleAlignment.NeutralOutlier);

            if (excludeOffered != null && excludeOffered.Count > 0)
            {
                var offeredIds = excludeOffered.Select(r => r.Role).ToHashSet();
                bp = bp.Where(r => !offeredIds.Contains(r.Role)).ToList();
                ep = ep.Where(r => !offeredIds.Contains(r.Role)).ToList();
                op = op.Where(r => !offeredIds.Contains(r.Role)).ToList();
            }

            var nonBp = ep.Concat(op).ToList();

            bool hasBenign = bp.Count > 0;
            bool hasNonBenign = nonBp.Count > 0;

            if (hasBenign && hasNonBenign)
            {
                if (Random.Range(0f, 1f) < 0.66f)
                    return OrderRoles(bp).First();
                else
                    return OrderRoles(nonBp).First();
            }
            else if (hasBenign)
                return OrderRoles(bp).First();
            else if (hasNonBenign)
                return OrderRoles(nonBp).First();
                
            var allRoles = bp.Concat(nonBp).ToList();
            if (allRoles.Count > 0)
                return OrderRoles(allRoles).First();
        }

        // Guaranteed 100% roles for pick
        if (faction != DraftFaction.RandomNeutral)
        {
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

        if (validPlayerIds != null && validPlayerIds.Count > 0)
        {
            var players = new List<byte>(validPlayerIds);
            players.Shuffle();
            PickOrder.AddRange(players);
            return;
        }

        // Fallback: build from AllPlayerControls, excluding spectators
        var fallback = new List<byte>();
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.Data != null && !player.Data.Disconnected &&
                !TownOfUs.Roles.Other.SpectatorRole.TrackedSpectators.Contains(player.Data.PlayerName))
                fallback.Add(player.PlayerId);
        }
        fallback.Shuffle();
        PickOrder.AddRange(fallback);
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
