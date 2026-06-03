using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs.Modifiers.Game;
using UnityEngine;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Modules.Localization;
using Il2CppInterop.Runtime.Attributes;
using TownOfUs.Utilities;
using MiraAPI.GameOptions;
using TownOfUs;
using MiraAPI.GameEnd;
using System.Collections.Generic;
using System.Linq;
using System;
using HarmonyLib;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;

namespace TouMegaChujoweExtension.Modifiers.Neutral;

public sealed class SidekickModifier : AllianceGameModifier, IWikiDiscoverable
{
    public override string ModifierName => "Recruit";
    public override bool HideOnUi => false;
    public override LoadableAsset<Sprite> ModifierIcon => TouExtensionIcons.SidekickModifierIcon;
    public override Color FreeplayFileColor => TouExtensionColors.Jackal;

    public byte JackalId { get; set; } = 255;
    public bool HasBetrayed { get; set; } = true;
    public bool WasNotified { get; set; }

    public override AlliedFaction TrueFactionType => AlliedFaction.Neutral;
    public override bool CountTowardsTrueFaction => true;
    public override ModifierFaction FactionType => ModifierFaction.Alliance;
    public override bool GetsPunished => false;

    public SidekickModifier() : base() { }

    public SidekickModifier(byte jackalId) : base()
    {
        JackalId = jackalId;
    }

    public override string LocaleKey => "Sidekick";
    public override string GetDescription() => TouLocale.GetParsed("SidekickTabDescription");

    public static string ShortName => TouLocale.Get("ExtensionModifierSidekickShortName");

    public override int GetAssignmentChance() => 0;

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities => [];

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed("SidekickWikiDescription") +
               MiscUtils.AppendOptionsText(GetType());
    }

    public override void OnActivate()
    {
        base.OnActivate();

        var player = Player;
        if (player == null) return;

        if (JackalId == 255 && Patches.Roles.Jackal.JackalStartPatch.PendingAssignments.TryGetValue(player.PlayerId, out var jId))
        {
            JackalId = jId;
            UnityEngine.Debug.Log($"[TOUMCE] Sidekick {player.Data?.PlayerName} discovered their Jackal: {jId}");
        }

        if (player.AmOwner)
        {
            var intro = UnityEngine.Object.FindObjectOfType<IntroCutscene>();
            if (intro != null)
            {
                Patches.Roles.Jackal.JackalIntroPatch.UpdateIntroCutscene(intro);
            }

            if (!WasNotified && JackalId != 255)
            {
                WasNotified = true;
                Reactor.Utilities.Coroutines.Start(DelayedNotification());
            }
        }
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();

        if (AmongUsClient.Instance == null || GameManager.Instance == null) return;

        var jackalPlayer = PlayerControl.AllPlayerControls.ToArray()
            .FirstOrDefault(p => p != null && p.Pointer != IntPtr.Zero && p.PlayerId == JackalId);

        if (jackalPlayer != null && jackalPlayer.Pointer != IntPtr.Zero && jackalPlayer.GetRole<JackalRole>() is { } jackal)
        {
            jackal.OnRecruitDie();
        }
    }

    private System.Collections.IEnumerator DelayedNotification()
    {
        yield return new WaitForSeconds(3f);

        if (AmongUsClient.Instance == null || Player == null || Player.Pointer == IntPtr.Zero) yield break;

        try
        {
            if (HudManager.Instance != null && HudManager.Instance.Chat != null)
            {
                HudManager.Instance.Chat.AddChat(Player, TouLocale.Get("ExtensionSidekickRecruitedChatMsg"));
            }

            var notification = MiraAPI.Utilities.Helpers.CreateAndShowNotification(
                TouLocale.Get("ExtensionSidekickRecruitedAlert"), 
                TouExtensionColors.Jackal, 
                spr: TouRoleIcons.Jackal.LoadAsset()
            );
            if (notification != null)
            {
                notification.AdjustNotification();
            }
            Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Jackal));
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"[TOUMCE] Error showing Sidekick recruited notification: {ex}");
        }
    }

    public override void Update()
    {
        base.Update();

        if (JackalId == 255 && Player != null && Player.Pointer != IntPtr.Zero && Patches.Roles.Jackal.JackalStartPatch.PendingAssignments.TryGetValue(Player.PlayerId, out var jId))
        {
            JackalId = jId;
        }
    }

    public override bool? DidWin(GameOverReason reason)
    {
        if (JackalId != 255)
        {
            var jackalPlayer = PlayerControl.AllPlayerControls.ToArray()
                .FirstOrDefault(p => p != null && p.Pointer != IntPtr.Zero && p.PlayerId == JackalId);

            if (jackalPlayer != null)
            {
                var jackal = jackalPlayer.GetRole<JackalRole>();
                if (jackal != null)
                {
                    return jackal.DidWin(reason);
                }
            }
        }

        return false;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
public static class SidekickFriendlyFirePatch
{
    [HarmonyPrefix]
    public static bool Prefix(PlayerControl __instance, PlayerControl target)
    {
        if (__instance == null || target == null) return true;

        var killerSidekick = __instance.GetModifier<SidekickModifier>();
        if (killerSidekick != null)
        {
            var targetSidekick = target.GetModifier<SidekickModifier>();
            if (targetSidekick != null && targetSidekick.JackalId == killerSidekick.JackalId) return false;
        }

        return true;
    }
}