using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Modifiers.Game.Crewmate;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles;

[HarmonyPatch]
public static class InnocentPatches
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
    [HarmonyPostfix]
    public static void ResetOnGameStart()
    {
        StripAllInnocentTargetMarkers();
        InnocentRole.ClearAndReload();
    }

    [RegisterEvent]
    public static void OnAfterMurder(AfterMurderEvent evt)
    {
        if (evt.Target == null || evt.Source == null) return;

        foreach (var marker in evt.Source.GetModifiers<InnocentTargetModifier>().ToArray())
        {
            if (!InnocentRole.ActiveInnocents.TryGetValue(marker.InnocentPlayerId, out var innocent)) continue;
            if (evt.Target.PlayerId == marker.InnocentPlayerId) continue;
            if (!IsValidForcedVictim(evt.Target)) continue;

            innocent.BeginTauntWinWindow(evt.Source.PlayerId);
        }

        if (evt.Target.HasModifier<BaitModifier>() && evt.Source.AmOwner && !MeetingHud.Instance)
        {
            evt.Source.CmdReportDeadBody(evt.Target.Data);
        }
    }

    [RegisterEvent]
    public static void OnEjection(EjectionEvent evt)
    {
        var exiled = evt.ExileController?.initData?.networkedPlayer?.Object;
        if (exiled == null) return;

        foreach (var innocent in GetInnocents())
        {
            if (!innocent.AwaitingNextMeetingExile || innocent.TauntedKillerId != exiled.PlayerId) continue;
            if (!KillerHasTauntMarkerForInnocent(exiled, innocent.Player.PlayerId)) continue;

            innocent.AboutToWin = true;
            innocent.AwaitingNextMeetingExile = false;
            innocent.TargetVoted = true;
            innocent.TransformWhenTauntResolved = false;
            RemoveInnocentTauntMarker(exiled.PlayerId, innocent.Player.PlayerId);
        }
    }

    [RegisterEvent]
    public static void OnPlayerDeath(PlayerDeathEvent evt)
    {
        if (evt.DeathReason != DeathReason.Exile) return;

        foreach (var innocent in GetInnocents())
        {
            if (innocent.TauntedKillerId == evt.Player.PlayerId && innocent.AboutToWin && !innocent.WinWindowExpired)
            {
                innocent.TargetVoted = true;
            }
        }
    }

    [RegisterEvent]
    public static void OnRoundStart(RoundStartEvent evt)
    {
        if (evt.TriggeredByIntro) return;

        foreach (var innocent in GetInnocents())
        {
            if (innocent.AboutToWin && innocent.TauntedKillerId.HasValue)
            {
                innocent.TargetVoted = true;
                innocent.TransformWhenTauntResolved = false;
                RemoveInnocentTauntMarker(innocent.TauntedKillerId.Value, innocent.Player.PlayerId);
            }
            else if (innocent.AwaitingNextMeetingExile)
            {
                if (innocent.TauntedKillerId.HasValue)
                {
                    RemoveInnocentTauntMarker(innocent.TauntedKillerId.Value, innocent.Player.PlayerId);
                }

                if (innocent.TransformWhenTauntResolved)
                {
                    InnocentRole.TryTransformAfterSpentTaunts(innocent.Player.PlayerId);
                }
                else
                {
                    innocent.ResetTauntState();
                    innocent.WinWindowExpired = true;
                }
            }
            else if (innocent.TransformWhenTauntResolved)
            {
                InnocentRole.TryTransformAfterSpentTaunts(innocent.Player.PlayerId);
            }
        }
    }

    private static bool IsValidForcedVictim(PlayerControl target) => target.IsCrewmate();

    private static IEnumerable<InnocentRole> GetInnocents() => InnocentRole.ActiveInnocents.Values;

    private static void StripAllInnocentTargetMarkers()
    {
        foreach (var player in PlayerControl.AllPlayerControls.ToArray())
        {
            if (player == null) continue;

            foreach (var marker in player.GetModifiers<InnocentTargetModifier>().ToArray())
            {
                player.RpcRemoveModifier(marker.UniqueId);
            }
        }
    }

    private static void RemoveInnocentTauntMarker(byte killerPlayerId, byte innocentPlayerId)
    {
        var killer = GameData.Instance?.GetPlayerById(killerPlayerId)?.Object;
        if (killer == null) return;

        foreach (var marker in killer.GetModifiers<InnocentTargetModifier>().ToArray())
        {
            if (marker.InnocentPlayerId == innocentPlayerId)
            {
                killer.RpcRemoveModifier(marker.UniqueId);
            }
        }
    }

    internal static bool KillerHasTauntMarkerForInnocent(PlayerControl killer, byte innocentPlayerId)
    {
        return killer.GetModifiers<InnocentTargetModifier>().Any(marker => marker.InnocentPlayerId == innocentPlayerId);
    }
}

internal static class InnocentTauntMeetingDisplay
{
    private const string TauntSymbol = "+";
    private static string? _tauntSymbolRichChunk;

    private static string TauntSymbolRichChunk =>
        _tauntSymbolRichChunk ??= $"<color=#{ColorUtility.ToHtmlStringRGBA(TouExtensionColors.Innocent)}> {TauntSymbol}</color>";

    internal static bool LocalShouldHighlightTauntTarget(PlayerControl row)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || row == null || local.Data == null) return false;

        foreach (var marker in row.GetModifiers<InnocentTargetModifier>())
        {
            if (!InnocentRole.ActiveInnocents.TryGetValue(marker.InnocentPlayerId, out var innocent)) continue;
            if (local.Data.IsDead) return true;
            if (local.PlayerId == marker.InnocentPlayerId) return true;
        }

        return false;
    }

    internal static void TryAppendTauntSymbol(ref string result, PlayerControl row)
    {
        if (!LocalShouldHighlightTauntTarget(row)) return;

        var chunk = TauntSymbolRichChunk;
        if (result.Contains(chunk)) return;

        result += chunk;
    }
}

[HarmonyPatch(typeof(PlayerRoleTextExtensions), nameof(PlayerRoleTextExtensions.UpdateTargetSymbols),
    new[] { typeof(string), typeof(PlayerControl), typeof(bool) })]
public static class InnocentTargetSymbolPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref string __result, PlayerControl player, bool hidden = false)
    {
        InnocentTauntMeetingDisplay.TryAppendTauntSymbol(ref __result, player);
    }
}

[HarmonyPatch(typeof(PlayerRoleTextExtensions), nameof(PlayerRoleTextExtensions.UpdateTargetSymbols),
    new[] { typeof(string), typeof(PlayerControl), typeof(DataVisibility) })]
public static class InnocentTargetSymbolDataVisibilityPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref string __result, PlayerControl player, DataVisibility visibility)
    {
        InnocentTauntMeetingDisplay.TryAppendTauntSymbol(ref __result, player);
    }
}

[HarmonyPatch(typeof(PlayerRoleTextExtensions), nameof(PlayerRoleTextExtensions.UpdateTargetColor),
    new[] { typeof(Color), typeof(PlayerControl), typeof(DataVisibility) })]
public static class InnocentTargetColorPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref Color __result, PlayerControl player, DataVisibility visibility)
    {
        if (InnocentTauntMeetingDisplay.LocalShouldHighlightTauntTarget(player))
        {
            __result = TouExtensionColors.Innocent;
        }
    }
}
