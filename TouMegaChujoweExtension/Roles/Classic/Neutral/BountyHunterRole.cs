using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameEnd;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using Reactor.Networking.Attributes;
using System.Collections.Generic;
using System.Text;
using System;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.GameOver;
using TownOfUs.Interfaces;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles.Neutral;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs;
using TownOfUs.Buttons;
using MiraAPI.Hud;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using UnityEngine;
using System.Linq;

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class BountyHunterRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable, IContinuesGame
{
    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => "BountyHunter";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") +
               MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities => new()
    {
        new CustomButtonWikiDescription(
            TouLocale.Get("ExtensionRoleBountyHunterKill"),
            TouLocale.Get("ExtensionRoleBountyHunterKillWikiDescription"),
            new LoadableBundleAsset<Sprite>("OfficerShootButton", TouAssets.MainBundle)
        )
    };

    public Color RoleColor => TouExtensionColors.BountyHunter;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralEvil;
    public bool HasImpostorVision => OptionGroupSingleton<BountyHunterOptions>.Instance.HasImpostorVision;
    public bool HasWon { get; set; }
    public PlayerControl? CurrentTarget { get; set; }
    public byte? LastTargetPlayerId { get; set; }
    public int KillsDone { get; set; }
    public bool TargetKilledThisRound { get; set; }
    public bool Hunting { get; set; }
    public bool IntroFinished { get; set; }
    public float IntroFinishTime { get; set; }

    public bool MetWinCon => HasWon;

    public bool ContinuesGame => !Player.HasDied()
        && OptionGroupSingleton<BountyHunterOptions>.Instance.WinMode == BountyHunterWinMode.WinWithWinners
        && Helpers.GetAlivePlayers().Count > 1;

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = false,
        Icon = TouExtensionIcons.BountyHunterRoleIcon,
        IntroSound = TouExtensionAudio.BountyHunterIntroSound,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>(),
        OptionsScreenshot = TouBanners.NeutralRoleBanner,
    };

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var stringB = ITownOfUsRole.SetNewTabText(this);
        var needed = (int)OptionGroupSingleton<BountyHunterOptions>.Instance.TargetsToKill.Value;
        var done = KillsDone;
        
        stringB.Append(TownOfUsPlugin.Culture, 
            $"\n{TouLocale.GetParsed("ExtensionBHTabTargetsKilled", "Targets Killed: {0} / {1}").Replace("{0}", done.ToString()).Replace("{1}", needed.ToString())}");

        if (CurrentTarget != null && Hunting)
            stringB.Append(TownOfUsPlugin.Culture,
                $"\n{TouLocale.GetParsed("ExtensionBHTabCurrentTarget", "Current Target: {0}").Replace("{0}", CurrentTarget.Data.PlayerName)}");
        return stringB;
    }

 [HideFromIl2Cpp]
