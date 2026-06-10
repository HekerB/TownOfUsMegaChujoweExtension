using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using System.Text;
using TownOfUs.Assets;
using TownOfUs.Events;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles.Neutral;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs;
using MiraAPI.Hud;
using TownOfUs.Buttons;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using UnityEngine;
using HarmonyLib;
using TownOfUs.Patches;
using MiraAPI.GameEnd;

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class PirateRole(IntPtr cppPtr)
    : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable, IContinuesGame
{
    // // // private static readonly BepInEx.Logging.ManualLogSource Log =
    // // //     BepInEx.Logging.Logger.CreateLogSource("PirateRole");

    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => "Pirate";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public bool ContinuesGame =>
        !Player.HasDied()
        && HasCompletedDuels
        && OptionGroupSingleton<PirateOptions>.Instance.WinMode == PirateWinMode.PirateWinsWithOthers
        && Helpers.GetAlivePlayers().Count > 1;

    public bool MetWinCon => HasCompletedDuels;

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities
    {
        get
        {
            return new List<CustomButtonWikiDescription>
            {
                new(
                    TouLocale.GetParsed("ExtensionRolePirateDuelWiki", "Duel"),
                    TouLocale.GetParsed("ExtensionRolePirateDuelWikiDescription"),
                    TouExtensionNeuAssets.PirateDuelButtonSprite)
            };
        }
    }

    public Color RoleColor => TouExtensionColors.Pirate;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralEvil;
    public bool HasImpostorVision => false;

    public int DuelsWon { get; set; }
    public byte DuelTargetId { get; set; } = byte.MaxValue;
    public byte LastDuelTargetId { get; set; } = byte.MaxValue;
    public int PirateChoice { get; set; }
    public int TargetChoice { get; set; }
    public bool DuelActive { get; set; }
    public bool DuelResolved { get; set; }
    public bool HasCompletedDuels { get; set; }

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = false,
        IntroSound = TouAudio.ToppatIntroSound,
        Icon = TouExtensionIcons.PirateRoleIcon,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>(),
        OptionsScreenshot = TouExtensionBanners.PirateBanner,
    };

    public bool CanContinueActing()
    {
        if (Player == null || Player.HasDied())
        {
            return false;
        }

        if (HasCompletedDuels &&
            OptionGroupSingleton<PirateOptions>.Instance.WinMode == PirateWinMode.PirateWinsWithOthers)
        {
            return false;
        }

        return true;
    }

    [HideFromIl2Cpp]
    public bool IsBlacklisted(byte targetId)
    {
        if (!OptionGroupSingleton<PirateOptions>.Instance.CantDuelSamePersonTwiceInARow)
        {
            return false;
        }

        return LastDuelTargetId != byte.MaxValue && LastDuelTargetId == targetId;
    }

    [HideFromIl2Cpp]
    private void HandleCompletedDuels(PlayerControl pirate)
    {
        var duelsNeeded = (int)OptionGroupSingleton<PirateOptions>.Instance.DuelsToWin.Value;
        if (DuelsWon < duelsNeeded || HasCompletedDuels)
        {
            return;
        }

        HasCompletedDuels = true;
        DuelTargetId = byte.MaxValue;
        ResetDuelState();

        if (PlayerControl.LocalPlayer != null)
        {
            HudManagerPatches.UpdateRoleNameText();
        }

        if (OptionGroupSingleton<PirateOptions>.Instance.WinMode == PirateWinMode.PirateWinsWithOthers)
        {
            RpcPirateCompletedDuels(pirate);

            if (pirate.AmOwner)
            {
                DeathHandlerModifier.RpcUpdateLocalDeathHandler(
                    PlayerControl.LocalPlayer,
                    "DiedToWinning",
                    DeathEventHandlers.CurrentRound,
                    DeathHandlerOverride.SetFalse,
                    lockInfo: DeathHandlerOverride.SetTrue);
                PlayerControl.LocalPlayer.RpcPlayerExile();
            }
        }
    }

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var stringB = ITownOfUsRole.SetNewTabText(this);
        var options = OptionGroupSingleton<PirateOptions>.Instance;
        var duelsNeeded = (int)options.DuelsToWin.Value;

        stringB.AppendLine($"Duels Won: {DuelsWon} / {duelsNeeded}");

        if (DuelTargetId != byte.MaxValue)
        {
            var target = MiscUtils.PlayerById(DuelTargetId);
            if (target != null)
            {
                stringB.AppendLine($"Current Target: {target.Data.PlayerName}");
            }
        }

        if (options.CantDuelSamePersonTwiceInARow && LastDuelTargetId != byte.MaxValue)
        {
            var lastTarget = MiscUtils.PlayerById(LastDuelTargetId);
            if (lastTarget != null && !lastTarget.HasDied())
            {
                stringB.AppendLine(TouLocale.GetParsed("ExtensionPirateTabCannotTarget", "Cannot target next round: {0}").Replace("{0}", lastTarget.Data.PlayerName));
            }
        }

        if (HasCompletedDuels &&
            options.WinMode == PirateWinMode.PirateWinsWithOthers)
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

        if (OptionGroupSingleton<PirateOptions>.Instance.WinMode != PirateWinMode.PirateWins)
        {
            return false;
        }

        return DuelsWon >= (int)OptionGroupSingleton<PirateOptions>.Instance.DuelsToWin.Value;
    }

    public void OffsetButtons()
    {
        var canVent = LocalSettingsTabSingleton<TownOfUsLocalSettings>.Instance.OffsetButtonsToggle.Value;
        var duel = MiraAPI.Hud.CustomButtonSingleton<PirateDuelButton>.Instance;
        Reactor.Utilities.Coroutines.Start(MiscUtils.CoMoveButtonIndex(duel, !canVent));
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        DuelsWon = 0;
        DuelTargetId = byte.MaxValue;
        LastDuelTargetId = byte.MaxValue;
        HasCompletedDuels = false;
        ResetDuelState();
        
        if (player.AmOwner)
        {
            OffsetButtons();
            HudManager.Instance.ImpostorVentButton.graphic.sprite = TouAssets.VentSprite.LoadAsset();
            HudManager.Instance.ImpostorVentButton.buttonLabelText.SetOutlineColor(RoleColor);
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);

        if (targetPlayer.AmOwner)
        {
            HudManager.Instance.ImpostorVentButton.graphic.sprite = TouAssets.VentSprite.LoadAsset();
            HudManager.Instance.ImpostorVentButton.buttonLabelText.SetOutlineColor(TownOfUsColors.Impostor);
        }

        if (!Player.HasModifier<BasicGhostModifier>() &&
            HasCompletedDuels &&
            OptionGroupSingleton<PirateOptions>.Instance.WinMode == PirateWinMode.PirateWinsWithOthers)
        {
            Player.AddModifier<BasicGhostModifier>();
        }
    }

    public void ResetDuelState()
    {
        PirateChoice = 0;
        TargetChoice = 0;
        DuelActive = false;
        DuelResolved = false;
        DuelTargetId = byte.MaxValue;
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
        if (HasCompletedDuels &&
            OptionGroupSingleton<PirateOptions>.Instance.WinMode == PirateWinMode.PirateWinsWithOthers)
        {
            return true;
        }

        return WinConditionMet();
    }

    [MethodRpc((uint)Networking.ExtensionRpc.PirateSetDuelTarget)]
    public static void RpcSetDuelTarget(PlayerControl pirate, byte targetId)
    {
        if (pirate.Data?.Role is not PirateRole pirateRole)
        {
            return;
        }

        if (!pirateRole.CanContinueActing())
        {
            return;
        }

        if (pirateRole.IsBlacklisted(targetId))
        {
            if (pirate.AmOwner)
            {
                ShowPirateNotification(TouLocale.Get("ExtensionPirateCantDuelSamePersonTwiceNotif", "You can't duel the same person twice in a row."));
            }

            return;
        }

        pirateRole.DuelTargetId = targetId;

        if (PlayerControl.LocalPlayer != null)
        {
            HudManagerPatches.UpdateRoleNameText();
        }
    }

    [MethodRpc((uint)Networking.ExtensionRpc.PirateDuelChoice)]
    public static void RpcDuelChoice(PlayerControl player, byte pirateId, int choice)
    {
        var piratePlayer = MiscUtils.PlayerById(pirateId);
        if (piratePlayer?.Data?.Role is not PirateRole pirateRole)
        {
            return;
        }

        if (!pirateRole.CanContinueActing())
        {
            return;
        }

        if (player.PlayerId == pirateId)
        {
            pirateRole.PirateChoice = choice;
        }
        else if (player.PlayerId == pirateRole.DuelTargetId)
        {
            pirateRole.TargetChoice = choice;
        }
    }

    [MethodRpc((uint)Networking.ExtensionRpc.PirateDuelResult)]
    public static void RpcDuelResult(PlayerControl pirate, byte targetId, int result)
    {
        if (pirate.Data?.Role is not PirateRole pirateRole)
        {
            return;
        }

        var target = MiscUtils.PlayerById(targetId);
        var choiceNames = new[] { "Rock", "Paper", "Scissors" };
        var pirateChoiceName = choiceNames[pirateRole.PirateChoice];
        var targetChoiceName = choiceNames[pirateRole.TargetChoice];
        // Always update last target ID if a duel was attempted, 
        // independent of the option check (we check the option in IsBlacklisted)
        pirateRole.LastDuelTargetId = targetId;

        if (result == 1)
        {
            pirateRole.DuelsWon++;
            pirateRole.HandleCompletedDuels(pirate);

            if (pirate.AmOwner)
            {
                PirateDuelSystem.FlashScreen(TouExtensionColors.Pirate, 0.5f, 0.3f);
                ShowPirateNotification($"You won the duel! ({pirateChoiceName} vs {targetChoiceName})\nTarget eliminated.");
            }

            PirateDuelSystem.AnimateMeetingDeath(targetId);

            if (target != null && !target.HasDied() && AmongUsClient.Instance.AmHost)
            {
                target.Exiled();

                DeathHandlerModifier.UpdateDeathHandlerImmediate(
                    target,
                    causeOfDeath: TouLocale.Get("ExtensionDiedToPirate", "Lost Duel"),
                    roundOfDeath: DeathEventHandlers.CurrentRound,
                    diedThisRound: DeathHandlerOverride.SetTrue,
                    killedBy: TouLocale.GetParsed("ExtensionDiedByPirateDuel", "Dueled by <player>").Replace("<player>", pirate.Data.PlayerName),
                    lockInfo: DeathHandlerOverride.SetTrue);

                // Trigger AfterMurderEvent so other systems (like Legacy Animation) pick it up
                var afterMurderEvent = new MiraAPI.Events.Vanilla.Gameplay.AfterMurderEvent(pirate, target, null);
                MiraEventManager.InvokeEvent(afterMurderEvent);
            }
        }
        else if (result == 0)
        {
            var options = OptionGroupSingleton<PirateOptions>.Instance;
            if (options.DrawCountsAsWin)
            {
                pirateRole.DuelsWon++;
                pirateRole.HandleCompletedDuels(pirate);

                if (pirate.AmOwner)
                {
                    PirateDuelSystem.FlashScreen(TouExtensionColors.Pirate, 0.5f, 0.3f);
                    ShowPirateNotification($"Draw! ({pirateChoiceName} vs {targetChoiceName})\nDuel counted.");
                }

                if (target != null && target.AmOwner)
                {
                    PirateDuelSystem.FlashScreen(TouExtensionColors.Pirate, 0.5f, 0.3f);
                    ShowPirateNotification($"Draw! ({targetChoiceName} vs {pirateChoiceName})\nYou survived the duel.");
                }
            }
            else
            {
                if (pirate.AmOwner)
                {
                    ShowPirateNotification($"Draw! ({pirateChoiceName} vs {targetChoiceName})\nDuel not counted.");
                }

                if (target != null && target.AmOwner)
                {
                    ShowPirateNotification($"Draw! ({targetChoiceName} vs {pirateChoiceName})\nYou survived the duel.");
                }
            }
        }
        else if (result == 2)
        {
            if (target != null && target.AmOwner)
            {
                PirateDuelSystem.FlashScreen(TouExtensionColors.Pirate, 0.5f, 0.3f);
                ShowPirateNotification($"You won the duel! ({targetChoiceName} vs {pirateChoiceName})\nThe Pirate failed.");
            }

            if (pirate.AmOwner)
            {
                ShowPirateNotification($"You lost the duel. ({pirateChoiceName} vs {targetChoiceName})");
            }
        }

        pirateRole.DuelResolved = true;
    }

    [MethodRpc((uint)Networking.ExtensionRpc.PirateCompletedDuels)]
    public static void RpcPirateCompletedDuels(PlayerControl pirate)
    {
        if (pirate == null || pirate.Data == null)
        {
            return;
        }

        if (pirate.Data.Role is PirateRole pirateRole)
        {
            pirateRole.HasCompletedDuels = true;
            pirateRole.DuelTargetId = byte.MaxValue;
            pirateRole.ResetDuelState();
        }

        var text = pirate.Data.PlayerName + " " +
                   TouLocale.Get("ExtensionPirateCompletedObjective", "has completed their Pirate objective!");

        try
        {
            var notif = Helpers.CreateAndShowNotification(
                text,
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.PirateRoleIcon.LoadAsset());
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

    private static void ShowPirateNotification(string text)
    {
        try
        {
            var notif = Helpers.CreateAndShowNotification(
                text,
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.PirateRoleIcon.LoadAsset());
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















