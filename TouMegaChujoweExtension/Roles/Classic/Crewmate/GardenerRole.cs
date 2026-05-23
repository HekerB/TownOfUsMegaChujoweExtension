using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using System;
using System.Collections.Generic;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Assets;

namespace TouMegaChujoweExtension.Roles.Classic.Crewmate;

public sealed class GardenerRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Insight;
    public string LocaleKey => "Gardener";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Gardener");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");
    public Color RoleColor => TouExtensionColors.Gardener;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateProtective;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = false,
        Icon = TouExtensionIcons.GardenerRoleIcon,
        IntroSound = TouAudio.ScientistIntroSound,
        OptionsScreenshot = TouBanners.CrewmateRoleBanner,
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.Get("ExtensionRoleGardenerGarden", "Garden"),
            TouLocale.GetParsed("ExtensionRoleGardenerGardenWikiDescription"),
            TouExtensionCrewAssets.GardenerButtonSprite)
    ];

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    public void PlaceGarden(Vector2 position)
    {
        RpcPlaceGarden(Player, position);
    }

    [MethodRpc((uint)ExtensionRpc.GardenerPlaceGarden)]
    public static void RpcPlaceGarden(PlayerControl player, Vector2 position)
    {
        if (player == null) return;

        var options = OptionGroupSingleton<GardenerOptions>.Instance;
        if (options == null) return;

        float maxLight = (ShipStatus.Instance != null && ShipStatus.Instance.MaxLightRadius > 0.1f)
            ? ShipStatus.Instance.MaxLightRadius
            : 3.75f;
        float radius = options.TrapSize * maxLight;
        GardenerSystem.SetGarden(player.PlayerId, position, radius);
    }


    [MethodRpc((uint)ExtensionRpc.GardenerAttackNotify)]
    public static void RpcGardenerAttackNotify(PlayerControl owner, byte attackerId, byte targetId, bool killed)
    {
        if (owner == null) return;
        GardenerSystem.RecordAttackLog(owner.PlayerId, attackerId, targetId, killed);
    }

    [MethodRpc((uint)ExtensionRpc.GardenerClearGarden)]
    public static void RpcClearGarden(PlayerControl player, byte ownerId)
    {
        GardenerSystem.RemoveGarden(ownerId);
    }
}