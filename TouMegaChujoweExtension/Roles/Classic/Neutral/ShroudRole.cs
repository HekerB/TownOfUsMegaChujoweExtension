using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities.Assets;
using MiraAPI.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles.Neutral;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs;
using TownOfUs.Buttons;

using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class ShroudRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public override void SpawnTaskHeader(PlayerControl playerControl)
    {
        if (playerControl != PlayerControl.LocalPlayer) return;
        var task = PlayerTask.GetOrCreateTask<ImportantTextTask>(playerControl, 0);
        task.Text = $"{TownOfUsColors.Neutral.ToTextColor()}{TouLocale.GetParsed("NeutralKillingTaskHeader")}</color>";
    }

    public DoomableType DoomHintType => DoomableType.Relentless;
    public string LocaleKey => "Shroud";
    public string RoleName => TouLocale.GetParsed("ExtensionRoleShroud");
    public string RoleDescription => TouLocale.GetParsed("ExtensionRoleShroudIntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed("ExtensionRoleShroudTabDescription");

    public string GetAdvancedDescription() => RoleLongDescription + MiscUtils.AppendOptionsText(GetType());

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities => new()
    {
        new(
            TouLocale.GetParsed("ExtensionRoleShroudAbility"),
            TouLocale.GetParsed("ExtensionRoleShroudAbilityWikiDescription"),
            TouExtensionNeuAssets.ShroudAbilitySprite)
    };

    public Color RoleColor => TouExtensionColors.Shroud;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralKilling;
	public bool HasImpostorVision => true;

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = OptionGroupSingleton<ShroudOptions>.Instance.CanVent,
        Icon = TouExtensionIcons.ShroudRoleIcon,
        IntroSound = TouAudio.DetectiveIntroSound,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>(),
        OptionsScreenshot = TouBanners.NeutralRoleBanner,
    };

    [HideFromIl2Cpp]
    public PlayerControl? GetCurrentShroudedPlayer()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && !player.HasDied() &&
                player.TryGetModifier<ShroudedModifier>(out var mod) &&
                mod.ShroudOwnerId == Player.PlayerId)
            {
                return player;
            }
        }
        return null;
    }

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var sb = ITownOfUsRole.SetNewTabText(this);
        var shrouded = GetCurrentShroudedPlayer();
        if (shrouded != null)
        {
            sb.Append($"\n<b>{TouLocale.GetParsed("ExtensionRoleShroudShroudedPlayer")}:</b>");
            sb.Append($"\n{Color.white.ToTextColor()}{shrouded.Data.PlayerName}</color>");
        }
        return sb;
    }

public bool WinConditionMet()
{
    if (Player.HasDied()) return false;

    var aliveCount = Helpers.GetAlivePlayers().Count;
    var killersAlive = MiscUtils.KillersAliveCount;

    return aliveCount <= 2 && killersAlive == 1;
}

    public void OffsetButtons()
    {
        var canVent = OptionGroupSingleton<ShroudOptions>.Instance.CanVent || LocalSettingsTabSingleton<TownOfUsLocalSettings>.Instance.OffsetButtonsToggle.Value;
        var ability = MiraAPI.Hud.CustomButtonSingleton<ShroudAbilityButton>.Instance;
        var kill = MiraAPI.Hud.CustomButtonSingleton<ShroudKillButton>.Instance;
        
        Reactor.Utilities.Coroutines.Start(MiscUtils.CoMoveButtonIndex(ability, !canVent));
        Reactor.Utilities.Coroutines.Start(MiscUtils.CoMoveButtonIndex(kill, !canVent));
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
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

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.TryGetModifier<ShroudedModifier>(out var mod) && mod.ShroudOwnerId == targetPlayer.PlayerId)
            {
                mod.WasInteractedWith = false;
                player.RemoveModifier<ShroudedModifier>();
            }
        }
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player)) return false;
        var console = usable.TryCast<Console>();
        if (console != null && !console.AllowImpostor) return false;
        var vent = usable.TryCast<Vent>();
        if (vent != null && !OptionGroupSingleton<ShroudOptions>.Instance.CanVent) return false;
        return true;
    }

    public override bool DidWin(GameOverReason gameOverReason) => WinConditionMet();
}