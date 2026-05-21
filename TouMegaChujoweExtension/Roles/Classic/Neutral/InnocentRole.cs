using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class InnocentRole(IntPtr cppPtr)
    : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable, ICrewVariant, IGuessable
{
    public static Dictionary<byte, InnocentRole> ActiveInnocents { get; } = new();

    public byte? PendingTauntKillerId { get; set; }
    public byte? TauntedKillerId { get; set; }
    public bool TargetVoted { get; set; }
    public bool AboutToWin { get; set; }
    public bool AwaitingNextMeetingExile { get; set; }
    public bool WinWindowExpired { get; set; }

    public DoomableType DoomHintType => DoomableType.Trickster;
    public RoleBehaviour CrewVariant => RoleManager.Instance.GetRole((RoleTypes)RoleId.Get<EngineerTouRole>());
    public bool CanBeGuessed => true;
    public string LocaleKey => "Innocent";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Innocent");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");
    public string GetAdvancedDescription() => TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    
    public Color RoleColor => TouExtensionColors.Innocent;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralEvil;
    public bool HasImpostorVision => false;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouExtensionIcons.InnocentRoleIcon,
        IntroSound = TownOfUs.Assets.TouAudio.NoisemakerIntroSound,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>(),
        MaxRoleCount = 1,
    };
    
    [HideFromIl2Cpp]
    public List<Type> RoleButtons => new List<Type> { typeof(InnocentTauntButton) };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}Taunt", "Taunt"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}TauntWikiDescription"),
            TouExtensionIcons.InnocentRoleIcon)
    ];

    public override void SpawnTaskHeader(PlayerControl playerControl)
    {
        if (playerControl != PlayerControl.LocalPlayer) return;
        var task = PlayerTask.GetOrCreateTask<ImportantTextTask>(playerControl, 0);
        task.Text = $"{TownOfUsColors.Neutral.ToTextColor()}{TouLocale.GetParsed("NeutralEvilTaskHeader")}</color>";
        task.name = "NeutralRoleText";
    }

    public override void Initialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Initialize(this, targetPlayer);
        ActiveInnocents[targetPlayer.PlayerId] = this;
        PendingTauntKillerId = null;
        TauntedKillerId = null;
        TargetVoted = false;
        AboutToWin = false;
        AwaitingNextMeetingExile = false;
        WinWindowExpired = false;
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        TouRoleUtils.ClearTaskHeader(Player);
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player)) return false;
        var console = usable.TryCast<Console>()!;
        return console == null || console.AllowImpostor;
    }

    public bool WinConditionMet()
    {
        if (!TargetVoted && !AboutToWin) return false;
        if (WouldImpostorsWin()) return false;
        return true;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        if (!TargetVoted) return false;
        return true;
    }

    private static bool WouldImpostorsWin()
    {
        if (MiscUtils.NKillersAliveCount > 0) return false;
        if (MiscUtils.ImpAliveCount > 0 && MiscUtils.CrewKillersAliveCount > 0) return false;

        var aliveCount = PlayerControl.AllPlayerControls.ToArray().Count(p => p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected);
        if (MiscUtils.GameHaltersAliveCount > 0 && aliveCount > 1) return false;
        if (MiscUtils.ImpAliveCount <= 0) return false;

        var aliveNonImpostors = aliveCount - MiscUtils.ImpAliveCount;
        return MiscUtils.ImpAliveCount >= aliveNonImpostors;
    }

    public static void ClearAndReload()
    {
        ActiveInnocents.Clear();
    }
}
