using System;
using System.Collections.Generic;
using System.Text;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using MiraAPI.Modifiers;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Events;
using TownOfUs.Interfaces;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles.Neutral;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class GrimReaperRole(IntPtr cppPtr)
    : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable, IContinuesGame
{
    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => "GrimReaper";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Grim Reaper");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public bool ContinuesGame =>
        !Player.HasDied()
        && HasCompletedObjective
        && OptionGroupSingleton<GrimReaperOptions>.Instance.WinMode == GrimReaperWinMode.GrimReaperWinsWithOthers
        && Helpers.GetAlivePlayers().Count > 1;

    public bool MetWinCon => HasCompletedObjective;

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.Get("ExtensionRoleGrimReaperReap", "Reap"),
            TouLocale.Get("ExtensionRoleGrimReaperReapWikiDescription", "Mark a player for death."),
            TownOfUs.Assets.TouNeutAssets.ReapSprite),
        new(
            TouLocale.Get("ExtensionRoleGrimReaperCollect", "Collect"),
            TouLocale.Get("ExtensionRoleGrimReaperCollectWikiDescription", "Collect a spawned soul of a marked dead player."),
            TownOfUs.Assets.TouNeutAssets.ReapSprite)
    ];

    public Color RoleColor => TouExtensionColors.GrimReaper;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralEvil;
    public bool HasImpostorVision => false;

    public int SoulsReaped { get; set; }
    public bool HasCompletedObjective { get; set; }

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = OptionGroupSingleton<GrimReaperOptions>.Instance.CanVent,
        IntroSound = TouAudio.ToppatIntroSound,
        Icon = TownOfUs.Assets.TouRoleIcons.Spectre,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>(),
        OptionsScreenshot = TouBanners.NeutralRoleBanner,
    };

    public bool CanContinueActing()
    {
        if (Player == null || Player.HasDied())
        {
            return false;
        }

        if (HasCompletedObjective &&
            OptionGroupSingleton<GrimReaperOptions>.Instance.WinMode == GrimReaperWinMode.GrimReaperWinsWithOthers)
        {
            return false;
        }

        return true;
    }

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var stringB = ITownOfUsRole.SetNewTabText(this);
        var options = OptionGroupSingleton<GrimReaperOptions>.Instance;
        var soulsNeeded = (int)options.SoulsToWin;

        stringB.AppendLine($"Souls Reaped: {SoulsReaped} / {soulsNeeded}");

        if (HasCompletedObjective &&
            options.WinMode == GrimReaperWinMode.GrimReaperWinsWithOthers)
        {
            stringB.AppendLine("Objective complete");
        }

        return stringB;
    }

    public bool WinConditionMet()
    {
        if (Player.HasDied())
        {
            return false;
        }

        if (OptionGroupSingleton<GrimReaperOptions>.Instance.WinMode != GrimReaperWinMode.GrimReaperWins)
        {
            return false;
        }

        return SoulsReaped >= (int)OptionGroupSingleton<GrimReaperOptions>.Instance.SoulsToWin;
    }

    public void OffsetButtons()
    {
        var canVent = OptionGroupSingleton<GrimReaperOptions>.Instance.CanVent || LocalSettingsTabSingleton<TownOfUsLocalSettings>.Instance.OffsetButtonsToggle.Value;
        var reapButton = MiraAPI.Hud.CustomButtonSingleton<GrimReaperReapButton>.Instance;
        var collectButton = MiraAPI.Hud.CustomButtonSingleton<GrimReaperCollectButton>.Instance;
        if (reapButton != null)
        {
            Reactor.Utilities.Coroutines.Start(MiscUtils.CoMoveButtonIndex(reapButton, !canVent));
        }
        if (collectButton != null)
        {
            Reactor.Utilities.Coroutines.Start(MiscUtils.CoMoveButtonIndex(collectButton, !canVent));
        }
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        SoulsReaped = 0;
        HasCompletedObjective = false;
        
        if (player.AmOwner)
        {
            OffsetButtons();
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);

        if (!Player.HasModifier<BasicGhostModifier>() &&
            HasCompletedObjective &&
            OptionGroupSingleton<GrimReaperOptions>.Instance.WinMode == GrimReaperWinMode.GrimReaperWinsWithOthers)
        {
            Player.AddModifier<BasicGhostModifier>();
        }
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

    public override bool DidWin(GameOverReason gameOverReason)
    {
        if (HasCompletedObjective &&
            OptionGroupSingleton<GrimReaperOptions>.Instance.WinMode == GrimReaperWinMode.GrimReaperWinsWithOthers)
        {
            return true;
        }

        return WinConditionMet();
    }

    [MethodRpc((uint)ExtensionRpc.GrimReaperReap)]
    public static void RpcReapSoul(PlayerControl reaper, byte targetPlayerId)
    {
        if (reaper == null || reaper.Data == null || reaper.Data.Role is not GrimReaperRole reaperRole)
        {
            return;
        }

        if (!reaperRole.CanContinueActing())
        {
            return;
        }

        // Remove the soul globally
        GrimReaperSystem.ReapSoul(targetPlayerId);

        // Process reward
        reaperRole.SoulsReaped++;

        var options = OptionGroupSingleton<GrimReaperOptions>.Instance;
        var soulsNeeded = (int)options.SoulsToWin;

        if (reaperRole.SoulsReaped >= soulsNeeded && !reaperRole.HasCompletedObjective)
        {
            reaperRole.HasCompletedObjective = true;

            if (options.WinMode == GrimReaperWinMode.GrimReaperWinsWithOthers)
            {
                RpcGrimReaperCompletedObjective(reaper);

                if (reaper.AmOwner)
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

        if (reaper.AmOwner)
        {
            ShowReaperNotification($"Soul reaped successfully! ({reaperRole.SoulsReaped} / {soulsNeeded})");
        }
    }

    [MethodRpc((uint)ExtensionRpc.GrimReaperMark)]
    public static void RpcMarkPlayer(PlayerControl reaper, PlayerControl target)
    {
        if (reaper == null || reaper.Data == null || target == null || target.HasDied())
        {
            return;
        }

        if (reaper.Data.Role is not GrimReaperRole reaperRole)
        {
            return;
        }

        if (!reaperRole.CanContinueActing())
        {
            return;
        }

        // Add modifier to the target player
        target.AddModifier<GrimReaperMarkedModifier>(reaper.PlayerId);

        if (reaper.AmOwner)
        {
            ShowReaperNotification($"{target.CachedPlayerData.PlayerName} has been marked for death!");
        }
    }

    [MethodRpc((uint)ExtensionRpc.GrimReaperReap + 100)] // Using a separate ID space offset for the helper RPC
    public static void RpcGrimReaperCompletedObjective(PlayerControl reaper)
    {
        if (reaper == null || reaper.Data == null) return;

        if (reaper.Data.Role is GrimReaperRole reaperRole)
        {
            reaperRole.HasCompletedObjective = true;
        }

        var text = reaper.Data.PlayerName + " " +
                   TouLocale.Get("ExtensionGrimReaperCompletedObjective", "has gathered enough souls!");

        try
        {
            var notif = Helpers.CreateAndShowNotification(
                text,
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TownOfUs.Assets.TouRoleIcons.Spectre.LoadAsset());
            notif?.AdjustNotification();
        }
        catch
        {
            if (HudManager.Instance != null)
            {
                HudManager.Instance.Notifier.AddDisconnectMessage(text);
            }
        }
    }

    private static void ShowReaperNotification(string text)
    {
        try
        {
            var notif = Helpers.CreateAndShowNotification(
                text,
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TownOfUs.Assets.TouRoleIcons.Spectre.LoadAsset());
            notif?.AdjustNotification();
        }
        catch
        {
            if (HudManager.Instance != null)
            {
                HudManager.Instance.Notifier.AddDisconnectMessage(text);
            }
        }
    }
}
