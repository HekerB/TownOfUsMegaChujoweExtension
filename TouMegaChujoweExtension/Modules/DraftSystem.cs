using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Options;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TouMegaChujoweExtension.Utilities;
using Random = UnityEngine.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using TownOfUs.Options;
using TownOfUs.Roles;
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
    public const int MaxRoleListSlots = 35;

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
    public static HashSet<byte> LastImpostorIds { get; } = new();
    public static HashSet<byte> LastNeutralKillingIds { get; } = new();
    public static bool DraftActiveThisRound { get; set; }
    public static List<RoleBehaviour>? CurrentOfferedRoles { get; set; }
    public static RoleAlignment? SelectedAlignment { get; set; }
    public static int TargetOtherNeutralCount { get; set; }
    public static bool LonerReducedImpostorSlot { get; set; }
    public static Dictionary<byte, DraftFaction> PlayerFactions { get; } = new();

    private static readonly Dictionary<RoleAlignment, List<RoleBehaviour>> _roleCache = new();
    private static bool _roleCacheDirty = true;

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

    private static bool IsOldDraftMode()
    {
        try
        {
            return OptionGroupSingleton<DraftModeOptions>.Instance.PoolMode.Value == DraftPoolMode.OldDraft;
        }
        catch
        {
            return true;
        }
    }

    private static int GetSafeCount(float min, float max)
    {
        if (min > max || Mathf.Approximately(min, max))
        {
            return Mathf.RoundToInt(min);
        }

        return Random.Range(0f, 100f) < 60f ? Mathf.RoundToInt(min) : Mathf.RoundToInt(max);
    }

    public static bool IsRoleListSlotImpostor(int slotIndex)
    {
        return IsImpostorBucket(GetRoleListBucketForSlot(slotIndex));
    }

    public static void AssignFactions(List<byte> allPlayerIds, HashSet<byte> impostorIds)
    {
        PlayerFactions.Clear();

        var options = OptionGroupSingleton<DraftModeOptions>.Instance;

        foreach (var id in impostorIds)
        {
            PlayerFactions[id] = DraftFaction.Impostor;
        }

        var remaining = allPlayerIds.Where(id => !impostorIds.Contains(id)).ToList();
        remaining.Shuffle();

        if (options == null)
        {
            UnityEngine.Debug.LogError("[TOUMCE] DraftModeOptions instance is null in AssignFactions!");
            return;
        }

        int neutralKillingCount;
        if (options.IsRoleListDraft)
        {
            var slotCount = Mathf.Min(allPlayerIds.Count, MaxRoleListSlots);
            neutralKillingCount = 0;
            for (var i = 0; i < slotCount; i++)
            {
                if (GetRoleListBucketForSlot(i) == DraftRoleListOption.NeutKilling)
                {
                    neutralKillingCount++;
                }
            }
            TargetOtherNeutralCount = slotCount - impostorIds.Count - neutralKillingCount;
        }
        else if (options.IsMinMaxDraft)
        {
            var neutralOptions = OptionGroupSingleton<DraftNeutralSettingsOptions>.Instance;
            neutralKillingCount = (int)neutralOptions.MaxNeutralKillingRoles.Value;
            TargetOtherNeutralCount = (int)neutralOptions.MaxNeutralTotal.Value - neutralKillingCount;
        }
        else
        {
            var oldOptions = OptionGroupSingleton<DraftOldSettingsOptions>.Instance;
            neutralKillingCount = GetSafeCount(oldOptions.MinNeutralKilling.Value, oldOptions.MaxNeutralKilling.Value);
            TargetOtherNeutralCount = GetSafeCount(oldOptions.MinOtherNeutrals.Value, oldOptions.MaxOtherNeutrals.Value);
        }

        if (neutralKillingCount + TargetOtherNeutralCount > remaining.Count)
        {
            var ratio = (float)remaining.Count / (neutralKillingCount + TargetOtherNeutralCount);
            neutralKillingCount = Mathf.FloorToInt(neutralKillingCount * ratio);
            TargetOtherNeutralCount = Mathf.FloorToInt(TargetOtherNeutralCount * ratio);
        }

        neutralKillingCount = Mathf.Min(neutralKillingCount, remaining.Count);

        var idx = 0;
        var nkReductionEnabled = options.ReduceKillingStreak.Value;
        var nkBiasPercent = options.ReductionChance.Value / 100f;
        var random = new System.Random();

        for (var i = 0; i < neutralKillingCount && remaining.Count > idx; i++)
        {
            var num = -1;

            if (nkReductionEnabled && LastNeutralKillingIds.Count > 0)
            {
                var subPool = remaining.Skip(idx).ToList();
                var nonRecentNk = subPool.Where(id => !LastNeutralKillingIds.Contains(id)).ToList();

                if (nonRecentNk.Count > 0 && random.NextDouble() < nkBiasPercent)
                {
                    var chosenId = nonRecentNk[random.Next(nonRecentNk.Count)];
                    num = remaining.IndexOf(chosenId);
                }
            }

            if (num == -1)
            {
                num = random.Next(idx, remaining.Count);
            }

            (remaining[idx], remaining[num]) = (remaining[num], remaining[idx]);
            PlayerFactions[remaining[idx]] = DraftFaction.NeutralKilling;
            idx++;
        }

        for (; idx < remaining.Count; idx++)
        {
            PlayerFactions[remaining[idx]] = DraftFaction.CrewOther;
        }
    }

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

    public static void InvalidateRoleCache()
    {
        _roleCacheDirty = true;
        _roleCache.Clear();
    }

    private static void EnsureRoleCacheBuilt()
    {
        if (!_roleCacheDirty && _roleCache.Count > 0)
        {
            return;
        }

        _roleCache.Clear();

        var generalOptions = OptionGroupSingleton<ExtensionGameMechanicOptions>.Instance;
        var preventVampires = false;
        var preventJackal = false;

        if (generalOptions != null && generalOptions.PreventVampiresWithJackal)
        {
            var jackalId = (ushort)RoleId.Get<TouMegaChujoweExtension.Roles.Classic.Neutral.JackalRole>();
            var vampireId = (ushort)RoleId.Get<TownOfUs.Roles.Neutral.VampireRole>();

            var jackalPicked = AlreadyPicked.Contains(jackalId);
            var vampirePicked = AlreadyPicked.Contains(vampireId);

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
                if (role.IsDead)
                {
                    continue;
                }

                if (!CustomRoleUtils.CanSpawnOnCurrentMode(role))
                {
                    continue;
                }

                if (roles.Any(r => r.Role == role.Role))
                {
                    continue;
                }

                var isJackalRole = role.Role == (RoleTypes)RoleId.Get<TouMegaChujoweExtension.Roles.Classic.Neutral.JackalRole>();
                var isVampireRole = role.Role == (RoleTypes)RoleId.Get<TownOfUs.Roles.Neutral.VampireRole>();

                /*
                if (AgentUtils.AgentCanSpawn() && IsAgentConflictRole(role.Role))
                {
                    continue;
                }
                */

                if (preventVampires && isVampireRole)
                {
                    continue;
                }

                if (preventJackal && isJackalRole)
                {
                    continue;
                }

                var assignData = MiscUtils.GetAssignData(role.Role);

                if (assignData.Count <= 0 || assignData.Chance <= 0)
                {
                    continue;
                }

                roles.Add(role);
            }

            _roleCache[alignment] = roles;
        }

        _roleCacheDirty = false;
    }

    private static List<RoleBehaviour> GetRolesForAlignment(RoleAlignment alignment)
    {
        EnsureRoleCacheBuilt();

        if (!_roleCache.TryGetValue(alignment, out var cached))
        {
            return new List<RoleBehaviour>();
        }

        return cached.Where(r =>
            (!AlreadyPicked.Contains((ushort)r.Role) && !ConflictsWithPickedAgent(r.Role)) ||
            r.Role == RoleTypes.Crewmate ||
            r.Role == RoleTypes.Impostor).ToList();
    }

    private static bool IsAgentConflictRole(RoleTypes roleType)
    {
        return roleType == (RoleTypes)RoleId.Get<TownOfUs.Roles.Impostor.TraitorRole>();
    }

    private static bool ConflictsWithPickedAgent(RoleTypes roleType)
    {
        /*
        if (!AlreadyPicked.Contains((ushort)RoleId.Get<AgentRole>()))
        {
            return false;
        }

        return IsAgentConflictRole(roleType);
        */
        return false;
    }

    private static List<RoleBehaviour> GetRolesForAlignments(List<RoleAlignment> alignments)
    {
        var result = new List<RoleBehaviour>();

        foreach (var alignment in alignments)
        {
            foreach (var role in GetRolesForAlignment(alignment))
            {
                if (result.Any(r => r.Role == role.Role))
                {
                    continue;
                }

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
            if (player?.Data == null ||
                player.Data.Disconnected ||
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

    private static IEnumerable<T> WeightedShuffle<T>(IEnumerable<T> items, Func<T, float> weightSelector)
    {
        var pool = items
            .Select(item => (Item: item, Weight: weightSelector(item)))
            .ToList();
        var result = new List<T>();

        while (pool.Count > 0)
        {
            var totalWeight = pool.Sum(entry => entry.Weight);

            if (totalWeight <= 0)
            {
                for (var i = pool.Count - 1; i > 0; i--)
                {
                    var j = Random.Range(0, i + 1);
                    (pool[i], pool[j]) = (pool[j], pool[i]);
                }

                result.AddRange(pool.Select(entry => entry.Item));
                break;
            }

            var r = Random.Range(0f, totalWeight);
            var current = 0f;
            var selected = pool[0];

            foreach (var item in pool)
            {
                current += item.Weight;

                if (r <= current)
                {
                    selected = item;
                    break;
                }
            }

            result.Add(selected.Item);
            pool.Remove(selected);
        }

        return result;
    }

    private static IEnumerable<RoleBehaviour> OrderRoles(IEnumerable<RoleBehaviour> roles)
    {
        bool respectChances;

        try
        {
            respectChances = OptionGroupSingleton<DraftModeOptions>.Instance.RespectRoleChances.Value;
        }
        catch
        {
            respectChances = false;
        }

        if (!respectChances)
        {
            return roles.OrderBy(_ => Random.Range(0f, 1f));
        }

        return WeightedShuffle(roles, role =>
        {
            var chance = (int)MiscUtils.GetAssignData(role.Role).Chance;
            return Mathf.Clamp(chance, 1, 100);
        });
    }

    private static int GetCurrentOtherNeutralCount()
    {
        var count = 0;

        foreach (var roleId in DraftPicks.Values)
        {
            if (IsOtherNeutral((RoleTypes)roleId))
            {
                count++;
            }
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
        {
            faction = assignedFaction;
        }
        else if (isImpostor)
        {
            faction = DraftFaction.Impostor;
        }
        else
        {
            faction = DraftFaction.CrewOther;
        }

        var enabledAlignments = GetAlignmentsForFaction(faction);

        if (enabledAlignments.Count == 0)
        {
            return new List<RoleBehaviour>();
        }

        var roleCount = RolesToShow;

        if (faction == DraftFaction.CrewOther)
        {
            var oldOptions = OptionGroupSingleton<DraftOldSettingsOptions>.Instance;
            var crewPool = GetRolesForAlignments(enabledAlignments);

            var currentNeutrals = GetCurrentOtherNeutralCount();
            var remainingEligiblePlayers = PickOrder.Count(id => PlayerFactions.TryGetValue(id, out var f) && f == DraftFaction.CrewOther);
            var neutralsNeeded = Mathf.Max(0, TargetOtherNeutralCount - currentNeutrals);
            var forceNeutrals = neutralsNeeded >= remainingEligiblePlayers && remainingEligiblePlayers > 0;
            var limitReached = currentNeutrals >= TargetOtherNeutralCount;
            var wantedNeutrals = 0;

            if (!limitReached)
            {
                if (forceNeutrals)
                {
                    wantedNeutrals = roleCount;
                }
                else
                {
                    var offerChance = remainingEligiblePlayers <= 0 ? 0f : (float)neutralsNeeded / remainingEligiblePlayers;

                    if (Random.Range(0f, 1f) < offerChance)
                    {
                        wantedNeutrals = Random.Range(0f, 100f) < 60f
                            ? (int)oldOptions.MinOtherNeutralsPerChoice.Value
                            : (int)oldOptions.MaxOtherNeutralsPerChoice.Value;

                        wantedNeutrals = Mathf.Min(wantedNeutrals, neutralsNeeded);
                    }
                }
            }

            wantedNeutrals = Mathf.Min(wantedNeutrals, roleCount);

            var finalPool = new List<RoleBehaviour>();

            if (wantedNeutrals > 0)
            {
                var neutralPool = new List<RoleAlignment>
                {
                    RoleAlignment.NeutralBenign,
                    RoleAlignment.NeutralEvil,
                    RoleAlignment.NeutralOutlier
                };

                var allNeutrals = OrderRoles(GetRolesForAlignments(neutralPool)).ToList();
                finalPool.AddRange(allNeutrals.Take(wantedNeutrals));
            }

            if (finalPool.Count < roleCount)
            {
                finalPool.AddRange(OrderRoles(crewPool).Take(roleCount - finalPool.Count));
            }

            return finalPool.OrderBy(_ => Random.Range(0f, 1f)).ToList();
        }

        var allRoles = faction == DraftFaction.Impostor
            ? FilterLonerForCurrentPicker(GetRolesForAlignments(enabledAlignments))
            : GetRolesForAlignments(enabledAlignments);

        if (allRoles.Count == 0)
        {
            return new List<RoleBehaviour>();
        }

        SelectedAlignment = null;

        if (allRoles.Count <= roleCount)
        {
            return allRoles.OrderBy(_ => Random.Range(0f, 1f)).ToList();
        }

        return OrderRoles(allRoles)
            .Take(roleCount)
            .OrderBy(_ => Random.Range(0f, 1f))
            .ToList();
    }

    private static List<RoleBehaviour> SelectMinMaxRolesToOffer()
    {
        var roleCount = RolesToShow;
        var myId = PlayerControl.LocalPlayer?.PlayerId ?? 255;
        var pickerId = CurrentPicker ?? myId;

        var faction = DraftFaction.CrewOther;
        PlayerFactions.TryGetValue(pickerId, out faction);

        var enabledAlignments = GetAlignmentsForFaction(faction);

        var availableAlignments = enabledAlignments
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
            .Where(role => role.Role != lonerRole || impostorsPicked == 0 && impostorSlotsRemaining >= 2)
            .ToList();
    }

    private static List<RoleBehaviour> FilterLonerForRoleList(IEnumerable<RoleBehaviour> roles)
    {
        var lonerRole = (RoleTypes)RoleId.Get<TouMegaChujoweExtension.Roles.Classic.Impostor.LonerRole>();
        var previousImpostorPicked = DraftPicks.Values.Any(IsImpostorRole);
        var futureImpostorSlots = GetFutureRoleListBuckets().Count(IsImpostorBucket);

        return roles
            .Where(role => role.Role != lonerRole || !previousImpostorPicked && futureImpostorSlots >= 1)
            .ToList();
    }

    private static DraftRoleListOption GetCurrentRoleListBucket()
    {
        EnsureRoleListSlotOrder();

        var picker = CurrentPicker ?? PlayerControl.LocalPlayer?.PlayerId ?? byte.MaxValue;
        var slotIndex = OriginalPickOrder.IndexOf(picker);

        if (slotIndex < 0)
        {
            slotIndex = Mathf.Clamp(DraftPicks.Count, 0, MaxRoleListSlots - 1);
        }

        var bucket = GetRoleListBucketForPickIndex(slotIndex);

        if (PlayerFactions.TryGetValue(picker, out var faction) &&
            faction == DraftFaction.CrewOther &&
            IsImpostorBucket(bucket))
        {
            return DraftRoleListOption.CrewRandom;
        }

        return bucket;
    }

    private static IEnumerable<DraftRoleListOption> GetFutureRoleListBuckets()
    {
        EnsureRoleListSlotOrder();

        var currentPicker = CurrentPicker;
        var currentIndex = currentPicker.HasValue ? OriginalPickOrder.IndexOf(currentPicker.Value) : -1;

        if (currentIndex < 0)
        {
            currentIndex = DraftPicks.Count;
        }

        for (var i = currentIndex + 1; i < OriginalPickOrder.Count && i < MaxRoleListSlots; i++)
        {
            yield return GetRoleListBucketForPickIndex(i);
        }
    }

    private static void EnsureRoleListSlotOrder()
    {
        var slotCount = GetRoleListSlotOrderCount();

        if (slotCount <= 0)
        {
            return;
        }

        if (RoleListSlotOrder.Count == slotCount)
        {
            return;
        }

        RoleListSlotOrder.Clear();

        for (var i = 0; i < slotCount; i++)
        {
            RoleListSlotOrder.Add(i);
        }

        RoleListSlotOrder.Shuffle();
    }

    private static int GetRoleListSlotOrderCount()
    {
        if (OriginalPickOrder.Count > 0)
        {
            return Mathf.Clamp(OriginalPickOrder.Count, 1, MaxRoleListSlots);
        }

        if (PickOrder.Count > 0)
        {
            return Mathf.Clamp(PickOrder.Count, 1, MaxRoleListSlots);
        }

        return GetVisibleRoleListSlotCount();
    }

    public static DraftRoleListOption GetRoleListBucketForPickIndex(int zeroBasedPickIndex)
    {
        EnsureRoleListSlotOrder();

        var slotIndex = zeroBasedPickIndex;

        if (zeroBasedPickIndex >= 0 && zeroBasedPickIndex < RoleListSlotOrder.Count)
        {
            slotIndex = RoleListSlotOrder[zeroBasedPickIndex];
        }

        return GetRoleListBucketForSlot(slotIndex);
    }

    private static DraftRoleListOption GetRoleListBucketForSlot(int zeroBasedSlot)
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
            20 => options.Slot21.Value,
            21 => options.Slot22.Value,
            22 => options.Slot23.Value,
            23 => options.Slot24.Value,
            24 => options.Slot25.Value,
            25 => options.Slot26.Value,
            26 => options.Slot27.Value,
            27 => options.Slot28.Value,
            28 => options.Slot29.Value,
            29 => options.Slot30.Value,
            30 => options.Slot31.Value,
            31 => options.Slot32.Value,
            32 => options.Slot33.Value,
            33 => options.Slot34.Value,
            34 => options.Slot35.Value,
            _ => DraftRoleListOption.NonImp
        };
    }

    private static bool IsImpostorBucket(DraftRoleListOption bucket)
    {
        return bucket is DraftRoleListOption.ImpConceal or
            DraftRoleListOption.ImpKilling or
            DraftRoleListOption.ImpPower or
            DraftRoleListOption.ImpSupport or
            DraftRoleListOption.ImpCommon or
            DraftRoleListOption.ImpSpecial or
            DraftRoleListOption.ImpRandom;
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

    private static List<RoleAlignment> GetAlignmentsForRoleListBucket(DraftRoleListOption bucket)
    {
        return bucket switch
        {
            DraftRoleListOption.CrewInvest => [RoleAlignment.CrewmateInvestigative],
            DraftRoleListOption.CrewKilling => [RoleAlignment.CrewmateKilling],
            DraftRoleListOption.CrewProtective => [RoleAlignment.CrewmateProtective],
            DraftRoleListOption.CrewPower => [RoleAlignment.CrewmatePower],
            DraftRoleListOption.CrewSupport => [RoleAlignment.CrewmateSupport],
            DraftRoleListOption.CrewCommon => [RoleAlignment.CrewmateInvestigative, RoleAlignment.CrewmateProtective, RoleAlignment.CrewmateSupport],
            DraftRoleListOption.CrewSpecial => [RoleAlignment.CrewmateKilling, RoleAlignment.CrewmatePower],
            DraftRoleListOption.CrewRandom => [RoleAlignment.CrewmateInvestigative, RoleAlignment.CrewmateKilling, RoleAlignment.CrewmateProtective, RoleAlignment.CrewmatePower, RoleAlignment.CrewmateSupport],
            DraftRoleListOption.CrewNeu => [RoleAlignment.CrewmateInvestigative, RoleAlignment.CrewmateKilling, RoleAlignment.CrewmateProtective, RoleAlignment.CrewmatePower, RoleAlignment.CrewmateSupport, RoleAlignment.NeutralBenign, RoleAlignment.NeutralEvil, RoleAlignment.NeutralOutlier],

            DraftRoleListOption.NeutBenign => [RoleAlignment.NeutralBenign],
            DraftRoleListOption.NeutEvil => [RoleAlignment.NeutralEvil],
            DraftRoleListOption.NeutKilling => [RoleAlignment.NeutralKilling],
            DraftRoleListOption.NeutOutlier => [RoleAlignment.NeutralOutlier],
            DraftRoleListOption.NeutCommon => [RoleAlignment.NeutralBenign, RoleAlignment.NeutralEvil],
            DraftRoleListOption.NeutSpecial => [RoleAlignment.NeutralKilling, RoleAlignment.NeutralOutlier],
            DraftRoleListOption.NeutWildcard => [RoleAlignment.NeutralBenign, RoleAlignment.NeutralEvil, RoleAlignment.NeutralOutlier],
            DraftRoleListOption.NeutRandom => [RoleAlignment.NeutralBenign, RoleAlignment.NeutralEvil, RoleAlignment.NeutralKilling, RoleAlignment.NeutralOutlier],

            DraftRoleListOption.ImpConceal => [RoleAlignment.ImpostorConcealing],
            DraftRoleListOption.ImpKilling => [RoleAlignment.ImpostorKilling],
            DraftRoleListOption.ImpPower => [RoleAlignment.ImpostorPower],
            DraftRoleListOption.ImpSupport => [RoleAlignment.ImpostorSupport],
            DraftRoleListOption.ImpCommon => [RoleAlignment.ImpostorConcealing, RoleAlignment.ImpostorSupport],
            DraftRoleListOption.ImpSpecial => [RoleAlignment.ImpostorKilling, RoleAlignment.ImpostorPower],
            DraftRoleListOption.ImpRandom => [RoleAlignment.ImpostorConcealing, RoleAlignment.ImpostorKilling, RoleAlignment.ImpostorPower, RoleAlignment.ImpostorSupport],

            DraftRoleListOption.NonImp => [RoleAlignment.CrewmateInvestigative, RoleAlignment.CrewmateKilling, RoleAlignment.CrewmateProtective, RoleAlignment.CrewmatePower, RoleAlignment.CrewmateSupport, RoleAlignment.NeutralBenign, RoleAlignment.NeutralEvil, RoleAlignment.NeutralKilling, RoleAlignment.NeutralOutlier],
            DraftRoleListOption.Any => [.. GetAllDraftAlignments()],
            _ => []
        };
    }

    public static RoleBehaviour? PickRandomRole(bool isImpostor, List<RoleBehaviour>? offeredPool = null)
    {
        var freshOffer = SelectRolesToOffer(isImpostor);

        if (freshOffer != null && freshOffer.Count > 0)
        {
            return freshOffer[Random.Range(0, freshOffer.Count)];
        }

        if (OptionGroupSingleton<DraftModeOptions>.Instance.PoolMode.Value != DraftPoolMode.OldDraft)
        {
            return null;
        }

        var myId = PlayerControl.LocalPlayer?.PlayerId ?? 255;
        DraftFaction faction;

        if (!PlayerFactions.TryGetValue(myId, out faction))
        {
            faction = isImpostor ? DraftFaction.Impostor : DraftFaction.CrewOther;
        }

        var enabledAlignments = GetAlignmentsForFaction(faction);

        var allRoles = faction == DraftFaction.Impostor
            ? FilterLonerForCurrentPicker(GetRolesForAlignments(enabledAlignments))
            : GetRolesForAlignments(enabledAlignments);

        if (allRoles.Count == 0)
        {
            return null;
        }

        return OrderRoles(allRoles).FirstOrDefault();
    }

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
        OriginalPickOrder.Clear();
        RoleListSlotOrder.Clear();

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
                if (player != null &&
                    player.Data != null &&
                    !player.Data.Disconnected &&
                    !TownOfUs.Roles.Other.SpectatorRole.TrackedSpectators.Contains(player.Data.PlayerName))
                {
                    players.Add(player.PlayerId);
                }
            }
        }

        if (players.Count == 0)
        {
            return;
        }

        if (IsOldDraftMode())
        {
            players.Shuffle();
            PickOrder.AddRange(players);
            OriginalPickOrder.AddRange(PickOrder);
            return;
        }

        var specialPlayers = players.Where(id =>
            PlayerFactions.ContainsKey(id) &&
            PlayerFactions[id] != DraftFaction.CrewOther).ToList();

        var crewPlayers = players.Where(id =>
            !PlayerFactions.ContainsKey(id) ||
            PlayerFactions[id] == DraftFaction.CrewOther).ToList();

        specialPlayers.Shuffle();
        crewPlayers.Shuffle();

        var count = players.Count;
        var bucketSize = count / 3;
        var remainder = count % 3;

        var bucketSizes = new int[3];
        bucketSizes[0] = bucketSize + (remainder > 0 ? 1 : 0);
        bucketSizes[1] = bucketSize + (remainder > 1 ? 1 : 0);
        bucketSizes[2] = bucketSize;

        var buckets = new List<byte>[3] { new(), new(), new() };

        var specialIdx = 0;
        var bIdx = 0;

        while (specialIdx < specialPlayers.Count)
        {
            buckets[bIdx].Add(specialPlayers[specialIdx++]);
            bIdx = (bIdx + 1) % 3;
        }

        var crewIdx = 0;

        for (var i = 0; i < 3; i++)
        {
            while (buckets[i].Count < bucketSizes[i] && crewIdx < crewPlayers.Count)
            {
                buckets[i].Add(crewPlayers[crewIdx++]);
            }
        }

        for (var i = 0; i < 3; i++)
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

        var count = OriginalPickOrder.Count;
        if (count == 0)
        {
            count = PickOrder.Count;
        }
        if (count == 0)
        {
            count = GetVisibleRoleListSlotCount();
        }

        var slotCount = Mathf.Clamp(count, 0, MaxRoleListSlots);

        // Categorize all slot indices
        var impSlots = new List<int>();
        var nkSlots = new List<int>();
        var crewSlots = new List<int>();

        for (var i = 0; i < slotCount; i++)
        {
            var bucket = GetRoleListBucketForSlot(i);
            if (IsImpostorBucket(bucket))
            {
                impSlots.Add(i);
            }
            else if (bucket == DraftRoleListOption.NeutKilling)
            {
                nkSlots.Add(i);
            }
            else
            {
                crewSlots.Add(i);
            }
        }

        impSlots.Shuffle();
        nkSlots.Shuffle();
        crewSlots.Shuffle();

        // Initialize RoleListSlotOrder with dummy values first
        for (var i = 0; i < slotCount; i++)
        {
            RoleListSlotOrder.Add(-1);
        }

        // Assign slots to players in OriginalPickOrder based on their faction
        var listToUse = OriginalPickOrder.Count > 0 ? OriginalPickOrder : PickOrder;

        for (var i = 0; i < slotCount; i++)
        {
            if (i >= listToUse.Count)
            {
                break;
            }

            var playerId = listToUse[i];
            var faction = DraftFaction.CrewOther;
            PlayerFactions.TryGetValue(playerId, out faction);

            int assignedSlot = -1;

            if (faction == DraftFaction.Impostor && impSlots.Count > 0)
            {
                assignedSlot = impSlots[0];
                impSlots.RemoveAt(0);
            }
            else if (faction == DraftFaction.NeutralKilling && nkSlots.Count > 0)
            {
                assignedSlot = nkSlots[0];
                nkSlots.RemoveAt(0);
            }
            else if (crewSlots.Count > 0)
            {
                assignedSlot = crewSlots[0];
                crewSlots.RemoveAt(0);
            }
            else
            {
                // Fallback to any remaining slot
                if (impSlots.Count > 0)
                {
                    assignedSlot = impSlots[0];
                    impSlots.RemoveAt(0);
                }
                else if (nkSlots.Count > 0)
                {
                    assignedSlot = nkSlots[0];
                    nkSlots.RemoveAt(0);
                }
            }

            RoleListSlotOrder[i] = assignedSlot;
        }

        // Fill any remaining unassigned slots
        var allRemaining = new List<int>();
        allRemaining.AddRange(impSlots);
        allRemaining.AddRange(nkSlots);
        allRemaining.AddRange(crewSlots);
        allRemaining.Shuffle();

        for (var i = 0; i < slotCount; i++)
        {
            if (RoleListSlotOrder[i] == -1 && allRemaining.Count > 0)
            {
                RoleListSlotOrder[i] = allRemaining[0];
                allRemaining.RemoveAt(0);
            }
        }
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
            if (PlayerControl.LocalPlayer == null)
            {
                return -1;
            }

            return PickOrder.IndexOf(PlayerControl.LocalPlayer.PlayerId);
        }
    }

    public static void Shuffle<T>(this List<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
