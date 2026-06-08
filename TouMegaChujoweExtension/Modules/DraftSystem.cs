using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Options;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using System.Linq;
using System;
using TownOfUs.Roles;
using TownOfUs.Options;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public enum DraftFaction
{
    Impostor,
    NeutralKilling,
    CrewOther
}

public static class DraftSystem
{
    public const int MaxRoleListSlots = 20;

    // === STATE ===
    public static bool IsRunning { get; set; }
    public static bool DraftComplete { get; set; }
    public static List<byte> PickOrder { get; } = new();
    public static List<byte> OriginalPickOrder { get; } = new();
    public static List<int> RoleListSlotOrder { get; } = new();
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
    public static bool LonerReducedImpostorSlot { get; set; }

    // === FACTION ASSIGNMENTS ===
    private static int GetSafeCount(float min, float max)
    {
        if (min > max || Mathf.Approximately(min, max)) return Mathf.RoundToInt(min);
        return (Random.Range(0f, 100f) < 60f) ? Mathf.RoundToInt(min) : Mathf.RoundToInt(max);
    }
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

        if (options == null)
        {
            UnityEngine.Debug.LogError("[TOUMCE] DraftModeOptions instance is null in AssignFactions!");
            return;
        }

        var oldOptions = OptionGroupSingleton<DraftOldSettingsOptions>.Instance;
        int neutralKillingCount = GetSafeCount(oldOptions.MinNeutralKilling.Value, oldOptions.MaxNeutralKilling.Value);
        TargetOtherNeutralCount = GetSafeCount(oldOptions.MinOtherNeutrals.Value, oldOptions.MaxOtherNeutrals.Value);

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
        var nkBiasPercent = options.ReductionChance.Value / 100f;
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

        var generalOptions = OptionGroupSingleton<ExtensionGameMechanicOptions>.Instance;
        
        bool preventVampires = false;
        bool preventJackal = false;

        if (generalOptions != null && generalOptions.PreventVampiresWithJackal)
        {
            var jackalId = (ushort)RoleId.Get<TouMegaChujoweExtension.Roles.Classic.Neutral.JackalRole>();
            var vampireId = (ushort)RoleId.Get<TownOfUs.Roles.Neutral.VampireRole>();

            bool jackalPicked = AlreadyPicked.Contains(jackalId);
            bool vampirePicked = AlreadyPicked.Contains(vampireId);

            if (jackalPicked)
            {
                preventVampires = true;
            }
            else if (vampirePicked)
            {
                preventJackal = true;
            }
        }

        var allAlignments = (RoleAlignment[])Enum.GetValues(typeof(RoleAlignment));
        foreach (var alignment in allAlignments)
        {
            var roles = new List<RoleBehaviour>();
            foreach (var role in MiscUtils.GetRegisteredRoles(alignment))
            {
                if (role.IsDead) continue;
                if (!CustomRoleUtils.CanSpawnOnCurrentMode(role)) continue;
                if (roles.Any(r => r.Role == role.Role)) continue;

                var isJackalRole = role.Role == (AmongUs.GameOptions.RoleTypes)RoleId.Get<TouMegaChujoweExtension.Roles.Classic.Neutral.JackalRole>();
                var isVampireRole = role.Role == (AmongUs.GameOptions.RoleTypes)RoleId.Get<TownOfUs.Roles.Neutral.VampireRole>();

                if (preventVampires && isVampireRole) continue;
                if (preventJackal && isJackalRole) continue;

                var assignData = MiscUtils.GetAssignData(role.Role);
                if (assignData.Count <= 0 || assignData.Chance <= 0) continue;

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

    private static bool CanCurrentPickerTakeLoner()
    {
        var myId = PlayerControl.LocalPlayer?.PlayerId;
        if (!myId.HasValue || LonerReducedImpostorSlot)
        {
            return false;
        }

        if (!PlayerFactions.TryGetValue(myId.Value, out var faction) || faction != DraftFaction.Impostor)
        {
            return false;
        }

        var previousImpostorPicked = DraftPicks.Keys.Any(id =>
            id != myId.Value &&
            PlayerFactions.TryGetValue(id, out var pickedFaction) &&
            pickedFaction == DraftFaction.Impostor);
        if (previousImpostorPicked)
        {
            return false;
        }

        var futureImpostorSlots = PickOrder.Count(id =>
            id != myId.Value &&
            PlayerFactions.TryGetValue(id, out var futureFaction) &&
            futureFaction == DraftFaction.Impostor);

        return futureImpostorSlots >= 1;
    }

    public static int GetActiveLobbyPlayerCount()
    {
        var count = 0;
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.Data == null || player.Data.Disconnected ||
                TownOfUs.Roles.Other.SpectatorRole.TrackedSpectators.Contains(player.Data.PlayerName))
            {
                continue;
            }

            count++;
        }

        return count;
    }

