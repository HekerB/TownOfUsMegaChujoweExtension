using System.Text;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Buttons.Impostor;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs;
using TownOfUs.Assets;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Impostor;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using TownOfUs.Utilities.Appearances;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Impostor;

public sealed class PossessorRole(IntPtr cppPtr)
    : NeutralGhostRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IGhostRole
{
    public bool Setup { get; set; }
    public bool Caught { get; set; }
    public bool Faded { get; set; }

    public bool CompletedAllTasks => TaskStage == GhostTaskStage.CompletedTasks;
    public bool CompletionAnnounced { get; private set; }
    public bool SuccessorChosen { get; private set; }

    public GhostTaskStage TaskStage { get; private set; } = GhostTaskStage.Unclickable;

    public bool GhostActive => Setup && !Caught;

    public bool CanBeClicked
    {
        get
        {
            if (SuccessorChosen || Caught) return false;
            return TaskStage is GhostTaskStage.Clickable or GhostTaskStage.CompletedTasks;
        }
        set { }
    }
	
	public bool CanCatch()
	{
		return true;
	}

    public string LocaleKey => "Possessor";

    public override string RoleName =>
        TouLocale.Get($"ExtensionRole{LocaleKey}", "Possessor");

    public override string RoleDescription =>
        TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");

    public override string RoleLongDescription =>
        TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public override Color RoleColor => TouExtensionColors.Possessor;

    public override RoleAlignment RoleAlignment => RoleAlignment.ImpostorAfterlife;


    public override CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouRoleIcons.Witch,
        CanUseVent = false,
        HideSettings = false,
        ShowInFreeplay = true
    };

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);

        CompletionAnnounced = false;
        SuccessorChosen = false;
        Setup = false;
        Caught = false;
        Faded = false;
        TaskStage = GhostTaskStage.Unclickable;

        if (!player.HasModifier<BasicGhostModifier>())
            player.AddModifier<BasicGhostModifier>();

        MiscUtils.AdjustGhostTasks(player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        TouRoleUtils.ClearTaskHeader(Player);

        targetPlayer.ResetAppearance();
        targetPlayer.cosmetics.ToggleNameVisible(true);
        targetPlayer.cosmetics.currentBodySprite.BodySprite.color = Color.white;
        targetPlayer.gameObject.layer = LayerMask.NameToLayer("Ghost");
        targetPlayer.MyPhysics.ResetMoveState(true);
    }

    public void Spawn()
    {
        Setup = true;

        Player.gameObject.layer = LayerMask.NameToLayer("Players");

        if (Player.AmOwner)
        {
            Player.SpawnAtRandomVent();
            Player.MyPhysics.ResetMoveState(true);
            HudManager.Instance.AbilityButton.SetDisabled();
        }
    }

    public void FadeUpdate()
    {
        if (!Caught && Setup)
        {
            Player.GhostFade();
            Faded = true;
        }
        else if (Faded)
        {
            Player.ResetAppearance();
            Player.gameObject.layer = LayerMask.NameToLayer("Ghost");
            Faded = false;
        }
    }

    public void Clicked()
    {
        Caught = true;
        Player.Exiled();

        if (Player.AmOwner)
            HudManager.Instance.AbilityButton.SetEnabled();
    }

    public override bool DidWin(GameOverReason reason)
    {
        return reason is GameOverReason.ImpostorsByKill
            or GameOverReason.ImpostorsByVote
            or GameOverReason.ImpostorsBySabotage
            or GameOverReason.ImpostorDisconnect;
    }
	
	[MethodRpc((uint)ExtensionRpc.PossessorChooseSuccessor)]
	public static void RpcPossessorChooseSuccessor(PlayerControl possessor, PlayerControl target)
	{
		if (possessor?.Data?.Role is not PossessorRole role ||
			target == null || target.Data == null)
			return;

		if (role.SuccessorChosen ||
			target.HasDied() ||
			target.Data.Disconnected ||
			target.IsImpostorAligned())
			return;

		role.SuccessorChosen = true;
		role.Caught = true;
		role.FadeUpdate();

		target.ChangeRole(RoleId.Get<TraitorRole>());
	}
}