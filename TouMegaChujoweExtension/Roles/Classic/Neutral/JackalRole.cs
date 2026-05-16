using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Hud;
using MiraAPI.Utilities;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
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
        
        // Count Jackal team
        var jackalTeam = alivePlayers.Where(p => 
            p != null && p.Pointer != IntPtr.Zero &&
            (p.IsRole<JackalRole>() || 
             (p.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == Player.PlayerId))
        ).ToList();
        
        var jackalTeamCount = jackalTeam.Count;

        // 1. Impostors prevent Jackal win
        if (MiscUtils.ImpAliveCount > 0) return false;

        // 2. Other Neutral Killing roles prevent Jackal win
        if (MiscUtils.NKillersAliveCount > alivePlayers.Count(p => p.IsRole<JackalRole>())) return false;

        // 3. Crewmate Killing roles prevent win
        if (alivePlayers.Any(p => p != null && p.Pointer != IntPtr.Zero && p.Is(RoleAlignment.CrewmateKilling))) return false;

        // Parity win against passive crewmates
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
        
        Reactor.Utilities.Coroutines.Start(MiscUtils.CoMoveButtonIndex(kill, !canVent));
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

    private bool _killAbilityAlertShown = false;

    public void OnRecruitDie()
    {
        // Check if all sidekicks are dead now
        var remainingSidekicks = PlayerControl.AllPlayerControls.ToArray()
            .Count(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && !p.Data.IsDead && p.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == Player.PlayerId);

        if (remainingSidekicks == 0 && !_killAbilityAlertShown)
        {
            _killAbilityAlertShown = true;
            
            // Notification for Jackal
            if (Player.AmOwner)
            {
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
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
public static class JackalDeathLifelinkPatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerControl __instance, DeathReason reason)
    {
        if (__instance == null || !AmongUsClient.Instance.AmHost) return;

        // If Jackal dies, kill their Sidekicks
        if (__instance.GetRole<JackalRole>() != null)
        {
            if (OptionGroupSingleton<JackalOptions>.Instance.LifelinkDeath)
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
}

[HarmonyPatch(typeof(TouRoleManagerPatches), "AssignRoles")]
public static class JackalVampireExclusionPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        
        var options = OptionGroupSingleton<ExtensionGeneralOptions>.Instance;
        if (!options.PreventVampiresWithJackal) return;

        // Logic handled in DraftSystem or other patches
    }
}