    public static int GetVisibleRoleListSlotCount()
    {
        var playerCount = GetActiveLobbyPlayerCount();
        if (playerCount <= 0 && GameData.Instance)
        {
            playerCount = GameData.Instance.PlayerCount;
        }

        return Mathf.Clamp(playerCount, 1, MaxRoleListSlots);
    }

    private static List<RoleBehaviour> FilterLonerForCurrentPicker(IEnumerable<RoleBehaviour> roles)
    {
        var lonerRole = (RoleTypes)RoleId.Get<TouMegaChujoweExtension.Roles.Classic.Impostor.LonerRole>();
        var canTakeLoner = CanCurrentPickerTakeLoner();

        return roles
            .Where(role => canTakeLoner || role.Role != lonerRole)
            .ToList();
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

        var roleList = roles.ToList();
        var guaranteed = new List<RoleBehaviour>();
        var passed = new List<RoleBehaviour>();
        var failed = new List<RoleBehaviour>();

        foreach (var r in roleList)
        {
            int chance = (int)MiscUtils.GetAssignData(r.Role).Chance;

            // Traktujemy role bez suwaka (0%) jako 100%
            if (chance <= 0) chance = 30;

            if (chance >= 100)
            {
                guaranteed.Add(r);
            }
            else if (Random.Range(0, 101) < chance)
            {
                passed.Add(r);
            }
            else
            {
                failed.Add(r);
            }
        }

        guaranteed.Shuffle();
        passed.Shuffle();
        failed.Shuffle();

        return guaranteed.Concat(passed).Concat(failed);
    }

    // === ROLE MIXING HELPERS ===
    private static int GetCurrentOtherNeutralCount()
    {
        int count = 0;
        foreach (var roleId in DraftPicks.Values)
        {
            if (IsOtherNeutral((RoleTypes)roleId)) count++;
        }
        return count;
    }

    private static bool IsOtherNeutral(RoleTypes roleId)
    {
        return MiscUtils.GetRegisteredRoles(RoleAlignment.NeutralBenign).Any(r => r.Role == roleId) ||
               MiscUtils.GetRegisteredRoles(RoleAlignment.NeutralEvil).Any(r => r.Role == roleId) ||
               MiscUtils.GetRegisteredRoles(RoleAlignment.NeutralOutlier).Any(r => r.Role == roleId);
    }

    public static List<RoleBehaviour> SelectRolesToOffer(bool isImpostor)
    {
        var options = OptionGroupSingleton<DraftModeOptions>.Instance;
        return options.PoolMode.Value switch
        {
            DraftPoolMode.MinMax => SelectMinMaxRolesToOffer(),
            DraftPoolMode.RoleList => SelectRoleListRolesToOffer(),
            _ => SelectOldRolesToOffer(isImpostor)
        };
    }

    private static List<RoleBehaviour> SelectOldRolesToOffer(bool isImpostor)
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

        var roleCount = RolesToShow;

