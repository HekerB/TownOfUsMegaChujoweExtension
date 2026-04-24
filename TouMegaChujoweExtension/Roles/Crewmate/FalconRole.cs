using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using MiraAPI.Patches.Stubs;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Buttons.Crewmate;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Assets;
using TownOfUs.Utilities;
using MiraAPI.Hud;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace TouMegaChujoweExtension.Roles.Crewmate;

public sealed class FalconRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Insight;
    public string LocaleKey => "Falcon";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

	public override bool IsAffectedByComms => false;

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities
    {
        get
        {
            return
            [
                new(
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}Zoom", "Zoom"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}ZoomWikiDescription"),
                    TouExtensionCrewAssets.ZoomOutButtonSprite)
            ];
        }
    }

    public Color RoleColor => TouExtensionColors.Falcon;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateInvestigative;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouExtensionIcons.FalconRoleIcon,
		IntroSound = TouAudio.QuestionSound,
    };

    public bool IsGuessable => true;
    public RoleBehaviour AppearAs => this;

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

public override void Deinitialize(PlayerControl targetPlayer)
{
    RoleBehaviourStubs.Deinitialize(this, targetPlayer);

    if (!targetPlayer.AmOwner) return;

    CustomButtonSingleton<FalconZoomButton>.Instance?.ForceReset();
}

    public override void OnDeath(DeathReason reason)
    {
        Deinitialize(Player);
    }
}
