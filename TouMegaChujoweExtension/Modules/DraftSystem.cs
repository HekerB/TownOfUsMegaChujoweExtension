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
    public static int TargetOtherNeutralCount { get; set; } // Global target for benign/evil/outliers

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
            if (min == max) return (int)min;
            return (Random.Range(0f, 100f) < 60f) ? (int)min : (int)max;
        }

        int neutralKillingCount = GetSafeCount(options.MinNeutralKilling.Value, options.MaxNeutralKilling.Value);
        TargetOtherNeutralCount = GetSafeCount(options.MinOtherNeutrals.Value, options.MaxOtherNeutrals.Value);

        if (neutralKillingCount + TargetOtherNeutralCount > remaining.Count)
        {
             // Adjust if we have more special roles than players
             float ratio = (float)remaining.Count / (neutralKillingCount + TargetOtherNeutralCount);
             neutralKillingCount = Mathf.FloorToInt(neutralKillingCount * ratio);
             TargetOtherNeutralCount = Mathf.FloorToInt(TargetOtherNeutralCount * ratio);
        }

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

    // === ROLE POOL CACHE ===
    private static readonly Dictionary<RoleAlignment, List<RoleBehaviour>> _roleCache = new();
    private static bool _roleCacheDirty = true;

    public static void InvalidateRoleCache()
    {
        _roleCacheDirty = true;
        _roleCache.Clear();
    }

    private static void EnsureRoleCacheBuilt()
    {
        if (!_roleCacheDirty && _roleCache.Count > 0) return;
        _roleCache.Clear();

        var allAlignments = (RoleAlignment[])Enum.GetValues(typeof(RoleAlignment));
        foreach (var alignment in allAlignments)
        {
            var roles = new List<RoleBehaviour>();
            foreach (var role in MiscUtils.GetRegisteredRoles(alignment))
            {
                if (role.IsDead) continue;
                if (!CustomRoleUtils.CanSpawnOnCurrentMode(role)) continue;
                if (roles.Any(r => r.Role == role.Role)) continue;

                var assignData = MiscUtils.GetAssignData(role.Role);
                if (assignData.Chance <= 0 || assignData.Count <= 0) continue;

                roles.Add(role);
            }
            _roleCache[alignment] = roles;
        }
        _roleCacheDirty = false;
    }

    private static List<RoleBehaviour> GetRolesForAlignment(RoleAlignment alignment)
    {
        EnsureRoleCacheBuilt();
        if (!_roleCache.TryGetValue(alignment, out var cached)) return new List<RoleBehaviour>();

        return cached.Where(r =>
            !AlreadyPicked.Contains((ushort)r.Role) ||
            r.Role == RoleTypes.Crewmate ||
            r.Role == RoleTypes.Impostor
        ).ToList();
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
        if (faction == DraftFaction.CrewOther)
        {
            var options = OptionGroupSingleton<DraftModeOptions>.Instance;
            var crewPool = GetRolesForAlignments(enabledAlignments);

            // Calculate neutrals to mix in
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

            int remainingPlayers = PickOrder.Count;
            
            // Logic: How many more neutrals do we NEED to hit the target?
            int neutralsNeeded = Mathf.Max(0, TargetOtherNeutralCount - currentNeutrals);
            
            // If we MUST have more neutrals and we are running out of players, force neutrals
            bool forceNeutrals = neutralsNeeded >= remainingPlayers;
            
            // If we already hit the global target, don't show any more neutrals
            bool limitReached = currentNeutrals >= TargetOtherNeutralCount;
            
            int wantedNeutrals = 0;
            if (!limitReached)
            {
                if (forceNeutrals)
                {
                    wantedNeutrals = roleCount; // Force as many as possible
                }
                else
                {
                    // Random choice 60/40
                    wantedNeutrals = (Random.Range(0f, 100f) < 60f) ? (int)options.MinOtherNeutralsPerChoice.Value : (int)options.MaxOtherNeutralsPerChoice.Value;
                }
            }

            // Constrain wantedNeutrals by global target and pool size
            wantedNeutrals = Mathf.Min(wantedNeutrals, neutralsNeeded > 0 ? neutralsNeeded : (limitReached ? 0 : 99));
            wantedNeutrals = Mathf.Min(wantedNeutrals, roleCount);

            var finalPool = new List<RoleBehaviour>();
            if (wantedNeutrals > 0)
            {
                var neutralPool = new List<RoleAlignment> { RoleAlignment.NeutralBenign, RoleAlignment.NeutralEvil, RoleAlignment.NeutralOutlier };
                var allNeutrals = OrderRoles(GetRolesForAlignments(neutralPool)).ToList();
                finalPool.AddRange(allNeutrals.Take(wantedNeutrals));
            }

            // Fill remaining with Crewmates (only if not forcing neutrals)
            if (!forceNeutrals)
            {
                finalPool.AddRange(OrderRoles(crewPool).Take(roleCount - finalPool.Count));
            }

            // Pad if necessary
            if (finalPool.Count < roleCount)
            {
                var fallback = GetRolesForAlignments(enabledAlignments).Where(r => !finalPool.Any(p => p.Role == r.Role)).ToList();
                finalPool.AddRange(OrderRoles(fallback).Take(roleCount - finalPool.Count));
            }

            return finalPool.OrderBy(_ => Random.Range(0f, 1f)).ToList();
        }

        // Standard flow for others (Impostor, NeutralKilling) - always All Classes
        var allRoles = GetRolesForAlignments(enabledAlignments);
        if (allRoles.Count == 0) return new List<RoleBehaviour>();

        SelectedAlignment = null;

        if (allRoles.Count <= roleCount)
            return allRoles.OrderBy(_ => Random.Range(0f, 1f)).ToList();

        return OrderRoles(allRoles)
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

        var enabledAlignments = GetAlignmentsForFaction(faction);
        
        // --- Crewmate/Neutral Mixing Logic for Random Pick ---
        if (faction == DraftFaction.CrewOther)
        {
            var options = OptionGroupSingleton<DraftModeOptions>.Instance;
            
            // Calculate current neutrals picked
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

            int remainingPlayers = PickOrder.Count;
            int neutralsNeeded = Mathf.Max(0, TargetOtherNeutralCount - currentNeutrals);
            bool forceNeutrals = neutralsNeeded >= remainingPlayers;
            bool limitReached = currentNeutrals >= TargetOtherNeutralCount;

            // Determine if this random pick should be a Neutral
            bool shouldBeNeutral = false;
            if (!limitReached)
            {
                if (forceNeutrals) 
                {
                    shouldBeNeutral = true;
                }
                else
                {
                    // Calculate a probability to pick a Neutral randomly if they were possible in choices
                    // We use the 60/40 logic to see if neutrals WERE in the choices, and then roll
                    int wantedNeutrals = (Random.Range(0f, 100f) < 60f) ? (int)options.MinOtherNeutralsPerChoice.Value : (int)options.MaxOtherNeutralsPerChoice.Value;
                    
                    if (wantedNeutrals > 0)
                    {
                        // Proportional chance: if 1/3 roles are neutral, 33% chance. If 2/3, 66% chance.
                        float ratio = (float)wantedNeutrals / RolesToShow;
                        shouldBeNeutral = Random.Range(0f, 1f) < ratio;
                    }
                }
            }

            if (shouldBeNeutral)
            {
                var neutralPool = new List<RoleAlignment> { RoleAlignment.NeutralBenign, RoleAlignment.NeutralEvil, RoleAlignment.NeutralOutlier };
                var roles = GetRolesForAlignments(neutralPool);
                if (excludeOffered != null)
                {
                    var offeredIds = excludeOffered.Select(r => r.Role).ToHashSet();
                    roles = roles.Where(r => !offeredIds.Contains(r.Role)).ToList();
                }
                if (roles.Count > 0) return OrderRoles(roles).First();
            }
        }

        // Standard flow
        var pool = GetRolesForAlignments(enabledAlignments);

        if (excludeOffered != null && excludeOffered.Count > 0)
        {
            var offeredIds = excludeOffered.Select(r => r.Role).ToHashSet();
            var notOffered = pool.Where(r => !offeredIds.Contains(r.Role)).ToList();
            if (notOffered.Count > 0)
                return OrderRoles(notOffered).First();
        }

        if (pool.Count > 0)
            return OrderRoles(pool).First();

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
        InvalidateRoleCache();
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