public bool WinConditionMet()
{
    if (!HasWon)
        return false;

    if (OptionGroupSingleton<BountyHunterOptions>.Instance.WinMode != BountyHunterWinMode.SoloWin)
        return false;

    return true;
}

    public void OffsetButtons()
    {
        var canVent = LocalSettingsTabSingleton<TownOfUsLocalSettings>.Instance.OffsetButtonsToggle.Value;
        var bounty = MiraAPI.Hud.CustomButtonSingleton<BountyHunterKillButton>.Instance;
        Reactor.Utilities.Coroutines.Start(MiscUtils.CoMoveButtonIndex(bounty, !canVent));
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        HasWon = false;
        CurrentTarget = null;
        LastTargetPlayerId = null;
        KillsDone = 0;
        TargetKilledThisRound = false;
        Hunting = false;
        IntroFinished = false;
        IntroFinishTime = 0f;
        
        if (player.AmOwner)
        {
            OffsetButtons();
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);

        if (targetPlayer.AmOwner)
        {
            ClearArrowModifiers();
        }

        if (!Player.HasModifier<BasicGhostModifier>() && HasWon)
        {
            Player.AddModifier<BasicGhostModifier>();
        }
    }

    public void ClearArrowModifiers()
    {
        var players = ModifierUtils.GetPlayersWithModifier<BountyHunterArrowModifier>();
        foreach (var player in players)
        {
            if (player.TryGetModifier<BountyHunterArrowModifier>(out var arrow) && arrow.Owner == Player)
            {
                player.RemoveModifier(arrow);
            }
        }
    }

    public void AssignNewTarget()
    {
        ClearArrowModifiers();

        if (Player == null || Player.Data == null || Player.Data.IsDead) return;

        var candidates = PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p != null
                        && p.Data != null
                        && !p.Data.IsDead
                        && !p.Data.Disconnected
                        && p.PlayerId != Player.PlayerId
                        && (!p.TryGetModifier<ChildModifier>(out var child) || child.IsAdult))
            .ToList();

        if (candidates.Count > 1 && CurrentTarget != null)
        {
            var filtered = candidates.Where(p => p.PlayerId != CurrentTarget.PlayerId).ToList();
            if (filtered.Count > 0)
                candidates = filtered;
        }

        if (candidates.Count == 0)
        {
            CurrentTarget = null;
            LastTargetPlayerId = null;
            return;
        }

        // Implement weighted selection: Neutrals and Impostors have 10% more chance
        var weightedCandidates = new List<PlayerControl>();
        foreach (var p in candidates)
        {
            int weight = 100;
            var role = p.GetTownOfUsRole();
            if (p.IsImpostorAligned() || (role != null && (role.RoleAlignment == TownOfUs.Roles.RoleAlignment.NeutralKilling || 
                                                           role.RoleAlignment == TownOfUs.Roles.RoleAlignment.NeutralEvil || 
                                                           role.RoleAlignment == TownOfUs.Roles.RoleAlignment.NeutralBenign)))
            {
                weight = 110; // 10% more
            }

            for (int i = 0; i < weight; i++)
            {
                weightedCandidates.Add(p);
            }
        }

        CurrentTarget = weightedCandidates[UnityEngine.Random.Range(0, weightedCandidates.Count)];
        LastTargetPlayerId = CurrentTarget.PlayerId;
        TargetKilledThisRound = false;

        if (Player.AmOwner && CurrentTarget != null)
        {
            CurrentTarget.AddModifier<BountyHunterArrowModifier>(Player, TouExtensionColors.BountyHunter);
        }
    }

    public void OnTargetKilled()
    {
        if (HasWon) return;

        KillsDone++;
        TargetKilledThisRound = true;

        var opts = OptionGroupSingleton<BountyHunterOptions>.Instance;
        var needed = (int)opts.TargetsToKill.Value;

        if (KillsDone >= needed)
        {
            HasWon = true;
            BountyHunterSystem.HasWon = true; 
            var isSolo = opts.WinMode == BountyHunterWinMode.SoloWin;
            BountyHunterSystem.GameEndedByBH = isSolo;
            ClearArrowModifiers();
            
            if (Player.AmOwner)
            {
                RpcBountyHunterWin(Player);
            }
            return;
        }

        if (Player.AmOwner)
        {
            AssignNewTarget();
        }
    }

    public override void OnDeath(DeathReason reason)
    {
        RoleBehaviourStubs.OnDeath(this, reason);
        ClearArrowModifiers();
        Hunting = false;
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player)) return false;
        var console = usable.TryCast<Console>()!;
        return console == null || console.AllowImpostor;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        if (!HasWon)
            return false;

        var winMode = OptionGroupSingleton<BountyHunterOptions>.Instance.WinMode;
        if (winMode == BountyHunterWinMode.WinWithWinners)
            return true;

        return BountyHunterSystem.GameEndedByBH;
    }

    [MethodRpc((uint)Networking.ExtensionRpc.BountyHunterWin)]
    public static void RpcBountyHunterWin(PlayerControl player)
    {
        BountyHunterSystem.HasWon = true;
        var isSolo = OptionGroupSingleton<BountyHunterOptions>.Instance.WinMode == BountyHunterWinMode.SoloWin;
        BountyHunterSystem.GameEndedByBH = isSolo;

        if (player?.Data?.Role is BountyHunterRole bh)
            bh.HasWon = true;
    }

    [MethodRpc((uint)Networking.ExtensionRpc.BountyHunterShowMisKill)]
    public static void RpcShowBountyHunterMisKillText(PlayerControl victim, PlayerControl bh)
    {
        if (victim != null)
        {
            DeathHandlerModifier.UpdateDeathHandlerImmediate(
                victim,
                TouLocale.Get("BountyHunterWrongVictim", "Wrong Victim"),
                TownOfUs.Events.DeathEventHandlers.CurrentRound,
                DeathHandlerOverride.SetTrue,
                "null",
                DeathHandlerOverride.SetTrue);
        }

        if (bh != null)
        {
            DeathHandlerModifier.UpdateDeathHandlerImmediate(
                bh,
                TouLocale.Get("DiedToSuicideBountyHunter", "Guild Execution"),
                TownOfUs.Events.DeathEventHandlers.CurrentRound,
                DeathHandlerOverride.SetTrue,
                "null",
                DeathHandlerOverride.SetTrue);

            if (bh.AmOwner)
            {
                var alertMsg = TouLocale.Get("BountyHunterWrongVictimAlert", "You broke the pact with the Guild and died!");
                Helpers.CreateAndShowNotification($"<b><size=120%><color=#{ColorUtility.ToHtmlStringRGBA(Palette.ImpostorRed)}>{alertMsg}</color></size></b>", Color.white, new Vector3(0f, 1.5f, -20f), spr: TouExtensionIcons.BountyHunterRoleIcon.LoadAsset());
            }
        }

        if (victim != null && victim.AmOwner)
        {
            var victimMsg = TouLocale.Get("BountyHunterWrongVictimAlertTarget", "The Bounty Hunter tried to kill the wrong target and was executed!");
            Helpers.CreateAndShowNotification($"<b>{victimMsg}</b>", Color.white, new Vector3(0f, 1.5f, -20f));
        }
    }
}
