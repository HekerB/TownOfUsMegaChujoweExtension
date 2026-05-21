using System.Text;
using System.Linq;
using System.Collections.Generic;
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
using TownOfUs.Events;
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

    public override bool CanUse(IUsable console)
    {
        var validUsable = console.TryCast<Console>() ||
                          console.TryCast<DoorConsole>() ||
                          console.TryCast<OpenDoorConsole>() ||
                          console.TryCast<DeconControl>() ||
                          console.TryCast<PlatformConsole>() ||
                          console.TryCast<Ladder>() ||
                          console.TryCast<ZiplineConsole>();

        return GhostActive && validUsable;
    }

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => new List<Type> { typeof(PossessorSuccessorButton) };

    public void FixedUpdate()
    {
        if (Player == null || Player.Data.Role is not PossessorRole || MeetingHud.Instance)
        {
            return;
        }

        FadeUpdate();
    }

    public void CheckTaskRequirements()
    {
        UpdateTaskStage(silent: false, forceRecalculate: false);
    }

    private void UpdateTaskStage(bool silent, bool forceRecalculate)
    {
        if (Caught || Player == null)
        {
            return;
        }

        GetTaskCounts(Player, out var completedTasks, out var totalTasks);
        var tasksRemaining = totalTasks - completedTasks;

        var clickableAt = (int)OptionGroupSingleton<PossessorOptions>.Instance.TasksLeftBeforeClickable;
        GhostTaskStage newStage;
        if (totalTasks > 0 && completedTasks == totalTasks)
        {
            newStage = GhostTaskStage.CompletedTasks;
        }
        else if (tasksRemaining <= clickableAt)
        {
            newStage = GhostTaskStage.Clickable;
        }
        else
        {
            newStage = GhostTaskStage.Unclickable;
        }

        if (!forceRecalculate)
        {
            if ((TaskStage is GhostTaskStage.Unclickable && newStage is GhostTaskStage.Clickable) ||
                (totalTasks > 0 && completedTasks == totalTasks && TaskStage is not GhostTaskStage.CompletedTasks))
            {
                TaskStage = newStage;
                HandleStageChange(newStage, silent);
            }
            else
            {
                var textlog = $"Possessor Stage for '{Player.Data.PlayerName}': {TaskStage.ToDisplayString()} - ({completedTasks} / {totalTasks})";
                MiscUtils.LogInfo(TownOfUsEventHandlers.LogLevel.Error, textlog);
            }
        }
        else
        {
            TaskStage = newStage;
            HandleStageChange(newStage, silent);
        }
    }

    private void HandleStageChange(GhostTaskStage stage, bool silent)
    {
        var textlog = $"Possessor Stage for '{Player.Data.PlayerName}': {stage.ToDisplayString()}";
        MiscUtils.LogInfo(TownOfUsEventHandlers.LogLevel.Error, textlog);

        if (stage is GhostTaskStage.Clickable)
        {
            if (Player.AmOwner && !silent)
            {
                var notif1 = Helpers.CreateAndShowNotification(
                    $"<b>{RoleColor.ToTextColor()}You are now clickable by players!</b></color>",
                    Color.white,
                    new Vector3(0f, 1f, -20f), spr: Configuration.Icon.LoadAsset());
                notif1.AdjustNotification();
            }
        }
        else if (stage is GhostTaskStage.CompletedTasks)
        {
            if (Player.AmOwner && !silent)
            {
                var notif2 = Helpers.CreateAndShowNotification(
                    $"<b>{RoleColor.ToTextColor()}Tasks completed! You can now choose a successor!</b></color>",
                    Color.white,
                    new Vector3(0f, 1f, -20f), spr: Configuration.Icon.LoadAsset());
                notif2.AdjustNotification();
            }
        }
    }

    private static void GetTaskCounts(PlayerControl player, out int completed, out int total)
    {
        completed = 0;
        total = 0;

        if (player == null || player.Data == null)
        {
            return;
        }

        if (player.myTasks != null && player.myTasks.Count > 0)
        {
            var tasks = player.myTasks.ToArray().Where(x => !PlayerTask.TaskIsEmergency(x) && !x.TryCast<ImportantTextTask>());
            foreach (var t in tasks)
            {
                total++;
                var taskInfo = player.Data.FindTaskById(t.Id);
                var isComplete = taskInfo != null ? taskInfo.Complete : t.IsComplete;
                if (isComplete)
                {
                    completed++;
                }
            }

            return;
        }

        foreach (var info in player.Data.Tasks)
        {
            total++;
            if (info.Complete)
            {
                completed++;
            }
        }
    }
}