using System;
using System.Collections.Generic;
using System.Text;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameEnd;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs;
using TownOfUs.Extensions;
using TownOfUs.GameOver;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using TownOfUs.Interfaces;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Neutral;

public sealed class BountyHunterRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable, IContinuesGame
{
    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => "BountyHunter";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") +
               MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities => new();

    public Color RoleColor => TouExtensionColors.BountyHunter;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralEvil;
    public bool HasImpostorVision => false;
    public bool HasWon { get; set; }

    public bool MetWinCon => HasWon || BountyHunterSystem.HasWon;

    public bool ContinuesGame => !Player.HasDied()
        && OptionGroupSingleton<BountyHunterOptions>.Instance.WinMode == BountyHunterWinMode.WinWithWinners
        && Helpers.GetAlivePlayers().Count > 1;

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = OptionGroupSingleton<BountyHunterOptions>.Instance.CanVent,
        Icon = TouExtensionIcons.BountyHunterRoleIcon,
        IntroSound = TouExtensionAudio.BountyHunterIntroSound,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>()
    };

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var stringB = ITownOfUsRole.SetNewTabText(this);
        var needed = (int)OptionGroupSingleton<BountyHunterOptions>.Instance.TargetsToKill.Value;
        var done = BountyHunterSystem.KillsDone;
        stringB.AppendLine(TownOfUsPlugin.Culture, $"Targets Killed: {done} / {needed}");
        if (BountyHunterSystem.CurrentTarget != null && BountyHunterSystem.Hunting)
            stringB.AppendLine(TownOfUsPlugin.Culture,
                $"Current Target: {BountyHunterSystem.CurrentTarget.Data.PlayerName}");
        return stringB;
    }

 [HideFromIl2Cpp]
public bool WinConditionMet()
{
    if (!HasWon && !BountyHunterSystem.HasWon)
        return false;

    if (OptionGroupSingleton<BountyHunterOptions>.Instance.WinMode != BountyHunterWinMode.SoloWin)
        return false;

    return true;
}

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        BountyHunterSystem.Reset();
        BountyHunterSystem.BountyHunterPlayerId = player.PlayerId;
        HasWon = false;
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);

        if (!Player.HasModifier<BasicGhostModifier>() && (HasWon || BountyHunterSystem.HasWon))
        {
            Player.AddModifier<BasicGhostModifier>();
        }

        BountyHunterSystem.Reset();
    }

    public override void OnDeath(DeathReason reason)
    {
        RoleBehaviourStubs.OnDeath(this, reason);
        BountyHunterSystem.ClearArrowModifiers();
        BountyHunterSystem.Hunting = false;
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player)) return false;
        var console = usable.TryCast<Console>()!;
        return console == null || console.AllowImpostor;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        if (!HasWon && !BountyHunterSystem.HasWon)
            return false;

        var winMode = OptionGroupSingleton<BountyHunterOptions>.Instance.WinMode;
        if (winMode == BountyHunterWinMode.WinWithWinners)
            return true;

        return BountyHunterSystem.GameEndedByBH;
    }

    [MethodRpc((uint)Networking.ExtensionRpc.BountyHunterWin)]
    public static void RpcBountyHunterWin(PlayerControl player)
    {
        BountyHunterSystem.HasWon = true;
        var isSolo = OptionGroupSingleton<BountyHunterOptions>.Instance.WinMode == BountyHunterWinMode.SoloWin;
        BountyHunterSystem.GameEndedByBH = isSolo;

        if (player?.Data?.Role is BountyHunterRole bh)
            bh.HasWon = true;
    }
}
