using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Networking;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Events.Mira;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Extensions;
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
using TownOfUs.Modules;
using TownOfUs.Utilities.Appearances;

namespace TouMegaChujoweExtension.Patches.Roles.Jackal;

[HarmonyPatch]
public static class JackalMechanicsPatch
{
    private static bool _lastShieldState;

    public static void ResetShieldState() => _lastShieldState = false;

    [RegisterEvent]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        if (IsMurderBlocked(@event.Source, @event.Target))
        {
            @event.Cancel();
        }
    }



    private static bool IsMurderBlocked(PlayerControl killer, PlayerControl victim, MeetingCheck meetingCheck = MeetingCheck.OutsideMeeting)
    {
        try
        {
            if (killer == null || killer.Pointer == IntPtr.Zero || victim == null || victim.Pointer == IntPtr.Zero || victim.Data == null) return false;

            byte killerJackalId = 255;
            if (killer.GetRole<JackalRole>() != null) killerJackalId = killer.PlayerId;
            else if (killer.TryGetModifier<SidekickModifier>(out var kMod) && kMod != null) killerJackalId = kMod.JackalId;

            byte victimJackalId = 255;
            if (victim.GetRole<JackalRole>() != null) victimJackalId = victim.PlayerId;
            else if (victim.TryGetModifier<SidekickModifier>(out var vMod) && vMod != null) victimJackalId = vMod.JackalId;

            if (killer.TryGetModifier<SidekickModifier>(out var kMod2) && kMod2 != null &&
                victim.TryGetModifier<SidekickModifier>(out var vMod2) && vMod2 != null &&
                kMod2.JackalId == vMod2.JackalId)
            {
                return true;
            }

            if (killer.GetRole<JackalRole>() != null)
            {
                var killerSidekicksAlive = PlayerControl.AllPlayerControls.ToArray()
                    .Any(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && !p.Data.IsDead && p.TryGetModifier<SidekickModifier>(out var m) && m != null && m.JackalId == killer.PlayerId);

                if (killerSidekicksAlive) return true;
            }

            if (MeetingHud.Instance != null || ExileController.Instance != null)
            {
                return false;
            }

            if (victim.GetRole<JackalRole>() != null)
            {
                var sidekicksAlive = PlayerControl.AllPlayerControls.ToArray()
                    .Any(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && !p.Data.IsDead && p.TryGetModifier<SidekickModifier>(out var m) && m != null && m.JackalId == victim.PlayerId);

                if (sidekicksAlive && OptionGroupSingleton<JackalOptions>.Instance != null && OptionGroupSingleton<JackalOptions>.Instance.ShieldWhileSidekicksAlive)
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[TOUMCE] Exception in IsMurderBlocked: {ex}");
        }

        return false;
    }

    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        if (@event.Target == null) return;

        var victim = @event.Target;

        if (victim.TryGetModifier<SidekickModifier>(out var mod))
        {
            var jackal = MiscUtils.PlayerById(mod.JackalId);
            if (jackal != null && !jackal.Data.IsDead)
            {
                var jackalRole = jackal.GetRole<JackalRole>();
                jackalRole?.OnRecruitDie();
            }
        }
        if (victim.GetRole<JackalRole>() != null && AmongUsClient.Instance.AmHost && OptionGroupSingleton<JackalOptions>.Instance != null && OptionGroupSingleton<JackalOptions>.Instance.LifelinkDeath)
        {
            foreach (var player in PlayerControl.AllPlayerControls.ToArray())
            {
                if (player != null && player.Data != null && !player.Data.IsDead && player.TryGetModifier<SidekickModifier>(out var sMod) && sMod.JackalId == victim.PlayerId)
                {
                    player.RpcCustomMurder(player, showKillAnim: false);
                    DeathHandlerModifier.UpdateDeathHandlerImmediate(
                        player,
                        causeOfDeath: TouLocale.Get("ExtensionSidekickJackalEliminatedDeathReason"),
                        roundOfDeath: DeathEventHandlers.CurrentRound,
                        diedThisRound: DeathHandlerOverride.SetTrue,
                        killedBy: victim.GetRole<JackalRole>()?.RoleName ?? "Jackal",
                        lockInfo: DeathHandlerOverride.SetTrue);
                }
            }
        }
    }

    [RegisterEvent]
    public static void PlayerDeathEventHandler(PlayerDeathEvent @event)
    {
        if (@event.Player == null) return;

        if (@event.Player.TryGetModifier<SidekickModifier>(out var mod))
        {
            var jackal = MiscUtils.PlayerById(mod.JackalId);
            if (jackal != null && !jackal.Data.IsDead)
            {
                jackal.GetRole<JackalRole>()?.OnRecruitDie();
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    [HarmonyPostfix]
    public static void FixedUpdatePostfix(PlayerControl __instance)
    {
        if (!__instance.AmOwner) return;

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
            bool isPcRecruit = (pc.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == teamJackalId) ||
                                (JackalStartPatch.PendingAssignments.TryGetValue(pc.PlayerId, out var jId) && jId == teamJackalId);

            if (isPcJackal || isPcRecruit)
            {
                pc.cosmetics.nameText.color = TouExtensionColors.Jackal;
            }
        }

        var sidekicksAlive = teamJackalId != 255 && PlayerControl.AllPlayerControls.ToArray()
            .Any(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && !p.Data.IsDead && p.PlayerId != __instance.PlayerId &&
                      (p.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == teamJackalId ||
                       (JackalStartPatch.PendingAssignments.TryGetValue(p.PlayerId, out var jId) && jId == teamJackalId)));

        var hasShieldMod = __instance.TryGetModifier<JackalShieldModifier>(out _);
        var shouldHaveShield = jackal != null && sidekicksAlive && OptionGroupSingleton<JackalOptions>.Instance.ShieldWhileSidekicksAlive;

        // Only send RPC when state changes to avoid network spam
        if (shouldHaveShield != _lastShieldState)
        {
            _lastShieldState = shouldHaveShield;
            if (shouldHaveShield && !hasShieldMod)
            {
                __instance.RpcAddModifier<JackalShieldModifier>();
            }
            else if (!shouldHaveShield && hasShieldMod)
            {
                __instance.RpcRemoveModifier<JackalShieldModifier>();
            }
        }
    }

    [RegisterEvent]
    public static void OnEjection(EjectionEvent @event)
    {
        var victim = @event.ExileController?.initData?.networkedPlayer?.Object;
        if (victim == null) return;

        if (victim.GetRole<JackalRole>() != null && AmongUsClient.Instance.AmHost && OptionGroupSingleton<JackalOptions>.Instance != null && OptionGroupSingleton<JackalOptions>.Instance.LifelinkDeath)
        {
            foreach (var recruit in PlayerControl.AllPlayerControls.ToArray())
            {
                if (recruit != null && recruit.Data != null && !recruit.Data.IsDead &&
                    recruit.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == victim.PlayerId)
                {
                    MiraAPI.Networking.CustomMurderRpc.RpcCustomMurder(recruit, recruit, MeetingCheck.OutsideMeeting, showKillAnim: false);
                    DeathHandlerModifier.UpdateDeathHandlerImmediate(
                        recruit,
                        causeOfDeath: TouLocale.Get("ExtensionSidekickJackalEliminatedDeathReason"),
                        roundOfDeath: DeathEventHandlers.CurrentRound,
                        diedThisRound: DeathHandlerOverride.SetTrue,
                        killedBy: victim.GetRole<JackalRole>()?.RoleName ?? "Jackal",
                        lockInfo: DeathHandlerOverride.SetTrue);
                }
            }
        }

        if (victim.TryGetModifier<SidekickModifier>(out var mod))
        {
            var jackal = MiscUtils.PlayerById(mod.JackalId);
            if (jackal != null && !jackal.Data.IsDead)
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

            if (HudManager.Instance != null && HudManager.Instance.Chat != null)
            {
                HudManager.Instance.Chat.AddChat(local, TouLocale.Get("ExtensionSidekickRecruitedChatMsg"));
            }
        }
    }
}