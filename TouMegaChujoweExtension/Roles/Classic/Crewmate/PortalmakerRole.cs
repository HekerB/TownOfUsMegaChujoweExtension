using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using System;
using System.Collections.Generic;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;
using System.Linq;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Buttons.Classic.Crewmate;
using MiraAPI.Hud;

namespace TouMegaChujoweExtension.Roles.Classic.Crewmate;

public sealed class PortalmakerRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Trickster;

    public string LocaleKey => "Portalmaker";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Portalmaker");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");
    public Color RoleColor => TouExtensionColors.Portalmaker;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateSupport;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = false,
        Icon = TouExtensionIcons.PortalmakerRoleIcon,
        IntroSound = TouAudio.TimeLordIntroSound,
        OptionsScreenshot = TouBanners.CrewmateRoleBanner,
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.Get("ExtensionRolePortalmakerPlace", "Place Portal"),
            TouLocale.GetParsed("ExtensionRolePortalmakerPlaceWikiDescription"),
            TouExtensionCrewAssets.PortalPlaceButtonSprite),
        new(
            TouLocale.Get("ExtensionRolePortalmakerTeleport", "Teleport"),
            TouLocale.GetParsed("ExtensionRolePortalmakerTeleportWikiDescription"),
            TouExtensionCrewAssets.PortalSprite)
    ];

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        if (player.AmOwner)
        {
            Modules.PortalmakerSystem.ClearPortals(player.PlayerId);
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    public override void OnMeetingStart()
    {
        RoleBehaviourStubs.OnMeetingStart(this);

        if (Player.AmOwner)
        {
            var placeButton = CustomButtonSingleton<PortalmakerPlaceButton>.Instance;
            if (placeButton != null)
            {
                placeButton.ResetCooldownAndOrEffect();
            }
        }
    }

    public void PlacePortal(Vector2 position)
    {
        RpcPlacePortal(Player, position);
    }

    [MethodRpc((uint)ExtensionRpc.PortalmakerPlacePortal)]
    public static void RpcPlacePortal(PlayerControl player, Vector2 position)
    {
        if (player == null) return;
        Modules.PortalmakerSystem.PlacePortal(player.PlayerId, position);
    }
}
