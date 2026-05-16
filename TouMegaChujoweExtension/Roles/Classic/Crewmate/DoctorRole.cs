using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using Reactor.Utilities.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Events.Crewmate;
using TouMegaChujoweExtension.Networking;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Modifiers.Crewmate;
using MiraAPI.Modifiers;
using Reactor.Utilities;

namespace TouMegaChujoweExtension.Roles.Classic.Crewmate;

public sealed class DoctorRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Insight;
    public string LocaleKey => "Doctor";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Doctor");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Doctor;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateSupport;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouExtensionIcons.DoctorRoleIcon,
        IntroSound = TouAudio.ScientistIntroSound,
        OptionsScreenshot = TouBanners.CrewmateRoleBanner,
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed("ExtensionRoleDoctorInject", "Inject"),
            TouLocale.GetParsed("ExtensionRoleDoctorInjectWikiDescription"),
            TouMegaChujoweExtension.Assets.TouExtensionCrewAssets.DoctorInjectButtonSprite)
    ];

    public override bool IsAffectedByComms => false;

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    [MethodRpc((uint)ExtensionRpc.DoctorInject)]
    public static void RpcDoctorInject(PlayerControl doctor, PlayerControl target, int seed)
    {
        DoctorEvents.ScheduleInject(doctor, target, seed);
    }

    [MethodRpc((uint)ExtensionRpc.DoctorShieldAttacked)]
    public static void RpcDoctorShieldAttacked(PlayerControl doctor, PlayerControl target, PlayerControl attacker)
    {
        DoctorEvents.ShieldAttacked(doctor, attacker, target);
    }
}
