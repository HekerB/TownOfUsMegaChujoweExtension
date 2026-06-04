using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Networking;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Events.Mira;
using MiraAPI.Hud;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Roles;
using TownOfUs.Modules;
using TownOfUs.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Assets;
using TownOfUs.Modules.Localization;
using TownOfUs.Buttons;
using TownOfUs;
using TownOfUs.Modifiers;
using TownOfUs.Events;
using TownOfUs.Utilities.Appearances;

namespace TouMegaChujoweExtension.Patches.Roles.Jackal;

[HarmonyPatch]
public static class JackalMechanicsPatch
{
    public static void ResetShieldState() {}

    [RegisterEvent]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        if (IsMurderBlocked(@event.Source, @event.Target))
        {
            @event.Cancel();

            if (@event.Source != null && @event.Source.AmOwner && IsMurderBlockedByJackalShield(@event.Source, @event.Target))
            {
                @event.Source.SetKillTimer(10f);
            }
        }
    }

    [RegisterEvent]
    public static void MiraButtonClickEventHandler(MiraButtonClickEvent @event)
    {
        var button = @event.Button;
        if (button == null) return;
        var source = PlayerControl.LocalPlayer;
        if (source == null) return;
        if (MeetingHud.Instance || ExileController.Instance) return;

        var targetProp = button.GetType().GetProperty("Target", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        var target = targetProp?.GetValue(button) as PlayerControl;

        if (target == null)
        {
#pragma warning disable S3011
            var targetField = button.GetType().GetField("_target", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
#pragma warning restore S3011
            target = targetField?.GetValue(button) as PlayerControl;
        }

        if (target == null || button is not IKillButton || !button.CanClick()) return;

        if (!IsMurderBlockedByJackalShield(source, target)) return;

        @event.Cancel();

        button.SetTimer(10f);
        source.SetKillTimer(10f);
    }

    private static bool IsMurderBlockedByJackalShield(PlayerControl killer, PlayerControl victim)
    {
        try
        {
            if (killer == null || killer.Pointer == IntPtr.Zero || victim == null || victim.Pointer == IntPtr.Zero || victim.Data == null) return false;
            if (MeetingHud.Instance != null || ExileController.Instance != null) return false;

            if (victim.GetRole<JackalRole>() != null)
            {
                var sidekicksAlive = PlayerControl.AllPlayerControls.ToArray()
                    .Any(p => p != null && p.Pointer != IntPtr.Zero && !p.HasDied() && p.TryGetModifier<SidekickModifier>(out var m) && m != null && m.JackalId == victim.PlayerId);

                if (sidekicksAlive && OptionGroupSingleton<JackalOptions>.Instance != null && OptionGroupSingleton<JackalOptions>.Instance.ShieldWhileSidekicksAlive)
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[TOUMCE] Exception in IsMurderBlockedByJackalShield: {ex}");
        }

        return false;
    }

    private static bool IsMurderBlocked(PlayerControl killer, PlayerControl victim)
    {
        try
        {
            if (killer == null || killer.Pointer == IntPtr.Zero || victim == null || victim.Pointer == IntPtr.Zero || victim.Data == null) return false;

            byte killerJackalTeamId = 255;
            if (killer.GetRole<JackalRole>() != null) killerJackalTeamId = killer.PlayerId;
            else if (killer.TryGetModifier<SidekickModifier>(out var kMod) && kMod != null) killerJackalTeamId = kMod.JackalId;

            byte victimJackalTeamId = 255;
            if (victim.GetRole<JackalRole>() != null) victimJackalTeamId = victim.PlayerId;
            else if (victim.TryGetModifier<SidekickModifier>(out var vMod) && vMod != null) victimJackalTeamId = vMod.JackalId;

            if (killerJackalTeamId != 255 && killerJackalTeamId == victimJackalTeamId)
            {
                return true;
            }

            if (killer.GetRole<JackalRole>() != null)
            {
                var killerSidekicksAlive = PlayerControl.AllPlayerControls.ToArray()
                    .Any(p => p != null && p.Pointer != IntPtr.Zero && !p.HasDied() && p.TryGetModifier<SidekickModifier>(out var m) && m != null && m.JackalId == killer.PlayerId);

                if (killerSidekicksAlive) return true;
            }

            if (MeetingHud.Instance != null || ExileController.Instance != null)
            {
                return false;
            }

            if (IsMurderBlockedByJackalShield(killer, victim))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[TOUMCE] Exception in IsMurderBlocked: {ex}");
        }

        return false;
    }

    [RegisterEvent]
    public static void PlayerDeathEventHandler(PlayerDeathEvent @event)
    {
        if (@event == null || @event.Player == null) return;

        var deadPlayer = @event.Player;

        // 1. If a Sidekick died, notify their Jackal to update/remove the shield
        if (deadPlayer.TryGetModifier<SidekickModifier>(out var mod) && mod != null)
        {
            var jackal = MiscUtils.PlayerById(mod.JackalId);
            if (jackal != null && !jackal.HasDied())
            {
                jackal.GetRole<JackalRole>()?.OnRecruitDie();
            }
        }

        // 2. If a Jackal died, kill/exile all their Sidekicks (lifelink)
        if (deadPlayer.GetRoleWhenAlive() is JackalRole && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            var jackalRoleName = (deadPlayer.GetRoleWhenAlive() as ITownOfUsRole)?.RoleName ?? "Jackal";
            var sidekicks = PlayerControl.AllPlayerControls.ToArray()
                .Where(p => p != null && p.Pointer != IntPtr.Zero && !p.HasDied() && p.TryGetModifier<SidekickModifier>(out var m) && m != null && m.JackalId == deadPlayer.PlayerId)
                .ToList();

            foreach (var sk in sidekicks)
            {
                switch (@event.DeathReason)
                {
                    case DeathReason.Exile:
                        DeathHandlerModifier.UpdateDeathHandlerImmediate(
                            sk,
                            causeOfDeath: TouLocale.Get("ExtensionSidekickJackalEliminatedDeathReason"),
                            roundOfDeath: DeathEventHandlers.CurrentRound,
                            diedThisRound: DeathHandlerOverride.SetFalse,
                            killedBy: jackalRoleName,
                            lockInfo: DeathHandlerOverride.SetTrue);
                        sk.Exiled();
                        break;
                    default: // Kill or other
                        sk.RpcCustomMurder(sk, showKillAnim: false);
                        DeathHandlerModifier.UpdateDeathHandlerImmediate(
                            sk,
                            causeOfDeath: TouLocale.Get("ExtensionSidekickJackalEliminatedDeathReason"),
                            roundOfDeath: DeathEventHandlers.CurrentRound,
                            diedThisRound: DeathHandlerOverride.SetTrue,
                            killedBy: jackalRoleName,
                            lockInfo: DeathHandlerOverride.SetTrue);
                        break;
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    [HarmonyPostfix]
    public static void FixedUpdatePostfix(PlayerControl __instance)
    {
        if (__instance == null || !__instance.AmOwner || AmongUsClient.Instance == null) return;

        var jackal = __instance.GetRole<JackalRole>();
        bool isSidekick = __instance.TryGetModifier<SidekickModifier>(out var skMod);

        if (jackal == null && !isSidekick) return;

        byte teamJackalId = 255;
        if (jackal != null) teamJackalId = __instance.PlayerId;
        else if (skMod != null) teamJackalId = skMod.JackalId;

        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc == null || pc.Pointer == IntPtr.Zero || pc.Data == null || pc.cosmetics == null || pc.cosmetics.nameText == null) continue;

            bool isPcJackal = (pc.GetRole<JackalRole>() != null && pc.PlayerId == teamJackalId);
            bool isPcRecruit = (pc.TryGetModifier<SidekickModifier>(out var m) && m != null && m.JackalId == teamJackalId) ||
                                (JackalStartPatch.PendingAssignments.TryGetValue(pc.PlayerId, out var jId) && jId == teamJackalId);

            if (isPcJackal || isPcRecruit)
            {
                pc.cosmetics.nameText.color = TouExtensionColors.Jackal;
            }
        }

        var sidekicksAlive = teamJackalId != 255 && PlayerControl.AllPlayerControls.ToArray()
            .Any(p => p != null && p.Pointer != IntPtr.Zero && !p.HasDied() && p.PlayerId != __instance.PlayerId &&
                      (p.TryGetModifier<SidekickModifier>(out var m) && m != null && m.JackalId == teamJackalId ||
                       (JackalStartPatch.PendingAssignments.TryGetValue(p.PlayerId, out var jId) && jId == teamJackalId)));

        var hasShieldMod = __instance.TryGetModifier<JackalShieldModifier>(out _);
        var shouldHaveShield = jackal != null && sidekicksAlive && OptionGroupSingleton<JackalOptions>.Instance != null && OptionGroupSingleton<JackalOptions>.Instance.ShieldWhileSidekicksAlive;

        if (shouldHaveShield && !hasShieldMod)
        {
            __instance.RpcAddModifier<JackalShieldModifier>();
        }
        else if (!shouldHaveShield && hasShieldMod)
        {
            __instance.RpcRemoveModifier<JackalShieldModifier>();
        }
    }

    [RegisterEvent]
    public static void OnEjection(EjectionEvent @event)
    {
        if (@event == null || @event.ExileController == null || @event.ExileController.initData == null || @event.ExileController.initData.networkedPlayer == null) return;
        var victim = @event.ExileController.initData.networkedPlayer.Object;
        if (victim == null) return;

        if (victim.AmOwner && victim.GetRoleWhenAlive() is JackalRole)
        {
            var killButton = MiraAPI.Hud.CustomButtonSingleton<JackalKillButton>.Instance;
            if (killButton != null && killButton.Button != null && killButton.Button.gameObject != null)
            {
                UnityEngine.Object.Destroy(killButton.Button.gameObject);
                UnityEngine.Debug.Log("[TOUMCE] Ejection event: Destroyed JackalKillButton.Button.gameObject for local Jackal.");
            }
        }

        if (victim.GetRoleWhenAlive() is JackalRole && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            var jackalRoleName = (victim.GetRoleWhenAlive() as ITownOfUsRole)?.RoleName ?? "Jackal";
            foreach (var recruit in PlayerControl.AllPlayerControls.ToArray())
            {
                if (recruit != null && !recruit.HasDied() &&
                    recruit.TryGetModifier<SidekickModifier>(out var m) && m != null && m.JackalId == victim.PlayerId)
                {
                    MiraAPI.Networking.CustomMurderRpc.RpcCustomMurder(recruit, recruit, MeetingCheck.OutsideMeeting, showKillAnim: false);
                    DeathHandlerModifier.UpdateDeathHandlerImmediate(
                        recruit,
                        causeOfDeath: TouLocale.Get("ExtensionSidekickJackalEliminatedDeathReason"),
                        roundOfDeath: DeathEventHandlers.CurrentRound,
                        diedThisRound: DeathHandlerOverride.SetTrue,
                        killedBy: jackalRoleName,
                        lockInfo: DeathHandlerOverride.SetTrue);
                }
            }
        }

        if (victim.TryGetModifier<SidekickModifier>(out var mod) && mod != null)
        {
            var jackal = MiscUtils.PlayerById(mod.JackalId);
            if (jackal != null && !jackal.HasDied())
            {
                jackal.GetRole<JackalRole>()?.OnRecruitDie();
            }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    public static void MeetingHudStartPostfix()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null) return;

        if (local.TryGetModifier<SidekickModifier>(out var mod) && !mod.WasNotified)
        {
            mod.WasNotified = true;

            Helpers.CreateAndShowNotification(
                TouLocale.Get("ExtensionSidekickRecruitedAlert"),
                TouExtensionColors.Jackal,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.SidekickModifierIcon.LoadAsset()
            ).AdjustNotification();
        }
    }
}