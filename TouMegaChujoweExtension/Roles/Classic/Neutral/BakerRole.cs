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
using MiraAPI.Modifiers;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class BakerRole(IntPtr cppPtr)
    : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public static bool PendingFamineAnnouncement { get; set; }

    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => "Baker";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Baker");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => [typeof(BakerGiveButton)];

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.Get("ExtensionRoleBakerGive", "Give"),
            TouLocale.Get("ExtensionRoleBakerGiveWikiDescription", "Give bread to a player."),
            TouExtensionNeuAssets.BakerGiveButtonSprite)
    ];

    public Color RoleColor => TouExtensionColors.Baker;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralKilling;
    public bool HasImpostorVision => false;

    public bool BreadGivenThisRound { get; set; }

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = false,
        IntroSound = TouAudio.ChefSound,
        Icon = TouExtensionIcons.BakerRoleIcon,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>(),
        OptionsScreenshot = TouBanners.NeutralRoleBanner,
    };

    public bool WinConditionMet() => false;

    public static int GetEffectiveBreadNeeded(PlayerControl? baker)
    {
        var configuredBreadNeeded = (int)OptionGroupSingleton<BakerOptions>.Instance.BreadNeeded;
        if (baker == null)
        {
            return configuredBreadNeeded;
        }

        var possibleRecipients = PlayerControl.AllPlayerControls.ToArray()
            .Count(x => x != null && !x.HasDied() && x.PlayerId != baker.PlayerId);

        return Math.Min(configuredBreadNeeded, possibleRecipients);
    }

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var stringB = ITownOfUsRole.SetNewTabText(this);
        var breadGivenCount = PlayerControl.AllPlayerControls.ToArray()
            .Count(x => x != null && !x.HasDied() && x.HasModifier<BakerBreadModifier>());
        var breadNeeded = GetEffectiveBreadNeeded(Player);

        stringB.AppendLine($"Bread Recipients: {breadGivenCount} / {breadNeeded}");
        return stringB;
    }

    public void OffsetButtons()
    {
        var canVent = OptionGroupSingleton<BakerOptions>.Instance.CanVent || LocalSettingsTabSingleton<TownOfUsLocalSettings>.Instance.OffsetButtonsToggle.Value;
        var giveButton = MiraAPI.Hud.CustomButtonSingleton<BakerGiveButton>.Instance;
        if (giveButton != null)
        {
            Reactor.Utilities.Coroutines.Start(MiscUtils.CoMoveButtonIndex(giveButton, !canVent));
        }
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        BreadGivenThisRound = false;

        if (player.AmOwner)
        {
            OffsetButtons();
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
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
        if (gameOverReason == MiraAPI.GameEnd.CustomGameOver.GameOverReason<GameOver.ExtensionNeutralGameOver>() &&
            TouMegaChujoweExtension.Patches.WinConditions.NeutralExtensionWinCondition.IsApocalypseAllianceWon)
        {
            return true;
        }
        return false;
    }

    [MethodRpc((uint)ExtensionRpc.BakerGiveBread)]
    public static void RpcGiveBread(PlayerControl baker, PlayerControl target)
    {
        if (baker == null || target == null || target.HasDied())
        {
            return;
        }

        if (baker.Data.Role is not BakerRole bakerRole)
        {
            return;
        }

        if (!target.HasModifier<BakerBreadModifier>())
        {
            target.AddModifier<BakerBreadModifier>();
        }

        if (!target.HasModifier<BakerBreadRevealModifier>())
        {
            target.AddModifier<BakerBreadRevealModifier>();
        }
    }

    [MethodRpc((uint)ExtensionRpc.BakerTransformToFamine, LocalHandling = Reactor.Networking.Rpc.RpcLocalHandling.Before)]
    public static void RpcTransformToFamine(PlayerControl baker)
    {
        if (baker == null || baker.HasDied())
        {
            return;
        }

        if (baker.Data.Role is not FamineRole)
        {
            baker.ChangeRole(RoleId.Get<FamineRole>());
        }

        if (OptionGroupSingleton<BakerOptions>.Instance.AnnounceFamine)
        {
            PendingFamineAnnouncement = true;
        }
    }

    public static void ShowPendingFamineAnnouncement()
    {
        if (!PendingFamineAnnouncement ||
            PlayerControl.LocalPlayer == null ||
            !OptionGroupSingleton<BakerOptions>.Instance.AnnounceFamine)
        {
            return;
        }

        PendingFamineAnnouncement = false;
        var notif = Helpers.CreateAndShowNotification(
            TouLocale.GetParsed("ExtensionRoleBakerFamineAnnouncement", "A terrible famine has consumed the Crew.\\%nl\\%\\%color=#023020FF\\%Famine\\%/color\\%, Horseman of the Apocalypse, has emerged!"),
            Color.white,
            new Vector3(0f, 1f, -20f),
            spr: TouExtensionIcons.FamineRoleIcon.LoadAsset());
        notif.AdjustNotification();
    }
}
