using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers.Crewmate;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TownOfUs;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Crewmate;

public sealed class SpiritMasterRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public override bool IsAffectedByComms => false;

    [HideFromIl2Cpp] public List<SpiritMasterMediatedModifier> MediatedPlayers { get; } = new();

    public DoomableType DoomHintType => DoomableType.Death;
    public string LocaleKey => "SpiritMaster";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Spirit Master");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") +
               MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}Mediate", "Mediate"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}MediateWikiDescription"),
            TouCrewAssets.MediateSprite)
    ];

    public Color RoleColor => TouExtensionColors.SpiritMaster;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateInvestigative;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouExtensionIcons.SpiritMasterIcon,
        OptionsScreenshot = TouBanners.MediumRoleBanner,
        IntroSound = TouAudio.MediumIntroSound,
    };

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);

        foreach (var modifier in MediatedPlayers.ToArray())
        {
            modifier.Player?.GetModifierComponent()?.RemoveModifier(modifier);
        }

        MediatedPlayers.Clear();
    }

    [MethodRpc((uint)ExtensionRpc.SpiritMasterMediate, LocalHandling = RpcLocalHandling.Before)]
    public static void RpcMediate(PlayerControl source, PlayerControl target)
    {
        if ((!source.AmOwner && !target.AmOwner) || (source.Data.Role is not SpiritMasterRole && !target.Data.IsDead))
        {
            return;
        }

        target.GetModifierComponent()?.AddModifier(new SpiritMasterMediatedModifier(source.PlayerId));
    }
}
