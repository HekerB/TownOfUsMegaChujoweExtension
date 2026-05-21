using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Modifiers;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
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

    [RegisterEvent(1000)]
    public static void OnBeforeMurderLate(BeforeMurderEvent evt)
    {
        if (!evt.IsCancelled || evt.Target == null || evt.Source == null) return;

        if (InnocentRole.ActiveInnocents.TryGetValue(evt.Target.PlayerId, out var innocent) &&
            innocent.PendingTauntKillerId == evt.Source.PlayerId)
        {
            innocent.PendingTauntKillerId = null;
        }
    }

    [RegisterEvent]
    public static void OnAfterMurder(AfterMurderEvent evt)
    {
        if (evt.Target == null || evt.Source == null ||
            !InnocentRole.ActiveInnocents.TryGetValue(evt.Target.PlayerId, out var innocent))
        {
            return;
        }

        if (innocent.PendingTauntKillerId != evt.Source.PlayerId) return;

        innocent.PendingTauntKillerId = null;
        innocent.TauntedKillerId = evt.Source.PlayerId;
        innocent.TargetVoted = false;
        innocent.AboutToWin = false;
        innocent.AwaitingNextMeetingExile = true;
        innocent.WinWindowExpired = false;

        GiveKillerTauntMarker(evt.Source, evt.Target.PlayerId);

        // Force Report option
        if (OptionGroupSingleton<InnocentOptions>.Instance.ForceReport)
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

            if (!InnocentTauntMeetingDisplay.KillerHasTauntMarkerForInnocent(exiled, innocent.Player.PlayerId)) continue;

            innocent.AboutToWin = true;
            innocent.AwaitingNextMeetingExile = false;
            innocent.TargetVoted = true;
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
                RemoveInnocentTauntMarker(innocent.TauntedKillerId.Value, innocent.Player.PlayerId);
            }
            else if (innocent.AwaitingNextMeetingExile)
            {
                if (innocent.TauntedKillerId.HasValue)
                {
                    RemoveInnocentTauntMarker(innocent.TauntedKillerId.Value, innocent.Player.PlayerId);
                }

                innocent.AwaitingNextMeetingExile = false;
                innocent.WinWindowExpired = true;
                innocent.TauntedKillerId = null;
            }
        }
    }

    private static IEnumerable<InnocentRole> GetInnocents() => InnocentRole.ActiveInnocents.Values;

    private static void StripAllInnocentTargetMarkers()
    {
        foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
        {
            if (pc == null) continue;
            var comp = pc.GetModifierComponent();
            if (comp == null) continue;

            foreach (var m in pc.GetModifiers<InnocentTargetModifier>().ToArray())
            {
                comp.RemoveModifier(m);
            }
        }
    }

    private static void GiveKillerTauntMarker(PlayerControl killer, byte innocentPlayerId)
    {
        var comp = killer.GetModifierComponent();
        if (comp == null) return;

        foreach (var existing in killer.GetModifiers<InnocentTargetModifier>().ToArray())
        {
            comp.RemoveModifier(existing);
        }

        killer.AddModifier<InnocentTargetModifier>(innocentPlayerId);
    }

    private static void RemoveInnocentTauntMarker(byte killerPlayerId, byte innocentPlayerId)
    {
        var killer = GameData.Instance?.GetPlayerById(killerPlayerId)?.Object;
        if (killer == null) return;

        var comp = killer.GetModifierComponent();
        if (comp == null) return;

        foreach (var m in killer.GetModifiers<InnocentTargetModifier>().ToArray())
        {
            if (m.InnocentPlayerId == innocentPlayerId)
            {
                comp.RemoveModifier(m);
            }
        }
    }
}

internal static class InnocentTauntMeetingDisplay
{
    private const string TauntSymbol = "⊕";
    private static string? _tauntSymbolRichChunk;

    private static string TauntSymbolRichChunk =>
        _tauntSymbolRichChunk ??= $"<color=#{ColorUtility.ToHtmlStringRGBA(TouExtensionColors.Innocent)}> {TauntSymbol}</color>";

    internal static bool LocalShouldHighlightTauntTarget(PlayerControl row)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || row == null || local.Data == null) return false;

        foreach (var mod in row.GetModifiers<InnocentTargetModifier>())
        {
            if (!InnocentRole.ActiveInnocents.TryGetValue(mod.InnocentPlayerId, out var innocent)) continue;
            if (innocent.TauntedKillerId != row.PlayerId) continue;
            if (local.Data.IsDead) return true;
            if (local.PlayerId == mod.InnocentPlayerId) return true;
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

    internal static bool KillerHasTauntMarkerForInnocent(PlayerControl killer, byte innocentPlayerId)
    {
        return killer.GetModifiers<InnocentTargetModifier>().Any(m => m.InnocentPlayerId == innocentPlayerId);
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