        // Unified Crewmate/Neutral Mix Logic
        if (faction == DraftFaction.CrewOther)
        {
            var oldOptions = OptionGroupSingleton<DraftOldSettingsOptions>.Instance;
            var crewPool = GetRolesForAlignments(enabledAlignments);

            int currentNeutrals = GetCurrentOtherNeutralCount();
            int remainingEligiblePlayers = PickOrder.Count(id => PlayerFactions.TryGetValue(id, out var f) && f == DraftFaction.CrewOther);
            int neutralsNeeded = Mathf.Max(0, TargetOtherNeutralCount - currentNeutrals);

            // Logic: Do we MUST have more neutrals to hit the target?
            bool forceNeutrals = neutralsNeeded >= remainingEligiblePlayers && remainingEligiblePlayers > 0;
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
                    // Randomly decide if this player should see neutrals to spread them out
                    float offerChance = (float)neutralsNeeded / remainingEligiblePlayers;
                    if (UnityEngine.Random.Range(0f, 1f) < offerChance)
                    {
                        // If selected, use the per-choice options
                        wantedNeutrals = (UnityEngine.Random.Range(0f, 100f) < 60f) ? (int)oldOptions.MinOtherNeutralsPerChoice.Value : (int)oldOptions.MaxOtherNeutralsPerChoice.Value;
                        // Cap by needed to avoid exceeding global target early
                        wantedNeutrals = Mathf.Min(wantedNeutrals, neutralsNeeded);
                    }
                }
            }

            wantedNeutrals = Mathf.Min(wantedNeutrals, roleCount);

            var finalPool = new List<RoleBehaviour>();
            if (wantedNeutrals > 0)
            {
                var neutralPool = new List<RoleAlignment> { RoleAlignment.NeutralBenign, RoleAlignment.NeutralEvil, RoleAlignment.NeutralOutlier };
                var allNeutrals = OrderRoles(GetRolesForAlignments(neutralPool)).ToList();
                finalPool.AddRange(allNeutrals.Take(wantedNeutrals));
            }

            // Fill remaining with Crewmates (only if not forcing neutrals or if we didn't have enough neutrals)
            if (finalPool.Count < roleCount)
            {
                finalPool.AddRange(OrderRoles(crewPool).Take(roleCount - finalPool.Count));
            }

            return finalPool.OrderBy(_ => Random.Range(0f, 1f)).ToList();
        }

        // Standard flow for others (Impostor, NeutralKilling)
        var allRoles = faction == DraftFaction.Impostor
            ? FilterLonerForCurrentPicker(GetRolesForAlignments(enabledAlignments))
            : GetRolesForAlignments(enabledAlignments);
        if (allRoles.Count == 0) return new List<RoleBehaviour>();

        SelectedAlignment = null;

        if (allRoles.Count <= roleCount)
            return allRoles.OrderBy(_ => Random.Range(0f, 1f)).ToList();

        return OrderRoles(allRoles)
            .Take(roleCount)
            .OrderBy(_ => Random.Range(0f, 1f))
            .ToList();
    }

    private static List<RoleBehaviour> SelectMinMaxRolesToOffer()
    {
        var roleCount = RolesToShow;
        var availableAlignments = GetAllDraftAlignments()
            .Where(alignment => GetRemainingSlotsForAlignment(alignment) > 0)
            .ToList();

        if (availableAlignments.Count == 0)
        {
            return new List<RoleBehaviour>();
        }

        var pool = new List<RoleBehaviour>();
        foreach (var alignment in availableAlignments)
        {
            pool.AddRange(GetRolesForAlignment(alignment));
        }

        pool = FilterLonerForMinMax(pool)
            .GroupBy(role => role.Role)
            .Select(group => group.First())
            .ToList();

        if (pool.Count <= roleCount)
        {
            return pool.OrderBy(_ => Random.Range(0f, 1f)).ToList();
        }

        return OrderRoles(pool)
            .Take(roleCount)
            .OrderBy(_ => Random.Range(0f, 1f))
            .ToList();
    }

    private static List<RoleBehaviour> SelectRoleListRolesToOffer()
    {
        var bucket = GetCurrentRoleListBucket();
        var alignments = GetAlignmentsForRoleListBucket(bucket);
        if (alignments.Count == 0)
        {
            return new List<RoleBehaviour>();
        }

        var pool = GetRolesForAlignments(alignments);
        if (IsImpostorBucket(bucket))
        {
            pool = FilterLonerForRoleList(pool);
        }

        if (pool.Count <= RolesToShow)
        {
            return pool.OrderBy(_ => Random.Range(0f, 1f)).ToList();
        }

        return OrderRoles(pool)
            .Take(RolesToShow)
            .OrderBy(_ => Random.Range(0f, 1f))
            .ToList();
    }

    private static IEnumerable<RoleAlignment> GetAllDraftAlignments()
    {
        yield return RoleAlignment.CrewmateInvestigative;
        yield return RoleAlignment.CrewmateKilling;
        yield return RoleAlignment.CrewmateProtective;
        yield return RoleAlignment.CrewmatePower;
        yield return RoleAlignment.CrewmateSupport;
        yield return RoleAlignment.ImpostorConcealing;
        yield return RoleAlignment.ImpostorKilling;
        yield return RoleAlignment.ImpostorPower;
        yield return RoleAlignment.ImpostorSupport;
        yield return RoleAlignment.NeutralBenign;
        yield return RoleAlignment.NeutralEvil;
        yield return RoleAlignment.NeutralKilling;
        yield return RoleAlignment.NeutralOutlier;
    }

    private static int GetRemainingSlotsForAlignment(RoleAlignment alignment)
    {
        var crewOptions = OptionGroupSingleton<DraftCrewmateSettingsOptions>.Instance;
        var impOptions = OptionGroupSingleton<DraftImpostorSettingsOptions>.Instance;
        var neutralOptions = OptionGroupSingleton<DraftNeutralSettingsOptions>.Instance;
        var max = alignment switch
        {
            RoleAlignment.CrewmateInvestigative => (int)crewOptions.MaxCrewInvestigative.Value,
            RoleAlignment.CrewmateKilling => (int)crewOptions.MaxCrewKilling.Value,
            RoleAlignment.CrewmateProtective => (int)crewOptions.MaxCrewProtective.Value,
            RoleAlignment.CrewmatePower => (int)crewOptions.MaxCrewPower.Value,
            RoleAlignment.CrewmateSupport => (int)crewOptions.MaxCrewSupport.Value,
            RoleAlignment.ImpostorConcealing => (int)impOptions.MaxImpConcealing.Value,
            RoleAlignment.ImpostorKilling => (int)impOptions.MaxImpKilling.Value,
            RoleAlignment.ImpostorPower => (int)impOptions.MaxImpPower.Value,
            RoleAlignment.ImpostorSupport => (int)impOptions.MaxImpSupport.Value,
            RoleAlignment.NeutralBenign => (int)neutralOptions.MaxNeutralBenign.Value,
            RoleAlignment.NeutralEvil => (int)neutralOptions.MaxNeutralEvil.Value,
            RoleAlignment.NeutralKilling => (int)neutralOptions.MaxNeutralKillingRoles.Value,
            RoleAlignment.NeutralOutlier => (int)neutralOptions.MaxNeutralOutlier.Value,
            _ => 0
        };

        if (IsImpostorAlignment(alignment) && CountPicked(IsImpostorAlignment) >= (int)impOptions.MaxImpostorsTotal.Value)
        {
            return 0;
        }

        if (IsNeutralAlignment(alignment) && CountPicked(IsNeutralAlignment) >= (int)neutralOptions.MaxNeutralTotal.Value)
        {
            return 0;
        }

        return Mathf.Max(0, max - CountPicked(pickedAlignment => pickedAlignment == alignment));
    }

    private static int CountPicked(Func<RoleAlignment, bool> predicate)
    {
        var count = 0;
        foreach (var roleId in DraftPicks.Values)
        {
            var role = RoleManager.Instance.GetRole((RoleTypes)roleId);
            if (role is ITownOfUsRole touRole && predicate(touRole.RoleAlignment))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsImpostorAlignment(RoleAlignment alignment)
    {
        return alignment is RoleAlignment.ImpostorConcealing or
            RoleAlignment.ImpostorKilling or
            RoleAlignment.ImpostorPower or
            RoleAlignment.ImpostorSupport;
    }

    private static bool IsNeutralAlignment(RoleAlignment alignment)
    {
        return alignment is RoleAlignment.NeutralBenign or
            RoleAlignment.NeutralEvil or
            RoleAlignment.NeutralKilling or
            RoleAlignment.NeutralOutlier;
    }

    private static List<RoleBehaviour> FilterLonerForMinMax(IEnumerable<RoleBehaviour> roles)
    {
        var options = OptionGroupSingleton<DraftImpostorSettingsOptions>.Instance;
        var impostorsPicked = CountPicked(IsImpostorAlignment);
        var impostorSlotsRemaining = (int)options.MaxImpostorsTotal.Value - impostorsPicked;
        var lonerRole = (RoleTypes)RoleId.Get<TouMegaChujoweExtension.Roles.Classic.Impostor.LonerRole>();

        return roles
            .Where(role => role.Role != lonerRole || (impostorsPicked == 0 && impostorSlotsRemaining >= 2))
            .ToList();
    }

    private static List<RoleBehaviour> FilterLonerForRoleList(IEnumerable<RoleBehaviour> roles)
    {
        var lonerRole = (RoleTypes)RoleId.Get<TouMegaChujoweExtension.Roles.Classic.Impostor.LonerRole>();
        var previousImpostorPicked = DraftPicks.Values.Any(IsImpostorRole);
        var futureImpostorSlots = GetFutureRoleListBuckets()
            .Count(IsImpostorBucket);

        return roles
            .Where(role => role.Role != lonerRole || (!previousImpostorPicked && futureImpostorSlots >= 1))
            .ToList();
    }

    private static RoleListOption GetCurrentRoleListBucket()
    {
        var picker = CurrentPicker ?? PlayerControl.LocalPlayer?.PlayerId ?? byte.MaxValue;
        var slotIndex = OriginalPickOrder.IndexOf(picker);
        if (slotIndex < 0)
        {
            slotIndex = Mathf.Clamp(DraftPicks.Count, 0, 19);
        }

        return GetRoleListBucketForPickIndex(slotIndex);
    }

    private static IEnumerable<RoleListOption> GetFutureRoleListBuckets()
    {
        var currentPicker = CurrentPicker;
        var currentIndex = currentPicker.HasValue ? OriginalPickOrder.IndexOf(currentPicker.Value) : -1;
        if (currentIndex < 0)
        {
            currentIndex = DraftPicks.Count;
        }

        for (var i = currentIndex + 1; i < OriginalPickOrder.Count && i < 20; i++)
        {
            yield return GetRoleListBucketForPickIndex(i);
        }
    }

    public static RoleListOption GetRoleListBucketForPickIndex(int zeroBasedPickIndex)
    {
        var slotIndex = zeroBasedPickIndex;
        if (zeroBasedPickIndex >= 0 && zeroBasedPickIndex < RoleListSlotOrder.Count)
        {
            slotIndex = RoleListSlotOrder[zeroBasedPickIndex];
        }

        return GetRoleListBucketForSlot(slotIndex);
    }

    private static RoleListOption GetRoleListBucketForSlot(int zeroBasedSlot)
    {
        var options = OptionGroupSingleton<DraftRoleListSettingsOptions>.Instance;
        return zeroBasedSlot switch
        {
            0 => options.Slot1.Value,
            1 => options.Slot2.Value,
            2 => options.Slot3.Value,
            3 => options.Slot4.Value,
            4 => options.Slot5.Value,
            5 => options.Slot6.Value,
            6 => options.Slot7.Value,
            7 => options.Slot8.Value,
            8 => options.Slot9.Value,
            9 => options.Slot10.Value,
            10 => options.Slot11.Value,
            11 => options.Slot12.Value,
            12 => options.Slot13.Value,
            13 => options.Slot14.Value,
            14 => options.Slot15.Value,
            15 => options.Slot16.Value,
            16 => options.Slot17.Value,
            17 => options.Slot18.Value,
            18 => options.Slot19.Value,
            19 => options.Slot20.Value,
            _ => RoleListOption.NonImp
        };
    }

    private static bool IsImpostorBucket(RoleListOption bucket)
    {
        return bucket is RoleListOption.ImpConceal or
            RoleListOption.ImpKilling or
            RoleListOption.ImpPower or
            RoleListOption.ImpSupport or
            RoleListOption.ImpCommon or
            RoleListOption.ImpSpecial or
            RoleListOption.ImpRandom;
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

    private static List<RoleAlignment> GetAlignmentsForRoleListBucket(RoleListOption bucket)
    {
        return bucket switch
        {
            RoleListOption.CrewInvest => [RoleAlignment.CrewmateInvestigative],
            RoleListOption.CrewKilling => [RoleAlignment.CrewmateKilling],
            RoleListOption.CrewProtective => [RoleAlignment.CrewmateProtective],
            RoleListOption.CrewPower => [RoleAlignment.CrewmatePower],
            RoleListOption.CrewSupport => [RoleAlignment.CrewmateSupport],
            RoleListOption.CrewCommon => [RoleAlignment.CrewmateInvestigative, RoleAlignment.CrewmateProtective, RoleAlignment.CrewmateSupport],
            RoleListOption.CrewSpecial => [RoleAlignment.CrewmateKilling, RoleAlignment.CrewmatePower],
            RoleListOption.CrewRandom => [RoleAlignment.CrewmateInvestigative, RoleAlignment.CrewmateKilling, RoleAlignment.CrewmateProtective, RoleAlignment.CrewmatePower, RoleAlignment.CrewmateSupport],
            RoleListOption.NeutBenign => [RoleAlignment.NeutralBenign],
            RoleListOption.NeutEvil => [RoleAlignment.NeutralEvil],
            RoleListOption.NeutKilling => [RoleAlignment.NeutralKilling],
            RoleListOption.NeutOutlier => [RoleAlignment.NeutralOutlier],
            RoleListOption.NeutCommon => [RoleAlignment.NeutralBenign, RoleAlignment.NeutralEvil],
            RoleListOption.NeutSpecial => [RoleAlignment.NeutralKilling, RoleAlignment.NeutralOutlier],
            RoleListOption.NeutWildcard => [RoleAlignment.NeutralBenign, RoleAlignment.NeutralEvil, RoleAlignment.NeutralOutlier],
            RoleListOption.NeutRandom => [RoleAlignment.NeutralBenign, RoleAlignment.NeutralEvil, RoleAlignment.NeutralKilling, RoleAlignment.NeutralOutlier],
            RoleListOption.ImpConceal => [RoleAlignment.ImpostorConcealing],
            RoleListOption.ImpKilling => [RoleAlignment.ImpostorKilling],
            RoleListOption.ImpPower => [RoleAlignment.ImpostorPower],
            RoleListOption.ImpSupport => [RoleAlignment.ImpostorSupport],
            RoleListOption.ImpCommon => [RoleAlignment.ImpostorConcealing, RoleAlignment.ImpostorSupport],
            RoleListOption.ImpSpecial => [RoleAlignment.ImpostorKilling, RoleAlignment.ImpostorPower],
            RoleListOption.ImpRandom => [RoleAlignment.ImpostorConcealing, RoleAlignment.ImpostorKilling, RoleAlignment.ImpostorPower, RoleAlignment.ImpostorSupport],
            RoleListOption.NonImp => [RoleAlignment.CrewmateInvestigative, RoleAlignment.CrewmateKilling, RoleAlignment.CrewmateProtective, RoleAlignment.CrewmatePower, RoleAlignment.CrewmateSupport, RoleAlignment.NeutralBenign, RoleAlignment.NeutralEvil, RoleAlignment.NeutralKilling, RoleAlignment.NeutralOutlier],
            RoleListOption.Any => [.. GetAllDraftAlignments()],
            _ => []
        };
    }

    public static RoleBehaviour? PickRandomRole(bool isImpostor, List<RoleBehaviour>? offeredPool = null)
    {
        var freshOffer = SelectRolesToOffer(isImpostor);
        if (freshOffer != null && freshOffer.Count > 0)
        {
            return freshOffer[UnityEngine.Random.Range(0, freshOffer.Count)];
        }

        if (OptionGroupSingleton<DraftModeOptions>.Instance.PoolMode.Value != DraftPoolMode.OldDraft)
        {
            return null;
        }

        var myId = PlayerControl.LocalPlayer?.PlayerId ?? 255;

        DraftFaction faction;
        if (!PlayerFactions.TryGetValue(myId, out faction))
            faction = isImpostor ? DraftFaction.Impostor : DraftFaction.CrewOther;

        var enabledAlignments = GetAlignmentsForFaction(faction);

        // Fallback w razie braku puli ofert (np. błąd UI)
        var allRoles = faction == DraftFaction.Impostor
            ? FilterLonerForCurrentPicker(GetRolesForAlignments(enabledAlignments))
            : GetRolesForAlignments(enabledAlignments);
        if (allRoles.Count == 0) return null;

        return OrderRoles(allRoles).FirstOrDefault();
    }

    // === LIFECYCLE ===

    public static void Reset()
    {
        IsRunning = false;
        DraftComplete = false;
        DraftActiveThisRound = false;
        PickOrder.Clear();
        OriginalPickOrder.Clear();
        RoleListSlotOrder.Clear();
        AlreadyPicked.Clear();
        DraftPicks.Clear();
        ImpostorPlayerIds.Clear();
        PlayerFactions.Clear();
        LonerReducedImpostorSlot = false;
        LocalPlayerPicked = false;
        PickTimer = 0f;
        CurrentOfferedRoles = null;
        SelectedAlignment = null;
        InvalidateRoleCache();
    }

    public static void GeneratePickOrder(List<byte>? validPlayerIds = null)
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

        OriginalPickOrder.AddRange(PickOrder);
        GenerateRoleListSlotOrder();
    }

    private static void GenerateRoleListSlotOrder()
    {
        RoleListSlotOrder.Clear();
        var slotCount = Mathf.Clamp(PickOrder.Count, 0, MaxRoleListSlots);
        for (var i = 0; i < slotCount; i++)
        {
            RoleListSlotOrder.Add(i);
        }

        RoleListSlotOrder.Shuffle();
    }

    public static void RegisterPick(byte playerId, ushort roleId)
    {
        AlreadyPicked.Add(roleId);
        DraftPicks[playerId] = roleId;
        PickOrder.Remove(playerId);
        PickTimer = 0f;
        ApplyLonerImpostorSlotReduction(playerId, roleId);

        if (playerId == PlayerControl.LocalPlayer?.PlayerId)
        {
            LocalPlayerPicked = true;
            CurrentOfferedRoles = null;
            SelectedAlignment = null;
        }

        InvalidateRoleCache();
    }

    private static void ApplyLonerImpostorSlotReduction(byte playerId, ushort roleId)
    {
        if (LonerReducedImpostorSlot ||
            roleId != RoleId.Get<TouMegaChujoweExtension.Roles.Classic.Impostor.LonerRole>())
        {
            return;
        }

        byte? futureImpostor = PickOrder
            .Cast<byte?>()
            .FirstOrDefault(id =>
                id != null &&
                id != playerId &&
                PlayerFactions.TryGetValue(id.Value, out var faction) &&
                faction == DraftFaction.Impostor);

        if (!futureImpostor.HasValue)
        {
            return;
        }

        PlayerFactions[futureImpostor.Value] = DraftFaction.CrewOther;
        ImpostorPlayerIds.Remove(futureImpostor.Value);
        LonerReducedImpostorSlot = true;
        InvalidateRoleCache();
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













