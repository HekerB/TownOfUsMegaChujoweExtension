using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs.Extensions;
using TownOfUs.Patches;
using TownOfUs.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Reactor.Utilities;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Networking;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Jackal;

[HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
public static class JackalLobbyPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        JackalStartPatch.Reset();
        JackalStartPatch.StartTime = Time.time;
        JackalVampireExclusionState.Reset();
        JackalMechanicsPatch.ResetShieldState();
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class JackalGameEndPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        JackalStartPatch.Reset();
        JackalMechanicsPatch.ResetShieldState();
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
public static class JackalShipStartPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        JackalStartPatch.Reset();
    }
}

[HarmonyPatch(typeof(TouRoleManagerPatches), "AssignRoles")]
[HarmonyPatch(typeof(TouRoleManagerPatches), "AssignRolesFromRoleList")]
public static class JackalStartPatch
{
    internal static Dictionary<byte, byte> PendingAssignments { get; } = [];
    internal static bool WasExecuted { get; set; }
    internal static float StartTime { get; set; } = -1f;
    public static float TimeSinceStart => StartTime > 0 ? Time.time - StartTime : 0;

    [HarmonyPostfix]
    public static void Postfix()
    {
        UnityEngine.Debug.Log("[TOUMCE] AssignRoles Postfix triggered.");
        if (!AmongUsClient.Instance.AmHost)
        {
            UnityEngine.Debug.Log("[TOUMCE] Not host, skipping assignment.");
            return;
        }
        if (WasExecuted)
        {
            UnityEngine.Debug.Log("[TOUMCE] Already executed, skipping.");
            return;
        }
        Coroutines.Start(DelayedAssignment());
    }

    public static void ExecuteAssignment()
    {
        if (!AmongUsClient.Instance.AmHost || WasExecuted) return;
        Coroutines.Start(DelayedAssignment());
    }

    private static IEnumerator DelayedAssignment()
    {
        if (WasExecuted) yield break;
        WasExecuted = true;
        UnityEngine.Debug.Log("[TOUMCE] Starting sidekick assignment...");

        yield return new WaitForSeconds(3f);

        var jackals = PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && !p.Data.IsDead &&
                        (p.GetRole<JackalRole>() != null || p.Data.Role is JackalRole))
            .OrderBy(p => p.PlayerId)
            .ToList();

        UnityEngine.Debug.Log($"[TOUMCE] Found {jackals.Count} Jackals out of {PlayerControl.AllPlayerControls.Count} players.");

        if (jackals.Count == 0)
        {
            UnityEngine.Debug.Log("[TOUMCE] No Jackals found, ending assignment.");
            yield break;
        }

        HashSet<byte> alreadyAssigned = [];

        foreach (var jackal in jackals)
        {
            UnityEngine.Debug.Log($"[TOUMCE] Picking sidekicks for Jackal {jackal.Data.PlayerName} (ID: {jackal.PlayerId})");
            var selected = GetSidekicksForJackal(jackal, alreadyAssigned);
            UnityEngine.Debug.Log($"[TOUMCE] Selected {selected.Count} sidekicks for Jackal {jackal.PlayerId}");
            foreach (var sidekick in selected)
            {
                if (alreadyAssigned.Contains(sidekick.PlayerId)) continue;

                alreadyAssigned.Add(sidekick.PlayerId);
                PendingAssignments[sidekick.PlayerId] = jackal.PlayerId;

                if (!sidekick.HasModifier<SidekickModifier>())
                {
                    sidekick.RpcAddModifier<SidekickModifier>(jackal.PlayerId);
                    UnityEngine.Debug.Log($"[TOUMCE] Assigned sidekick {sidekick.Data.PlayerName} (ID: {sidekick.PlayerId}) to Jackal {jackal.Data.PlayerName} (ID: {jackal.PlayerId})");
                }
            }
        }
        UnityEngine.Debug.Log($"[TOUMCE] Finished sidekick assignment. Total assigned: {alreadyAssigned.Count}");

        if (PendingAssignments.Count > 0)
        {
            var victims = PendingAssignments.Keys.ToArray();
            var jackalIds = PendingAssignments.Values.ToArray();
            JackalRole.RpcSetSidekickAssignments(PlayerControl.LocalPlayer, victims, jackalIds);
        }
    }

    public static void Reset()
    {
        WasExecuted = false;
        PendingAssignments.Clear();
        SidekickMeetingNotificationPatch.ResetNotification();

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Pointer == System.IntPtr.Zero) continue;
            try
            {
                if (player.HasModifier<SidekickModifier>())
                {
                    player.RemoveModifier<SidekickModifier>();
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[TOUMCE] Error removing SidekickModifier: {ex}");
            }
        }
    }

    public static List<PlayerControl> GetSidekicksForJackal(PlayerControl jackal, HashSet<byte> excludeIds)
    {
        var candidates = PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && !p.Data.IsDead
                        && p.PlayerId != jackal.PlayerId
                        && p.GetRole<JackalRole>() == null
                        && p.Data.Role is not JackalRole
                        && !p.HasModifier<LoverModifier>()
                        && !p.HasModifier<EgotistModifier>()
                        && !p.HasModifier<CrewpostorModifier>()
                        && !excludeIds.Contains(p.PlayerId))
            .ToList();

        if (candidates.Count == 0) return [];

        var rng = new System.Random();

        var impostors = candidates.Where(p => p.Data != null && p.Data.Role != null && p.Data.Role.IsImpostor).OrderBy(p => p.PlayerId).ToList();
        var crewmates = candidates.Where(p => p.IsCrewmate()).OrderBy(p => p.PlayerId).ToList();

        List<PlayerControl> selected = [];

        if (impostors.Count > 0)
        {
            selected.Add(impostors[rng.Next(impostors.Count)]);
        }
        if (crewmates.Count > 0)
        {
            selected.Add(crewmates[rng.Next(crewmates.Count)]);
        }

        return selected;
    }
}
