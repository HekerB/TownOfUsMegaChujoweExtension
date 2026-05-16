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

        if (player.AmOwner && !WasNotified && JackalId != 255)
        {
            WasNotified = true;
            Reactor.Utilities.Coroutines.Start(DelayedNotification());
        }
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();
        ClearArrows();

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

        if (Player != null)
        {
            HudManager.Instance.Chat.AddChat(Player, TouLocale.Get("ExtensionSidekickRecruitedChatMsg"));
        }

        MiraAPI.Utilities.Helpers.CreateAndShowNotification(TouLocale.Get("ExtensionSidekickRecruitedAlert"), TouExtensionColors.Jackal, spr: TouRoleIcons.Jackal.LoadAsset()).AdjustNotification();
        Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Jackal));
    }

    private static readonly Dictionary<byte, ArrowBehaviour> _arrows = [];

    public override void Update()
    {
        base.Update();

        if (JackalId == 255 && Player != null && Player.Pointer != IntPtr.Zero && Patches.Roles.Jackal.JackalStartPatch.PendingAssignments.TryGetValue(Player.PlayerId, out var jId))
        {
            JackalId = jId;
        }

        if (Player == null || !Player.AmOwner || Player.Data.IsDead)
        {
            ClearArrows();
            return;
        }

        var options = OptionGroupSingleton<JackalOptions>.Instance;
        if (!options.ShowArrowToSidekicks)
        {
            ClearArrows();
            return;
        }

        UpdateArrows();
    }

    [HideFromIl2Cpp]
    private void UpdateArrows()
    {
        var player = Player;
        if (player == null) return;

        var teamMembers = PlayerControl.AllPlayerControls.ToArray()
            .Where(p =>
            {
                if (p == null || p.Pointer == IntPtr.Zero || p.Data == null || p.Data.IsDead) return false;
                if (p.PlayerId == player.PlayerId) return false;

                if (p.PlayerId == JackalId) return true;

                return p.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == JackalId;
            })
            .ToList();

        var currentIds = teamMembers.Select(p => p.PlayerId).ToHashSet();
        var toRemove = _arrows.Keys.Where(id => !currentIds.Contains(id)).ToList();
        foreach (var id in toRemove)
        {
            if (_arrows.TryGetValue(id, out var arrow) && arrow != null && arrow.gameObject != null)
            {
                UnityEngine.Object.Destroy(arrow.gameObject);
            }
            _arrows.Remove(id);
        }

        foreach (var member in teamMembers)
        {
            if (!_arrows.TryGetValue(member.PlayerId, out var targetArrow))
            {
                targetArrow = MiscUtils.CreateArrow(player.transform, TouExtensionColors.Jackal);
                _arrows[member.PlayerId] = targetArrow;
            }

            if (targetArrow != null)
            {
                targetArrow.target = member.transform.position;
                targetArrow.gameObject.SetActive(true);
            }
        }
    }

    [HideFromIl2Cpp]
    private void ClearArrows()
    {
        foreach (var arrow in _arrows.Values)
        {
            if (arrow != null && arrow.gameObject != null) UnityEngine.Object.Destroy(arrow.gameObject);
        }
        _arrows.Clear();
    }

    public override bool? DidWin(GameOverReason reason)
    {
        if (reason == MiraAPI.GameEnd.CustomGameOver.GameOverReason<TouMegaChujoweExtension.GameOver.ExtensionNeutralGameOver>()) return true;

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
            if (target.PlayerId == killerSidekick.JackalId) return false;

            var targetSidekick = target.GetModifier<SidekickModifier>();
            if (targetSidekick != null && targetSidekick.JackalId == killerSidekick.JackalId) return false;
        }

        if (__instance.GetRole<JackalRole>() != null)
        {
            var targetSidekick = target.GetModifier<SidekickModifier>();
            if (targetSidekick != null && targetSidekick.JackalId == __instance.PlayerId) return false;
        }

        return true;
    }
}
