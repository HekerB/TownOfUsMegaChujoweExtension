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
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmatePower;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = false,
        Icon = TouRoleIcons.Traitor,
        IntroSound = TouAudio.ScientistIntroSound,
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.Get("ExtensionRoleGardenerGarden", "Garden"),
            TouLocale.GetParsed("ExtensionRoleGardenerGardenWikiDescription"),
            TouRoleIcons.Traitor)
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

        float radius = OptionGroupSingleton<GardenerOptions>.Instance.Radius * 5f;
        float duration = OptionGroupSingleton<GardenerOptions>.Instance.Duration;

        GardenerSystem.SetGarden(player.PlayerId, position, radius, duration);
    }


    [MethodRpc((uint)ExtensionRpc.GardenerAttackNotify)]
    public static void RpcGardenerAttackNotify(byte ownerId, byte attackerId, bool killed)
    {
        GardenerSystem.HandleAttackNotification(ownerId, attackerId, killed);
    }
}