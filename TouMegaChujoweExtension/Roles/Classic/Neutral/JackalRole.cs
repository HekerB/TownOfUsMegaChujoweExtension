using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Hud;
using MiraAPI.Utilities;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs.Modules.Localization;
using Il2CppInterop.Runtime.Attributes;
using TownOfUs.Roles;
using TownOfUs.Modules;
using System;
using MiraAPI.Utilities.Assets;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Roles.Neutral;
using TownOfUs.Modules.Wiki;
using TownOfUs.Extensions;
using TownOfUs;
using AmongUs.GameOptions;
using UnityEngine;
using System.Linq;
using TownOfUs.Utilities;
using TownOfUs.Interfaces;
using MiraAPI.GameEnd;
using TouMegaChujoweExtension.GameOver;
using TownOfUs.Patches;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options;
using HarmonyLib;
using System.Collections.Generic;
using System.Globalization;
using TownOfUs.Modifiers;
using TownOfUs.Events;

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class JackalRole(IntPtr cppPtr) : NeutralRole(cppPtr), IExtensionRole, IWikiDiscoverable, IDoomable, TownOfUs.Extensions.ISpawnChange, IContinuesGame, IUnlovable, ICrewVariant
{
    public bool IsUnlovable => true;

    public DoomableType DoomHintType => DoomableType.Trickster;

    public bool ContinuesGame => Player != null && Player.Pointer != IntPtr.Zero && !Player.HasDied();

    public bool WinConditionMet()
    {
        if (Player == null || Player.Pointer == IntPtr.Zero || Player.HasDied()) return false;

        var alivePlayers = Helpers.GetAlivePlayers();
        var aliveCount = alivePlayers.Count;

        var jackalTeam = alivePlayers.Where(p =>
            p != null && p.Pointer != IntPtr.Zero &&
            (p.IsRole<JackalRole>() ||
             (p.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == Player.PlayerId))
        ).ToList();

        var jackalTeamCount = jackalTeam.Count;

        return aliveCount == jackalTeamCount;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        if (Player == null || Player.Pointer == IntPtr.Zero) return false;

        if (gameOverReason == CustomGameOver.GameOverReason<ExtensionNeutralGameOver>())
        {
            return NeutralExtensionWinCondition.GetWinningJackalId() == Player.PlayerId;
        }
        return WinConditionMet();
    }
    public string LocaleKey => "Jackal";
    public RoleBehaviour CrewVariant => CrewVariants.Of<TownOfUs.Roles.Crewmate.DeputyRole>();
    public string RoleName => TouLocale.Get("ExtensionRoleJackal");
    public string RoleDescription => TouLocale.GetParsed("ExtensionRoleJackalIntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed("ExtensionRoleJackalTabDescription");
    public string IntroInfo => RoleDescription;
    public string LoreInfo => RoleLongDescription;
    public Color RoleColor { get; } = TouExtensionColors.Jackal;
    public bool HasImpostorVision => true;

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(TouLocale.GetParsed("ExtensionRoleJackalAssassination"),
            TouLocale.GetParsed("ExtensionRoleJackalAssassinationWikiDescription"),
            TouNeutAssets.PestKillSprite),
        new(TouLocale.GetParsed("ExtensionRoleJackalRecruits"),
            TouLocale.GetParsed("ExtensionRoleJackalRecruitsWikiDescription"),
            TouExtensionIcons.SidekickModifierIcon)
    ];

    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralOutlier;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouRoleIcons.Jackal,
        CanUseVent = OptionGroupSingleton<JackalOptions>.Instance.CanVent,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>(),
        OptionsScreenshot = TouBanners.NeutralRoleBanner,
        IntroSound = TouAudio.VampIntroSound,
        MaxRoleCount = 1,
    };



    public bool NoSpawn =>
        PlayerControl.AllPlayerControls.ToArray().Any(p => p.HasModifier<TownOfUs.Modifiers.Game.Alliance.LoverModifier>()) ||
        PlayerControl.AllPlayerControls.ToArray().Any(p => p.HasModifier<TownOfUs.Modifiers.Game.Alliance.EgotistModifier>()) ||
        PlayerControl.AllPlayerControls.ToArray().Any(p => p.HasModifier<TownOfUs.Modifiers.Game.Alliance.CrewpostorModifier>());


    public void OffsetButtons()
    {
        if (OptionGroupSingleton<JackalOptions>.Instance == null) return;
        var canVent = OptionGroupSingleton<JackalOptions>.Instance.CanVent ||
                      (LocalSettingsTabSingleton<TownOfUs.TownOfUsLocalSettings>.Instance != null && LocalSettingsTabSingleton<TownOfUs.TownOfUsLocalSettings>.Instance.OffsetButtonsToggle.Value);
        var kill = MiraAPI.Hud.CustomButtonSingleton<JackalKillButton>.Instance;
        if (kill != null)
        {
            Reactor.Utilities.Coroutines.Start(MiscUtils.CoMoveButtonIndex(kill, !canVent));
        }
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        if (player.AmOwner)
        {
            OffsetButtons();
            if (OptionGroupSingleton<JackalOptions>.Instance != null && OptionGroupSingleton<JackalOptions>.Instance.CanVent &&
                HudManager.Instance != null && HudManager.Instance.ImpostorVentButton != null && HudManager.Instance.ImpostorVentButton.graphic != null)
            {
                HudManager.Instance.ImpostorVentButton.graphic.sprite = TouNeutAssets.PestVentSprite.LoadAsset();
                HudManager.Instance.ImpostorVentButton.buttonLabelText.SetOutlineColor(RoleColor);
            }
            if (!player.HasModifier<TownOfUs.Modifiers.IgnoreInvulnerabilityModifier>())
            {
                player.AddModifier<TownOfUs.Modifiers.IgnoreInvulnerabilityModifier>();
            }
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        if (targetPlayer != null && targetPlayer.AmOwner)
        {
            var killButton = MiraAPI.Hud.CustomButtonSingleton<JackalKillButton>.Instance;
            if (killButton != null && killButton.Button != null && killButton.Button.gameObject != null)
            {
                UnityEngine.Object.Destroy(killButton.Button.gameObject);

                // Clear the reference as well. Mira reaches this button through `Button?.` in
                // SetHudActive, and the null-conditional does not see Unity's destroyed-object null,
                // so leaving it set throws on every hud refresh after the Infiltrator dies.
                killButton.Button = null;
                UnityEngine.Debug.Log("[TOUMCE] Deinitialize: Destroyed JackalKillButton.Button.gameObject for local Jackal.");
            }
            if (targetPlayer.HasModifier<TownOfUs.Modifiers.IgnoreInvulnerabilityModifier>())
            {
                targetPlayer.RemoveModifier<TownOfUs.Modifiers.IgnoreInvulnerabilityModifier>();
            }
        }
    }

    public bool KillAbilityAlertShown { get; set; }
    public bool ShieldExpiredAlertShown { get; set; }

    public void OnRecruitDie()
    {
        Reactor.Utilities.Coroutines.Start(CoOnRecruitDie());
    }

    private System.Collections.IEnumerator CoOnRecruitDie()
    {
        yield return new WaitForSeconds(0.1f);

        if (AmongUsClient.Instance == null || Player == null || Player.Pointer == IntPtr.Zero || Player.Data == null)
        {
            yield break;
        }

        var sidekicks = PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p != null && p.Pointer != IntPtr.Zero && p.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == Player.PlayerId)
            .ToList();

        var remainingSidekicks = sidekicks.Count(p => !p.HasDied());

        UnityEngine.Debug.Log($"[TOUMCE] Jackal {Player.Data.PlayerName} sidekick died. Remaining: {remainingSidekicks}");

        if (remainingSidekicks == 0 && !KillAbilityAlertShown)
        {
            KillAbilityAlertShown = true;
            UnityEngine.Debug.Log("[TOUMCE] All sidekicks dead, enabling Jackal kill ability.");

            if (Player.AmOwner && AmongUsClient.Instance != null)
            {
                var killButton = MiraAPI.Hud.CustomButtonSingleton<JackalKillButton>.Instance;
                if (killButton != null)
                {
                    killButton.Timer = 10f;
                    UnityEngine.Debug.Log("[TOUMCE] Set Jackal Kill Timer to 10s");
                }

                try
                {
                    var shieldOptions = OptionGroupSingleton<JackalOptions>.Instance;
                    if (shieldOptions != null)
                    {
                        string msg = TouLocale.Get("ExtensionJackalKillAbilityAlert");
                        if (shieldOptions.IsShieldActive(ExtensionDeathInfo.CurrentRound))
                        {
                            msg += "\n" + TouLocale.Get("ExtensionJackalShieldLostAlert");
                        }

                        var notification = MiraAPI.Utilities.Helpers.CreateAndShowNotification(
                            msg,
                            TouExtensionColors.Jackal,
                            new Vector3(0f, 1f, -20f),
                            spr: TouRoleIcons.Jackal.LoadAsset()
                        );

                        notification?.AdjustNotification();
                    }
                    Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Jackal));
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"[TOUMCE] Error showing Jackal kill ability notification: {ex}");
                }
            }
        }
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player)) return false;
        var console = usable.TryCast<Console>();
        return console == null || console.AllowImpostor;
    }

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed("ExtensionRoleJackalWikiDescription") +
               MiscUtils.AppendOptionsText(GetType());
    }

    /// <summary>Packs the host's recruit -> Infiltrator map as "victim:jackal,victim:jackal".</summary>
    public static string EncodeSidekickAssignments(IEnumerable<KeyValuePair<byte, byte>> assignments)
    {
        return string.Join(",", assignments.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
    }

    /// <summary>
    /// Broadcasts the host's recruit -> Infiltrator map.
    /// <para>
    /// This used to take two <c>byte[]</c> parameters, and Reactor's <c>MessageSerializer</c> cannot
    /// serialize <c>Byte[]</c>: the send threw before the message left the host, so every non-host
    /// client was left with an empty <see cref="Patches.Roles.Jackal.JackalStartPatch.PendingAssignments"/>
    /// and no idea which Recruit belonged to which Infiltrator. A string is a type Reactor does
    /// serialize, the same as the chat RPCs in this mod.
    /// </para>
    /// </summary>
    [MethodRpc((uint)ExtensionRpc.SetSidekickAssignments)]
    public static void RpcSetSidekickAssignments(PlayerControl sender, string payload)
    {
        _ = sender;
        Patches.Roles.Jackal.JackalStartPatch.PendingAssignments.Clear();
        if (string.IsNullOrEmpty(payload)) return;

        foreach (var pair in payload.Split(','))
        {
            var parts = pair.Split(':');
            if (parts.Length != 2 ||
                !byte.TryParse(parts[0], NumberStyles.Integer, TouMegaChujoweExtensionPlugin.Culture, out var victimId) ||
                !byte.TryParse(parts[1], NumberStyles.Integer, TouMegaChujoweExtensionPlugin.Culture, out var jackalId))
            {
                continue;
            }

            Patches.Roles.Jackal.JackalStartPatch.PendingAssignments[victimId] = jackalId;
            UnityEngine.Debug.Log($"[TOUMCE] Synced sidekick {victimId} to Jackal {jackalId}");
        }

        // The SidekickModifier carries the Jackal id as a constructor argument, but it can attach late
        // on a remote client - re-stamp it here so the two records cannot disagree.
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.Pointer != IntPtr.Zero && player.TryGetModifier<SidekickModifier>(out var mod) &&
                Patches.Roles.Jackal.JackalStartPatch.PendingAssignments.TryGetValue(player.PlayerId, out var jId))
            {
                mod.JackalId = jId;
                UnityEngine.Debug.Log($"[TOUMCE] Explicitly set JackalId={jId} for Sidekick {player.Data?.PlayerName}");
            }
        }
    }
}

