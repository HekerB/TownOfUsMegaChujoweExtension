using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using MiraAPI.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Neutral;

public enum GaslighterAbility
{
    Kill = 0,
    Knight = 1,
    Curse = 2,
    Shield = 3
}

public sealed class GaslighterRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable
{
    public string LocaleKey => "Gaslighter";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Gaslighter");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Gaslighter;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralEvil;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = false,
        Icon = TouRoleIcons.Vampire, // Placeholder
        IntroSound = TouAudio.MediumIntroSound
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new("Kill", "Kill players in the 1st round of each cycle.", TouAssets.KillSprite),
        new("Knight", "Grant extra votes to players in the 2nd round of each cycle.", TouRoleIcons.Monarch),
        new("Curse", "Mark players for death in the 3rd round of each cycle.", TouExtensionImpAssets.SpellButtonSprite),
        new("Shield", "Protect players in the 4th round of each cycle.", TouRoleIcons.Medic)
    ];

    public int MeetingCount { get; set; } = 0;

    public GaslighterAbility CurrentCycleAbility
    {
        get
        {
            // MeetingCount starts at 0 before first meeting, so Round 1 (pre-meeting 1) is index 0.
            return (GaslighterAbility)(MeetingCount % 4);
        }
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player)) return false;
        var console = usable.TryCast<Console>();
        return console == null || console.AllowImpostor;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        var options = OptionGroupSingleton<GaslighterOptions>.Instance;
        
        switch (options.WinCondition)
        {
            case GaslighterWinMode.CrewmateLose:
                return gameOverReason is GameOverReason.ImpostorsByKill or GameOverReason.ImpostorsBySabotage or GameOverReason.ImpostorsByVote;
            case GaslighterWinMode.LastStanding:
                return !Player.HasDied() && Helpers.GetAlivePlayers().Count <= 2; // Simple last standing check
            case GaslighterWinMode.AliveAtEnd:
                return !Player.HasDied();
            default:
                return false;
        }
    }

    [MethodRpc((uint)ExtensionRpc.GaslighterKnight)]
    public static void RpcGaslighterKnight(PlayerControl sender, PlayerControl target)
    {
        if (sender.Data.Role is not GaslighterRole) return;
        target.AddModifier<GaslighterKnightedModifier>();
    }

    [MethodRpc((uint)ExtensionRpc.GaslighterCurse)]
    public static void RpcGaslighterCurse(PlayerControl sender, PlayerControl target)
    {
        if (sender.Data.Role is not GaslighterRole role) return;
        target.AddModifier<GaslighterCursedModifier>(sender.PlayerId, role.MeetingCount);
    }

    [MethodRpc((uint)ExtensionRpc.GaslighterShield)]
    public static void RpcGaslighterShield(PlayerControl sender, PlayerControl target)
    {
        if (sender.Data.Role is not GaslighterRole) return;
        target.AddModifier<GaslighterShieldModifier>();
    }

    public override void OnMeetingStart()
    {
        RoleBehaviourStubs.OnMeetingStart(this);
        
        // Handle Curse kills at the start of meeting
        if (AmongUsClient.Instance.AmHost)
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && !pc.HasDied() && pc.TryGetModifier<GaslighterCursedModifier>(out var curse))
                {
                    if (curse.GaslighterId == Player.PlayerId)
                    {
                        // Use RpcSpecialMurder to kill without teleporting the Gaslighter
                        Player.RpcSpecialMurder(pc, isIndirect: true, teleportMurderer: false, causeOfDeath: "Gaslighted");
                        pc.RemoveModifier(curse);
                    }
                }
            }
        }

        MeetingCount++;
    }
}
