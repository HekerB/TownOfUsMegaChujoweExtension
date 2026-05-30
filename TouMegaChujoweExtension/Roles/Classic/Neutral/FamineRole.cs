using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
using TouMegaChujoweExtension.Events.Neutral;
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

public sealed class FamineRole(IntPtr cppPtr)
    : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IUnguessable
{
    public const string StarvedDeathReason = "Starved";

    [HideFromIl2Cpp]
    public bool CanStarveAnyone { get; set; }

    [HideFromIl2Cpp]
    public bool HadBreadTargets { get; set; }


    public string YouAreText => TouLocale.Get("YouAre");
    public string YouWereText => TouLocale.Get("YouWere");
    public string LocaleKey => "Famine";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Famine");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public bool IsGuessable => false;
    public RoleBehaviour AppearAs => RoleManager.Instance.GetRole((RoleTypes)RoleId.Get<BakerRole>());

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => [typeof(FamineStarveButton)];

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.Get("ExtensionRoleFamineStarve", "Starve"),
            TouLocale.Get("ExtensionRoleFamineStarveWikiDescription", "Mark a player to starve at the next meeting."),
            TownOfUs.Assets.TouNeutAssets.ReapSprite)
    ];

    public Color RoleColor => TouExtensionColors.Famine;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralKilling;
    public bool HasImpostorVision => true;

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = OptionGroupSingleton<BakerOptions>.Instance.CanVent,
        HideSettings = true,
        CanModifyChance = false,
        DefaultChance = 0,
        DefaultRoleCount = 0,
        MaxRoleCount = 0,
        IntroSound = TouAudio.PhantomIntroSound,
        Icon = TouExtensionIcons.FamineRoleIcon,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>()
    };

    public bool WinConditionMet()
    {
        if (Player == null || Player.HasDied())
        {
            return false;
        }

        var famineCount = Helpers.GetAlivePlayers().Count(x => x != null && x.Data != null && x.Data.Role is FamineRole);
        if (MiscUtils.KillersAliveCount > famineCount)
        {
            return false;
        }

        var aliveCount = Helpers.GetAlivePlayers().Count;
        return aliveCount <= 2;
    }

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var stringB = new StringBuilder();
        stringB.AppendLine(TownOfUsPlugin.Culture,
            $"{RoleColor.ToTextColor()}{YouAreText}<b> {RoleName},\n<size=80%>{RoleDescription}</size></b></color>");
        stringB.AppendLine(TownOfUsPlugin.Culture,
            $"<size=60%>{TouLocale.Get("Alignment")}: <b>{MiscUtils.GetParsedRoleAlignment(RoleAlignment, true)}</b></size>");
        stringB.Append("<size=70%>");
        stringB.AppendLine(TownOfUsPlugin.Culture, $"{RoleLongDescription}");

        return stringB;
    }

    public void OffsetButtons()
    {
        var canVent = OptionGroupSingleton<BakerOptions>.Instance.CanVent || LocalSettingsTabSingleton<TownOfUsLocalSettings>.Instance.OffsetButtonsToggle.Value;
        var starveButton = MiraAPI.Hud.CustomButtonSingleton<FamineStarveButton>.Instance;
        if (starveButton != null)
        {
            Reactor.Utilities.Coroutines.Start(MiscUtils.CoMoveButtonIndex(starveButton, !canVent));
        }
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        CanStarveAnyone = false;
        HadBreadTargets = PlayerControl.AllPlayerControls.ToArray()
            .Any(x => x != null && !x.HasDied() && x != player && x.HasModifier<BakerBreadModifier>());
        if (!HadBreadTargets)
        {
            CanStarveAnyone = true;
        }

        EnsureFamineInvulnerability(Player);

        if (player.AmOwner)
        {
            OffsetButtons();
            Reactor.Utilities.Coroutines.Start(CoStartStarveCooldown());
        }
    }

    private static IEnumerator CoStartStarveCooldown()
    {
        yield return new WaitForSeconds(0.1f);

        var starveButton = MiraAPI.Hud.CustomButtonSingleton<FamineStarveButton>.Instance;
        if (starveButton != null)
        {
            starveButton.Timer = starveButton.Cooldown;
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        if (Player.HasModifier<InvulnerabilityModifier>())
        {
            Player.RemoveModifier<InvulnerabilityModifier>();
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
        if (gameOverReason == MiraAPI.GameEnd.CustomGameOver.GameOverReason<GameOver.ExtensionNeutralGameOver>() &&
            TouMegaChujoweExtension.Patches.WinConditions.NeutralExtensionWinCondition.IsBakerFaminePlagueAllianceWon)
        {
            return true;
        }
        return WinConditionMet();
    }

    [MethodRpc((uint)ExtensionRpc.FamineStarve)]
    public static void RpcStarvePlayer(PlayerControl famine, PlayerControl target)
    {
        if (famine == null || target == null || target.HasDied())
        {
            return;
        }

        if (!target.HasModifier<FamineStarvedModifier>())
        {
            target.AddModifier<FamineStarvedModifier>();
        }

        if (!target.HasModifier<BakerBreadRevealModifier>())
        {
            target.AddModifier<BakerBreadRevealModifier>();
        }

        if (target.HasModifier<BakerBreadModifier>())
        {
            target.RemoveModifier<BakerBreadModifier>();
        }
    }

    [MethodRpc((uint)ExtensionRpc.FamineUnlock, LocalHandling = Reactor.Networking.Rpc.RpcLocalHandling.Before)]
    public static void RpcUnlockFamine(PlayerControl famine)
    {
        if (famine == null || famine.HasDied() || famine.Data?.Role is not FamineRole famineRole)
        {
            return;
        }

        famineRole.CanStarveAnyone = true;

        EnsureFamineInvulnerability(famine);

        if (!famine.AmOwner)
        {
            return;
        }

        Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(Color.white, 0.15f, 0.35f));

        var notif = Helpers.CreateAndShowNotification(
            TouLocale.Get("ExtensionRoleFamineAllBreadTargetsDead", "All your breaded targets have died. You can now starve anyone!"),
            Color.white,
            new Vector3(0f, 1f, -20f),
            spr: TouExtensionIcons.FamineRoleIcon.LoadAsset());
        notif?.AdjustNotification();
    }

    [MethodRpc((uint)ExtensionRpc.FamineQueueStarveAnimation, LocalHandling = Reactor.Networking.Rpc.RpcLocalHandling.Before)]
    public static void RpcQueueStarveAnimation(PlayerControl target)
    {
        if (target == null)
        {
            return;
        }

        BakerEvents.PendingStarvationDeaths.Add(target.PlayerId);

        if (target.AmOwner)
        {
            Reactor.Utilities.Coroutines.Start(CoShowQueuedStarveAnimation(target));
        }
    }

    private static IEnumerator CoShowQueuedStarveAnimation(PlayerControl target)
    {
        var timer = 0f;
        while (target != null && !target.HasDied() && timer < 2f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (target != null)
        {
            BakerEvents.TryShowStarvationAnimation(target);
        }
    }

    private static void EnsureFamineInvulnerability(PlayerControl famine)
    {
        if (famine.HasModifier<InvulnerabilityModifier>())
        {
            famine.RemoveModifier<InvulnerabilityModifier>();
        }

        famine.AddModifier<InvulnerabilityModifier>(false, false, true);
    }
}
