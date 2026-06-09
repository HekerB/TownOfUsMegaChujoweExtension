using System;
using System.Collections.Generic;
using System.Text;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.LocalSettings;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles.Neutral;
using TownOfUs.Roles;
using TownOfUs.Modifiers;
using TownOfUs.Events;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class JokerRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable, IContinuesGame
{
    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => "Joker";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public bool MetWinCon { get; set; }
    public bool ContinuesGame => !Player.HasDied() && OptionGroupSingleton<JokerOptions>.Instance.WinMode == JokerWinOptions.WinWithWinners && MetWinCon;

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") +
               MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities
    {
        get
        {
            return new List<CustomButtonWikiDescription>
            {
                new(TouLocale.GetParsed("ExtensionRoleJokerPlaceCloneWiki", "Place Clone"),
                    TouLocale.GetParsed("ExtensionRoleJokerPlaceCloneWikiDescription",
                        "Place a clone of a player on the map. If killing roles kill enough clones, you win!"),
                    TouExtensionNeuAssets.JokerPlaceCloneButtonSprite)
            };
        }
    }

    public Color RoleColor => TouExtensionColors.Joker;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralEvil;
    public bool HasImpostorVision => false;

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = false,
        Icon = TouExtensionIcons.JokerRoleIcon,
        IntroSound = TouExtensionAudio.JokerLaugh,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>()
    };

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var sb = ITownOfUsRole.SetNewTabText(this);

        var killsNeeded = (int)OptionGroupSingleton<JokerOptions>.Instance.KillsToWin;
        var currentKills = JokerCloneSystem.KilledCloneCount;

        if (MetWinCon)
        {
            sb.AppendLine("<b>Objective Complete!</b>");
        }
        else
        {
            sb.AppendLine($"Clones Killed: {currentKills} / {killsNeeded}");
        }

        return sb;
    }

    public bool WinConditionMet()
    {
        var options = OptionGroupSingleton<JokerOptions>.Instance;

        if (options.WinMode == JokerWinOptions.WinWithWinners) return false;

        if (Player.HasDied() && !MetWinCon) return false;

        return MetWinCon || JokerCloneSystem.KilledCloneCount >= (int)options.KillsToWin;
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        JokerCloneSystem.ClearAll();
        MetWinCon = false;
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        JokerCloneSystem.RemoveClonesForJoker(targetPlayer.PlayerId);
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player)) return false;
        var console = usable.TryCast<Console>()!;
        return console == null || console.AllowImpostor;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        return MetWinCon || JokerCloneSystem.KilledCloneCount >= (int)OptionGroupSingleton<JokerOptions>.Instance.KillsToWin;
    }

    // === RPC Methods ===

    [MethodRpc((uint)Networking.ExtensionRpc.JokerPlaceClone)]
    public static void RpcJokerPlaceClone(PlayerControl joker, byte appearancePlayerId, float x, float y, float z)
    {
        var appearanceSource = MiscUtils.PlayerById(appearancePlayerId);
        if (appearanceSource == null) return;

        JokerCloneSystem.PlaceClone(joker.PlayerId, appearanceSource, new Vector3(x, y, z));
    }

    [MethodRpc((uint)Networking.ExtensionRpc.JokerCloneKilled)]
    public static void RpcJokerCloneKilled(PlayerControl killer, byte jokerId, byte cloneIndex)
    {
        JokerCloneSystem.AddKill();

        if (!JokerCloneSystem.TryRemoveClone(cloneIndex, out _)) return;

        if (killer.AmOwner)
        {
            try
            {
                SoundManager.Instance.PlaySound(TouExtensionAudio.JokerLaugh.LoadAsset(), false, 1f);

                var notif = Helpers.CreateAndShowNotification(
                    TouLocale.GetParsed("ExtensionRoleJokerFooledNotif", "You've been fooled!"),
                    TouExtensionColors.Joker,
                    new Vector3(0f, 1f, -20f),
                    spr: TouExtensionIcons.JokerRoleIcon.LoadAsset());

                notif?.AdjustNotification();
            }
            catch { }
        }

        var jokerPlayer = MiscUtils.PlayerById(jokerId);
        if (jokerPlayer == null) return;

        var options = OptionGroupSingleton<JokerOptions>.Instance;
        var killsNeeded = (int)options.KillsToWin;
        var currentKills = JokerCloneSystem.KilledCloneCount;

        if (currentKills >= killsNeeded)
        {
            if (jokerPlayer.Data.Role is JokerRole role)
            {
                role.MetWinCon = true;
                if (options.WinMode == JokerWinOptions.WinWithWinners)
                {
                    if (jokerPlayer.AmOwner)
                    {
                        DeathHandlerModifier.RpcUpdateLocalDeathHandler(
                            PlayerControl.LocalPlayer,
                            "DiedToWinning",
                            DeathEventHandlers.CurrentRound,
                            DeathHandlerOverride.SetFalse,
                            lockInfo: DeathHandlerOverride.SetTrue);
                    }
                }
            }
        }

        if (jokerPlayer.AmOwner)
        {
            try
            {
                var notif = Helpers.CreateAndShowNotification(
                    $"{TouLocale.GetParsed("ExtensionRoleJokerCloneKilledNotif", "Clone killed!")} ({currentKills}/{killsNeeded})",
                    TouExtensionColors.Joker,
                    new Vector3(0f, 1f, -20f),
                    spr: TouExtensionIcons.JokerRoleIcon.LoadAsset());

                notif?.AdjustNotification();
            }
            catch { }
        }
    }

    [MethodRpc((uint)Networking.ExtensionRpc.JokerDestroyClone)]
    public static void RpcJokerDestroyClone(PlayerControl joker, byte cloneIndex)
    {
        JokerCloneSystem.TryRemoveClone(cloneIndex, out _);
    }
}
