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
    : NeutralRole(cppPtr), IExtensionRole, IWikiDiscoverable, IDoomable, ICrewVariant
{
    public static bool PendingFamineAnnouncement { get; set; }
    public static bool FamineAnnounced { get; set; }

    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => "Baker";
    public RoleBehaviour CrewVariant => CrewVariants.Of<TownOfUs.Roles.Crewmate.MedicRole>();
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

    /// <summary>
    /// The same solo win Famine has - a Baker who never got to transform still outlasts the
    /// lobby, and without this the game simply never ended around them.
    /// </summary>
    public bool WinConditionMet()
    {
        if (Player == null || Player.HasDied())
        {
            return false;
        }

        var alivePlayers = Helpers.GetAlivePlayers();
        var ownCount = alivePlayers.Count(x => x != null && x.Data != null && x.Data.Role is BakerRole);
        if (MiscUtils.KillersAliveCount > ownCount)
        {
            return false;
        }

        return alivePlayers.Count <= 2;
    }

    public static int GetEffectiveBreadNeeded(PlayerControl? baker)
    {
        var configuredBreadNeeded = (int)OptionGroupSingleton<BakerOptions>.Instance.BreadNeeded;
        if (baker == null)
        {
            return configuredBreadNeeded;
        }

        // Players already holding another Baker's bread cannot take this one's.
        var possibleRecipients = PlayerControl.AllPlayerControls.ToArray()
            .Count(x => x != null &&
                        !x.HasDied() &&
                        x.PlayerId != baker.PlayerId &&
                        !ApocalypseUtils.AreAllied(baker, x) &&
                        !x.HasModifier<InvulnerabilityModifier>() &&
                        (!x.HasModifier<BakerBreadModifier>() || BakerBreadModifier.HasBreadFrom(x, baker.PlayerId)));

        return Math.Min(configuredBreadNeeded, possibleRecipients);
    }

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var stringB = ITownOfUsRole.SetNewTabText(this);
        var breadGivenCount = BakerBreadModifier.CountFor(Player.PlayerId);
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

        return WinConditionMet();
    }

    [MethodRpc((uint)ExtensionRpc.BakerGiveBread)]
    public static void RpcGiveBread(PlayerControl baker, PlayerControl target)
    {
        if (baker == null || target == null || target.HasDied() || ApocalypseUtils.AreAllied(baker, target))
        {
            return;
        }

        if (baker.Data.Role is not BakerRole bakerRole)
        {
            return;
        }

        if (!target.HasModifier<BakerBreadModifier>())
        {
            target.AddModifier<BakerBreadModifier>(baker.PlayerId);
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

        foreach (var bread in baker.GetModifiers<BakerBreadModifier>())
        {
            TouMegaChujoweExtension.Events.Neutral.BakerEvents.QueueBreadReevaluation(bread.BakerId);
        }

        if (OptionGroupSingleton<BakerOptions>.Instance.AnnounceFamine)
        {
            PendingFamineAnnouncement = true;
            ShowPendingFamineAnnouncement();
        }
    }

    public static void ShowPendingFamineAnnouncement()
    {
        if (!PendingFamineAnnouncement ||
            FamineAnnounced ||
            PlayerControl.LocalPlayer == null ||
            MeetingHud.Instance == null ||
            !OptionGroupSingleton<BakerOptions>.Instance.AnnounceFamine)
        {
            return;
        }

        PendingFamineAnnouncement = false;
        FamineAnnounced = true;
        var msg = TouLocale.GetParsed("ExtensionRoleBakerFamineAnnouncement", "A terrible famine has consumed the Crew.\\%nl\\%\\%color=#023020FF\\%Famine\\%/color\\%, Horseman of the Apocalypse, has emerged!");
        var title = $"<color=#{UnityEngine.ColorUtility.ToHtmlStringRGBA(TouExtensionColors.Baker)}>{TouLocale.Get("ExtensionRoleBakerFamineAnnouncementTitle", "Famine Warning")}</color>";
        TouMegaChujoweExtension.Modules.RoleAlertUtils.ShowRoleAlert(
            $"<b>{msg.Replace("\n", " ")}</b>",
            Color.white,
            TouExtensionIcons.FamineRoleIcon.LoadAsset());

        MiscUtils.AddFakeChat(PlayerControl.LocalPlayer.Data, title, msg, false, true);
    }
}
