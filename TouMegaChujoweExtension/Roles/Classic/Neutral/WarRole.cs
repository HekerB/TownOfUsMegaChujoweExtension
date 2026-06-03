using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.LocalSettings;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using System.Text;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Options;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class WarRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable, IUnguessable
{
    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string RoleName => TouLocale.Get("ExtensionRoleWar", "War");
    public string RoleDescription => TouLocale.GetParsed("ExtensionRoleWarIntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed("ExtensionRoleWarTabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed("ExtensionRoleWarWikiDescription");
    }

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => [typeof(WarKillButton)];

    [HideFromIl2Cpp]
    public float WarSpreeUntil { get; set; }

    public Color RoleColor => TouExtensionColors.War;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralKilling;
    public bool HasImpostorVision => true;
    public bool IsGuessable => false;
    public RoleBehaviour AppearAs => this;

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = OptionGroupSingleton<BerserkerOptions>.Instance.WarCanVent,
        HideSettings = true,
        Icon = TouExtensionIcons.WarRoleIcon,
        IntroSound = TouAudio.WarlockIntroSound,
        MaxRoleCount = 0,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>(),
        OptionsScreenshot = TouBanners.NeutralRoleBanner,
    };

    public override void SpawnTaskHeader(PlayerControl playerControl)
    {
        if (playerControl != PlayerControl.LocalPlayer)
        {
            return;
        }

        var task = PlayerTask.GetOrCreateTask<ImportantTextTask>(playerControl, 0);
        task.Text = $"{TownOfUsColors.Neutral.ToTextColor()}{TouLocale.GetParsed("NeutralKillingTaskHeader")}</color>";
    }

    public bool WinConditionMet()
    {
        if (Player == null || Player.HasDied())
        {
            return false;
        }

        var alivePlayers = Helpers.GetAlivePlayers();
        var warsAlive = alivePlayers.Count(x => x != null && x.Data?.Role is WarRole);

        if (MiscUtils.ImpAliveCount > 0)
        {
            return false;
        }

        if (MiscUtils.NKillersAliveCount > warsAlive)
        {
            return false;
        }

        var otherKillers = MiscUtils.KillersAliveCount - warsAlive;
        if (otherKillers > 0)
        {
            return false;
        }

        return alivePlayers.Count <= warsAlive * 2;
    }

    public void OffsetButtons()
    {
        var canVent = OptionGroupSingleton<BerserkerOptions>.Instance.WarCanVent ||
                      LocalSettingsTabSingleton<TownOfUsLocalSettings>.Instance.OffsetButtonsToggle.Value;
        var kill = MiraAPI.Hud.CustomButtonSingleton<WarKillButton>.Instance;
        if (kill != null)
        {
            Reactor.Utilities.Coroutines.Start(MiscUtils.CoMoveButtonIndex(kill, !canVent));
        }
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);

        if (!player.HasModifier<InvulnerabilityModifier>())
        {
            player.AddModifier<InvulnerabilityModifier>(false, false, true);
        }

        if (player.AmOwner && HudManager.Instance?.ImpostorVentButton != null)
        {
            OffsetButtons();
            var ventButton = HudManager.Instance.ImpostorVentButton;
            ventButton.gameObject.SetActive(OptionGroupSingleton<BerserkerOptions>.Instance.WarCanVent);
            ventButton.graphic.sprite = TouNeutAssets.WerewolfVentSprite.LoadAsset();
            ventButton.buttonLabelText.SetOutlineColor(TouExtensionColors.War);
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

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player))
        {
            return false;
        }

        var console = usable.TryCast<Console>();
        if (console != null && !console.AllowImpostor)
        {
            return false;
        }

        var vent = usable.TryCast<Vent>();
        if (vent != null && !OptionGroupSingleton<BerserkerOptions>.Instance.WarCanVent)
        {
            return false;
        }

        return true;
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

    public StringBuilder SetTabText()
    {
        return ITownOfUsRole.SetNewTabText(this);
    }
}
