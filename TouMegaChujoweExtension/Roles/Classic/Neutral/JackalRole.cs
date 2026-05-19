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

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class JackalRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, TownOfUs.Extensions.ISpawnChange, IContinuesGame
{
    public bool ContinuesGame => !Player.HasDied();

    public bool WinConditionMet()
    {
        if (Player.HasDied()) return false;

        var alivePlayers = Helpers.GetAlivePlayers();
        var aliveCount = alivePlayers.Count;

        var jackalTeam = alivePlayers.Where(p =>
            p != null && p.Pointer != IntPtr.Zero &&
            (p.IsRole<JackalRole>() ||
             (p.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == Player.PlayerId))
        ).ToList();

        var jackalTeamCount = jackalTeam.Count;

        if (MiscUtils.ImpAliveCount > 0) return false;
        if (MiscUtils.NKillersAliveCount > alivePlayers.Count(p => p.IsRole<JackalRole>())) return false;
        if (alivePlayers.Any(p => p != null && p.Pointer != IntPtr.Zero && p.Is(RoleAlignment.CrewmateKilling))) return false;

        return aliveCount <= jackalTeamCount * 2;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        if (gameOverReason == CustomGameOver.GameOverReason<ExtensionNeutralGameOver>()) return true;
        return WinConditionMet();
    }
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
    };



    public bool NoSpawn =>
        PlayerControl.AllPlayerControls.ToArray().Any(p => p.HasModifier<TownOfUs.Modifiers.Game.Alliance.LoverModifier>()) ||
        PlayerControl.AllPlayerControls.ToArray().Any(p => p.HasModifier<TownOfUs.Modifiers.Game.Alliance.EgotistModifier>()) ||
        PlayerControl.AllPlayerControls.ToArray().Any(p => p.HasModifier<TownOfUs.Modifiers.Game.Alliance.CrewpostorModifier>());


    public void OffsetButtons()
    {
        var canVent = MiraAPI.GameOptions.OptionGroupSingleton<JackalOptions>.Instance.CanVent || LocalSettingsTabSingleton<TownOfUs.TownOfUsLocalSettings>.Instance.OffsetButtonsToggle.Value;
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
            if (OptionGroupSingleton<JackalOptions>.Instance.CanVent)
            {
                HudManager.Instance.ImpostorVentButton.graphic.sprite = TouNeutAssets.PestVentSprite.LoadAsset();
                HudManager.Instance.ImpostorVentButton.buttonLabelText.SetOutlineColor(RoleColor);
            }
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    public bool KillAbilityAlertShown { get; set; }

    public void OnRecruitDie()
    {
        Reactor.Utilities.Coroutines.Start(CoOnRecruitDie());
    }

    private System.Collections.IEnumerator CoOnRecruitDie()
    {
        // Wait a bit to ensure the player state is updated across the network/array
        yield return new WaitForSeconds(0.1f);

        // Check if all sidekicks are dead now
        var sidekicks = PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && p.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == Player.PlayerId)
            .ToList();

        var remainingSidekicks = sidekicks.Count(p => !p.Data.IsDead);

        UnityEngine.Debug.Log($"[TOUMCE] Jackal {Player.Data.PlayerName} sidekick died. Remaining: {remainingSidekicks}");

        if (remainingSidekicks == 0 && !KillAbilityAlertShown)
        {
            KillAbilityAlertShown = true;
            UnityEngine.Debug.Log("[TOUMCE] All sidekicks dead, enabling Jackal kill ability.");

            if (Player.AmOwner)
            {
                var killButton = MiraAPI.Hud.CustomButtonSingleton<JackalKillButton>.Instance;
                if (killButton != null)
                {
                    killButton.Timer = 10f;
                    UnityEngine.Debug.Log("[TOUMCE] Set Jackal Kill Timer to 10s");
                }

                string msg = TouLocale.Get("ExtensionJackalKillAbilityAlert");
                if (OptionGroupSingleton<JackalOptions>.Instance.ShieldWhileSidekicksAlive)
                {
                    msg += "\n" + TouLocale.Get("ExtensionJackalShieldLostAlert");
                }
                MiraAPI.Utilities.Helpers.CreateAndShowNotification(msg, TouExtensionColors.Jackal, new Vector3(0f, 1f, -20f), spr: TouRoleIcons.Jackal.LoadAsset()).AdjustNotification();
                Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Jackal));
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

    public void FixedUpdate()
    {
        if (Player == null || Player.Pointer == IntPtr.Zero || Player.Data == null || Player.Data.IsDead) return;

        // Manage Shield Modifier
        if (OptionGroupSingleton<JackalOptions>.Instance.ShieldWhileSidekicksAlive)
        {
            var sidekicksAlive = PlayerControl.AllPlayerControls.ToArray()
                .Any(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && !p.Data.IsDead && p.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == Player.PlayerId);

            if (sidekicksAlive)
            {
                if (!Player.HasModifier<JackalShieldModifier>())
                {
                    Player.AddModifier<JackalShieldModifier>();
                }
            }
            else
            {
                if (Player.HasModifier<JackalShieldModifier>())
                {
                    Player.RemoveModifier<JackalShieldModifier>();
                }
            }
        }
    }

    [MethodRpc((uint)ExtensionRpc.SetSidekickAssignments)]
    public static void RpcSetSidekickAssignments(PlayerControl sender, byte[] victims, byte[] jackalIds)
    {
        Patches.Roles.Jackal.JackalStartPatch.PendingAssignments.Clear();
        if (victims == null || jackalIds == null) return;

        for (int i = 0; i < victims.Length && i < jackalIds.Length; i++)
        {
            Patches.Roles.Jackal.JackalStartPatch.PendingAssignments[victims[i]] = jackalIds[i];
            UnityEngine.Debug.Log($"[TOUMCE] Synced sidekick {victims[i]} to Jackal {jackalIds[i]}");
        }

        // Update active SidekickModifiers with the synced Jackal IDs
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

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
public static class JackalDeathLifelinkPatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerControl __instance)
    {
        if (__instance == null || !AmongUsClient.Instance.AmHost) return;

        // If Jackal dies, kill their Sidekicks
        if (__instance.GetRole<JackalRole>() != null && OptionGroupSingleton<JackalOptions>.Instance.LifelinkDeath)
        {
            var sidekicks = PlayerControl.AllPlayerControls.ToArray()
                .Where(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && !p.Data.IsDead && p.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == __instance.PlayerId);

            foreach (var sk in sidekicks)
            {
                sk.RpcCustomMurder(__instance);
            }
        }
    }
}
