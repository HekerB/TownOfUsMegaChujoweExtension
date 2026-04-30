using System;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Modules;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs;


namespace TouMegaChujoweExtension.Roles.Crewmate;

public sealed class VampireHunterRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITouCrewRole, IWikiDiscoverable, ISpawnChange, IDoomable, IGuessable
{
    public int FailedStakes { get; set; }
    public int SuccessfulStakes { get; set; }

    public bool CanSpawnOnCurrentMode() => false;
    public bool NoSpawn => true;
	public override bool IsAffectedByComms => false;

    public bool CanBeGuessed =>
        RoleManager.Instance.GetRole((RoleTypes)RoleId.Get<VampireHunterRole>()) is ICustomRole customRole &&
        (int)customRole.GetCount()! > 0 && (int)customRole.GetChance()! > 0;

	public DoomableType DoomHintType => DoomableType.Relentless;
    public string LocaleKey => "VampireHunter";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Vampire Hunter");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") +
               MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities
    {
        get
        {
            return
            [
                new(
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}Stake", "Stake"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}StakeWikiDescription"),
                    TouExtensionCrewAssets.StakeButtonIcon)
            ];
        }
    }

    public Color RoleColor => TouExtensionColors.VampireHunter;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateKilling;
    public bool IsPowerCrew => true;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouExtensionIcons.VampireHunterRoleIcon,
        CanUseVent = false,
        MaxRoleCount = 1,
        IntroSound = TouAudio.VampIntroSound,
    };

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        FailedStakes = 0;
        SuccessfulStakes = 0;
    }

    [HideFromIl2Cpp]
    public int MaxFailedStakes => (int)OptionGroupSingleton<VampireHunterOptions>.Instance.MaxFailedStakes;

    [HideFromIl2Cpp]
    public bool HasStakesLeft => MaxFailedStakes == 0 || FailedStakes < MaxFailedStakes;

    public void ConvertToNewRole()
    {
        var become = OptionGroupSingleton<VampireHunterOptions>.Instance.BecomeOnVampireDeath;

        ushort newRoleId = become switch
        {
            VampireHunterBecomes.Sheriff => RoleId.Get<TownOfUs.Roles.Crewmate.SheriffRole>(),
            VampireHunterBecomes.Veteran => RoleId.Get<TownOfUs.Roles.Crewmate.VeteranRole>(),
            VampireHunterBecomes.Vigilante => RoleId.Get<TownOfUs.Roles.Crewmate.VigilanteRole>(),
            VampireHunterBecomes.Hunter => RoleId.Get<TownOfUs.Roles.Crewmate.HunterRole>(),
            VampireHunterBecomes.Officer => RoleId.Get<TownOfUs.Roles.Crewmate.OfficerRole>(),
            _ => (ushort)RoleTypes.Crewmate
        };

        Player.RpcChangeRole(newRoleId, false);
    }
}
