using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Modifiers;
using MiraAPI.Hud;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Game.Crewmate;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Modules.Localization;
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

            if (evt.Source.AmOwner && !MeetingHud.Instance)
            {
                evt.Source.CmdReportDeadBody(evt.Target.Data);
            }
        }

        if (evt.Target.HasModifier<BaitModifier>() && evt.Source.AmOwner && !MeetingHud.Instance)
        {
            evt.Source.CmdReportDeadBody(evt.Target.Data);
        }
    }

    [RegisterEvent]
    public static void OnMeetingStart(StartMeetingEvent evt)
    {
        foreach (var innocent in GetInnocents())
        {
            if (innocent.Player.AmOwner && !innocent.AwaitingNextMeetingExile && innocent.TauntedKillerId == null)
            {
                InnocentTauntButton.ClearExistingMarkerForInnocent(innocent.Player.PlayerId);
                innocent.ResetTauntState();

                if (innocent.TransformWhenTauntResolved)
                {
                    innocent.WinWindowExpired = true;
                    InnocentRole.TryTransformAfterSpentTaunts(innocent.Player.PlayerId);
                }
            }

            if (!innocent.Player.AmOwner ||
                string.IsNullOrEmpty(innocent.PendingMeetingAlertKey) ||
                string.IsNullOrEmpty(innocent.PendingMeetingAlertFallback))
            {
                continue;
            }

            MiraAPI.Utilities.Helpers.CreateAndShowNotification(
                $"<b>{TouExtensionColors.Innocent.ToTextColor()}{TouLocale.Get(innocent.PendingMeetingAlertKey, innocent.PendingMeetingAlertFallback)}</color></b>",
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.InnocentRoleIcon.LoadAsset())?.AdjustNotification();

            innocent.PendingMeetingAlertKey = null;
            innocent.PendingMeetingAlertFallback = null;
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

            if (OptionGroupSingleton<InnocentOptions>.Instance.AfterWin.Value == (int)InnocentAfterWin.Haunt)
            {
                PrepareHauntTargets(innocent, exiled);
            }

            ShowWinNotification(innocent);
            innocent.LastTauntVoters.Clear();
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
        foreach (var innocent in GetInnocents())
        {
            innocent.HasTauntedThisRound = false;
        }

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
            else if (innocent.TransformWhenTauntResolved && innocent.WinWindowExpired)
            {
                InnocentRole.TryTransformAfterSpentTaunts(innocent.Player.PlayerId);
            }
        }

        if (!evt.TriggeredByIntro)
        {
            InnocentHauntButton.ClearRoundHaunts();
        }
    }

    [RegisterEvent]
    public static void OnHandleVote(MiraAPI.Events.Vanilla.Meeting.Voting.HandleVoteEvent evt)
    {
        var suspect = evt.TargetPlayerInfo?.Object;
        if (suspect == null)
        {
            return;
        }

        foreach (var marker in suspect.GetModifiers<InnocentTargetModifier>())
        {
            if (InnocentRole.ActiveInnocents.TryGetValue(marker.InnocentPlayerId, out var innocent) &&
                innocent.AwaitingNextMeetingExile &&
                innocent.TauntedKillerId == suspect.PlayerId)
            {
                innocent.LastTauntVoters.Add(evt.Player.PlayerId);
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

    private static void PrepareHauntTargets(InnocentRole innocent, PlayerControl exiled)
    {
        if (!innocent.Player.AmOwner)
        {
            return;
        }

        var voters = PlayerControl.AllPlayerControls.ToArray()
            .Where(player => player != null &&
                             !player.HasDied() &&
                             player.PlayerId != innocent.Player.PlayerId &&
                             player.PlayerId != exiled.PlayerId &&
                             innocent.LastTauntVoters.Contains(player.PlayerId))
            .ToList();

        if (voters.Count == 0)
        {
            voters = [.. PlayerControl.AllPlayerControls.ToArray()
                .Where(player => player != null &&
                                 !player.HasDied() &&
                                 player.PlayerId != innocent.Player.PlayerId &&
                                 player.PlayerId != exiled.PlayerId)];
        }

        foreach (var voter in voters)
        {
            voter.AddModifier<MisfortuneTargetModifier>();
        }

        InnocentHauntButton.ShowThisRound = voters.Count > 0;
        CustomButtonSingleton<InnocentHauntButton>.Instance?.SetActive(InnocentHauntButton.ShowThisRound, innocent);
    }

    private static void ShowWinNotification(InnocentRole innocent)
    {
        if (!innocent.Player.AmOwner)
        {
            return;
        }

        DeathHandlerModifier.RpcUpdateLocalDeathHandler(
            PlayerControl.LocalPlayer,
            "DiedToWinning",
            TownOfUs.Events.DeathEventHandlers.CurrentRound,
            DeathHandlerOverride.SetFalse,
            lockInfo: DeathHandlerOverride.SetTrue);

        MiraAPI.Utilities.Helpers.CreateAndShowNotification(
            $"<b>{TouExtensionColors.Innocent.ToTextColor()}{TouLocale.Get("ExtensionRoleInnocentWinNotif", "Your target was exiled. You win with the winners!")}</color></b>",
            Color.white,
            new Vector3(0f, 1f, -20f),
            spr: TouExtensionIcons.InnocentRoleIcon.LoadAsset())?.AdjustNotification();
    }
}

public static class InnocentTauntMeetingDisplay
{
    private const string TauntSymbol = "[+]";
    private static string? _tauntSymbolRichChunk;

    private static string TauntSymbolRichChunk =>
        _tauntSymbolRichChunk ??= $"<color=#{ColorUtility.ToHtmlStringRGBA(TouExtensionColors.Innocent)}> {TauntSymbol}</color>";

    internal static bool LocalShouldHighlightTauntTarget(PlayerControl row)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || row == null || local.Data == null) return false;

        foreach (var marker in row.GetModifiers<InnocentTargetModifier>())
        {
            if (!InnocentRole.ActiveInnocents.TryGetValue(marker.InnocentPlayerId, out _)) continue;
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

[HarmonyPatch(typeof(PlayerRoleTextExtensions), nameof(PlayerRoleTextExtensions.UpdateTargetColor),
    [typeof(Color), typeof(PlayerControl), typeof(DataVisibility)])]
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
