using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Networking;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
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

using MiraAPI.GameEnd;
using MiraAPI.Roles;
using TouMegaChujoweExtension.GameOver;

namespace TouMegaChujoweExtension.Patches.Roles.Jackal;

[HarmonyPatch]
public static class JackalMechanicsPatch
{
    [RegisterEvent]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        if (IsMurderBlocked(@event.Source, @event.Target, MeetingCheck.OutsideMeeting))
        {
            @event.Cancel();
        }
    }

    [RegisterEvent]
    public static void MiraButtonClickEventHandler(MiraButtonClickEvent @event)
    {
        var button = @event.Button as MiraAPI.Hud.CustomActionButton<PlayerControl>;
        var source = PlayerControl.LocalPlayer;
        var target = button?.Target;

        if (target == null || button is not TownOfUs.Buttons.IKillButton || !button.CanClick()) return;
        if (source == null) return;

        if (IsMurderBlocked(source, target, MeetingCheck.OutsideMeeting))
        {
            @event.Cancel();
            source.SetKillTimer(10f);
        }
    }

    private static bool IsMurderBlocked(PlayerControl killer, PlayerControl victim, MeetingCheck meetingCheck = MeetingCheck.OutsideMeeting)
    {
        if (killer == null || killer.Pointer == IntPtr.Zero || victim == null || victim.Pointer == IntPtr.Zero) return false;

        // 1. Team protection (Sidekicks cannot kill each other)
        if (killer.TryGetModifier<SidekickModifier>(out var killerMod) && 
            victim.TryGetModifier<SidekickModifier>(out var victimMod) && 
            killerMod.JackalId == victimMod.JackalId)
        {
            return true;
        }

        // 2. Shield logic: protect Jackal if sidekicks are alive
        // Blocks ONLY kills outside of meetings (like Monarch)
        var jackal = victim.GetRole<JackalRole>();
        if (jackal != null && meetingCheck == MeetingCheck.OutsideMeeting)
        {
            var sidekicksAlive = PlayerControl.AllPlayerControls.ToArray()
                .Any(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && !p.Data.IsDead && p.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == victim.PlayerId);

            if (sidekicksAlive && OptionGroupSingleton<JackalOptions>.Instance.ShieldWhileSidekicksAlive)
            {
                if (victim.AmOwner)
                {
                    Helpers.CreateAndShowNotification(
                        TownOfUs.Modules.Localization.TouLocale.Get("ExtensionJackalShieldModifierDesc"), 
                        Color.white, 
                        new Vector3(0f, 1f, -20f), 
                        spr: TouRoleIcons.Jackal.LoadAsset()
                    ).AdjustNotification();
                    Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Jackal));
                }
                return true; // Block kill
            }
        }

        return false;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    [HarmonyPrefix]
    public static bool MurderPrefix(PlayerControl __instance, PlayerControl target)
    {
        if (IsMurderBlocked(__instance, target, MeetingCheck.OutsideMeeting)) return false;
        return true;
    }

    [HarmonyPatch(typeof(MiraAPI.Networking.CustomMurderRpc), nameof(MiraAPI.Networking.CustomMurderRpc.RpcCustomMurder), typeof(PlayerControl), typeof(PlayerControl), typeof(MeetingCheck), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool))]
    [HarmonyPrefix]
    public static bool MiraRpcCustomMurderPrefix(PlayerControl source, PlayerControl target, MeetingCheck meetingCheck)
    {
        if (source == target) return true;
        if (IsMurderBlocked(source, target, meetingCheck)) return false;
        return true;
    }


    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    [HarmonyPostfix]
    public static void FixedUpdatePostfix(PlayerControl __instance)
    {
        if (!__instance.AmOwner) return;
        
        var jackal = __instance.GetRole<JackalRole>();
        bool isSidekick = __instance.TryGetModifier<SidekickModifier>(out _);
        
        if (jackal == null && !isSidekick) return;

        // Fallback for assignment if it didn't run (Host only)
        if (AmongUsClient.Instance.AmHost && !JackalStartPatch.WasExecuted && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
        {
            JackalStartPatch.ExecuteAssignment();
        }

        byte teamJackalId = 255;
        if (jackal != null)
            teamJackalId = __instance.PlayerId;
        else if (isSidekick && __instance.TryGetModifier<SidekickModifier>(out var skMod))
            teamJackalId = skMod.JackalId;

        // Color teammates
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc == null || pc.Pointer == IntPtr.Zero || pc.Data == null || pc.cosmetics == null || pc.cosmetics.nameText == null) continue;
            
            bool isPcJackal = (pc.GetRole<JackalRole>() != null && pc.PlayerId == teamJackalId);
            bool isPcRecruit = (pc.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == teamJackalId) ||
                                (JackalStartPatch.PendingAssignments.TryGetValue(pc.PlayerId, out var jId) && jId == teamJackalId && JackalStartPatch.TimeSinceStart < 10f);

            bool shouldColor = false;
            if (jackal != null) // Local player is Infiltrator
                shouldColor = isPcJackal || isPcRecruit;
            else if (isSidekick) // Local player is Recruit
                shouldColor = isPcRecruit; // Recruits ONLY see other recruits, not the Infiltrator

            if (shouldColor)
            {
                pc.cosmetics.nameText.color = TouExtensionColors.Jackal;
            }
            else if (pc.cosmetics.nameText.color == TouExtensionColors.Jackal)
            {
                // Reset if it was our team color but they shouldn't be visible to us
                pc.cosmetics.nameText.color = Color.white; 
            }
        }

        var sidekicksAlive = teamJackalId != 255 && PlayerControl.AllPlayerControls.ToArray()
            .Any(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && !p.Data.IsDead && p.PlayerId != __instance.PlayerId &&
                      (p.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == teamJackalId ||
                       (JackalStartPatch.PendingAssignments.TryGetValue(p.PlayerId, out var jId) && jId == teamJackalId && JackalStartPatch.TimeSinceStart < 10f)));

        var hasShieldMod = __instance.TryGetModifier<JackalShieldModifier>(out _);

        if (jackal != null && sidekicksAlive && OptionGroupSingleton<JackalOptions>.Instance.ShieldWhileSidekicksAlive)
        {
            if (!hasShieldMod)
            {
                __instance.RpcAddModifier<JackalShieldModifier>();
            }
        }
        else if (hasShieldMod)
        {
            __instance.RpcRemoveModifier<JackalShieldModifier>();
        }
    }

    [HarmonyPatch(typeof(RoleBehaviour), nameof(RoleBehaviour.DidWin))]
    [HarmonyPrefix]
    public static bool RoleBehaviourDidWinPrefix(RoleBehaviour __instance, GameOverReason gameOverReason, ref bool __result)
    {
        // Simplified safety: avoid accessing RoleBehaviour.Player which may be invalid.
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Pointer == IntPtr.Zero) return true;
        // If the local player is a Sidekick, override win condition.
        if (local.TryGetModifier<SidekickModifier>(out _))
        {
            __result = (gameOverReason == CustomGameOver.GameOverReason<ExtensionNeutralGameOver>());
            return false; // skip original logic for sidekick
        }
        // For all other cases, proceed with original logic.
        return true;
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    public static void MeetingHudStartPostfix()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null) return;

        // Fallback: if recruit wasn't notified yet (intro ran too early before assignment)
        if (local.TryGetModifier<SidekickModifier>(out var mod) && !mod.WasNotified)
        {
            mod.WasNotified = true;

            // Show recruitment notification
            Helpers.CreateAndShowNotification(
                TouLocale.Get("ExtensionSidekickRecruitedAlert"),
                TouExtensionColors.Jackal,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.SidekickModifierIcon.LoadAsset()
            ).AdjustNotification();

            // Chat message as additional info
            if (HudManager.Instance != null && HudManager.Instance.Chat != null)
            {
                string msg = TouLocale.Get("ExtensionSidekickRecruitedChatMsg");
                HudManager.Instance.Chat.AddChat(local, msg);
            }
        }
    }
}
