using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class DeathRole(IntPtr cppPtr)
    : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IUnguessable
{
    public const string DeathReason = "ExtensionDeathClaimed";

    public string LocaleKey => "Death";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Death");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");
    public bool IsGuessable => false;
    public RoleBehaviour AppearAs => RoleManager.Instance.GetRole((RoleTypes)RoleId.Get<SoulCollectorRole>());

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription");
    }

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => [typeof(DeathKillButton)];

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.Get("ExtensionRoleSoulCollectorReap", "Reap"),
            TouLocale.Get("ExtensionRoleDeathReapWikiDescription", "Kill a player and leave a blackened unreportable body."),
            TouNeutAssets.ReapSprite)
    ];

    public Color RoleColor => TouExtensionColors.Death;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralKilling;
    public bool HasImpostorVision => true;

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = OptionGroupSingleton<SoulCollectorOptions>.Instance.DeathCanVent,
        HideSettings = true,
        CanModifyChance = false,
        DefaultChance = 0,
        DefaultRoleCount = 0,
        MaxRoleCount = 0,
        IntroSound = TouAudio.PhantomIntroSound,
        Icon = TouExtensionIcons.SoulCollectorRoleIcon,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>()
    };

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        EnsureInvulnerability(player);

        if (player.AmOwner && HudManager.Instance?.ImpostorVentButton != null)
        {
            HudManager.Instance.ImpostorVentButton.graphic.sprite = TouNeutAssets.ReaperVentSprite.LoadAsset();
            HudManager.Instance.ImpostorVentButton.buttonLabelText.SetOutlineColor(TouExtensionColors.Death);
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);

        if (targetPlayer.HasModifier<InvulnerabilityModifier>())
        {
            targetPlayer.RemoveModifier<InvulnerabilityModifier>();
        }

        if (targetPlayer.AmOwner && HudManager.Instance?.ImpostorVentButton != null)
        {
            HudManager.Instance.ImpostorVentButton.graphic.sprite = TouAssets.VentSprite.LoadAsset();
            HudManager.Instance.ImpostorVentButton.buttonLabelText.SetOutlineColor(TownOfUsColors.Impostor);
        }
    }

    public bool WinConditionMet()
    {
        if (Player == null || Player.HasDied())
        {
            return false;
        }

        var deathCount = PlayerControl.AllPlayerControls.ToArray()
            .Count(x => x != null && !x.HasDied() && x.Data?.Role is DeathRole);

        if (MiscUtils.KillersAliveCount > deathCount)
        {
            return false;
        }

        var aliveCount = Helpers.GetAlivePlayers().Count;
        return aliveCount <= 2;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        if (gameOverReason == MiraAPI.GameEnd.CustomGameOver.GameOverReason<GameOver.ExtensionNeutralGameOver>() &&
            TouMegaChujoweExtension.Patches.WinConditions.NeutralExtensionWinCondition.IsApocalypseAllianceWon)
        {
            return true;
        }

        return WinConditionMet();
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player))
        {
            return false;
        }

        var console = usable.TryCast<Console>();
        return console == null || console.AllowImpostor;
    }

    private static void EnsureInvulnerability(PlayerControl death)
    {
        if (death.HasModifier<InvulnerabilityModifier>())
        {
            death.RemoveModifier<InvulnerabilityModifier>();
        }

        death.AddModifier<InvulnerabilityModifier>(false, false, true);
    }

    [MethodRpc((uint)ExtensionRpc.DeathKill)]
    public static void RpcDeathKill(PlayerControl death, PlayerControl target)
    {
        if (death == null ||
            target == null ||
            target.HasDied() ||
            death.Data?.Role is not DeathRole ||
            ApocalypseUtils.AreAllied(death, target))
        {
            return;
        }

        death.RpcSpecialMurder(
            target,
            ignoreShield: false,
            createDeadBody: true,
            teleportMurderer: false,
            showKillAnim: false,
            playKillSound: true,
            causeOfDeath: DeathReason);
    }

    [MethodRpc((uint)ExtensionRpc.DeathMarkBody, LocalHandling = Reactor.Networking.Rpc.RpcLocalHandling.Before)]
    public static void RpcMarkDeathBody(PlayerControl death, byte targetId)
    {
        var target = MiscUtils.PlayerById(targetId);
        if (death == null || target == null || death.Data?.Role is not DeathRole || ApocalypseUtils.AreAllied(death, target))
        {
            return;
        }

        SoulCollectorSystem.MarkDeathBody(targetId);
    }
}
