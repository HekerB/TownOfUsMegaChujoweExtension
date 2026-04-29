using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Events.Crewmate;
using TouMegaChujoweExtension.Networking;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Crewmate;

public sealed class DoctorRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Insight;
    public string LocaleKey => "Doctor";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
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
        UseVanillaKillButton = false,
        Icon = TouExtensionIcons.DoctorRoleIcon
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities
    {
        get
        {
            return new List<CustomButtonWikiDescription>
            {
                new(
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}Heal", "Heal"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}HealWikiDescription"),
                    TouExtensionImpAssets.InjectorInjectButtonSprite) // Placeholder
            };
        }
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    [MethodRpc((uint)ExtensionRpc.DoctorHeal)]
    public static void RpcDoctorHeal(PlayerControl doctor, PlayerControl target, int randomSeed)
    {
        if (doctor.Data.Role is not DoctorRole)
        {
            Error("RpcDoctorHeal - Invalid doctor");
            return;
        }

        if (target == null || target.HasDied())
        {
            return;
        }

        DoctorEvents.ScheduleHeal(doctor, target, randomSeed);
    }

    [MethodRpc((uint)ExtensionRpc.DoctorShieldAttacked)]
    public static void RpcDoctorShieldAttacked(PlayerControl doctor, PlayerControl attacker, PlayerControl target)
    {
        if (doctor.Data.Role is not DoctorRole)
        {
            Error("RpcDoctorShieldAttacked - Invalid doctor");
            return;
        }

        DoctorEvents.ShieldAttacked(doctor, attacker, target);
    }
}
