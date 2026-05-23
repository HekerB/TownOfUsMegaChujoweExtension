using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using Reactor.Utilities.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Networking;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Impostor;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Events;
using TownOfUs.Events.TouEvents;
using TouMegaChujoweExtension.Buttons.Classic.Impostor;

namespace TouMegaChujoweExtension.Roles.Classic.Impostor;

public sealed class AstralRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Hunter;
    public string LocaleKey => "Astral";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Astral");
    public string RoleDescription => TouLocale.Get($"ExtensionRole{LocaleKey}IntroBlurb", "Phase through walls and return to your start position.");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription", "Phase through walls and teleport back. You must kill someone to survive!");

    public bool KillMadeDuringPhase { get; set; }

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Astral;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorConcealing;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouExtensionIcons.AstralRoleIcon,
        IntroSound = TouAudio.PhantomIntroSound,
        OptionsScreenshot = TouExtensionBanners.AstralBanner
    };

    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}Phase", "Phase"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}PhaseWikiDescription"),
            TouCrewAssets.RewindSprite),
        new(
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}Materialize", "Materialize"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}MaterializeWikiDescription"),
            TouCrewAssets.RewindSprite)
    ];

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        KillMadeDuringPhase = false;
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }
}
