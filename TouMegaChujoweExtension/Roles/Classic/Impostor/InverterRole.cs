using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Buttons.Classic.Impostor;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using MiraAPI.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Impostor;

public sealed class InverterRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable
{
    public string LocaleKey => "Inverter";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Inverter");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => Palette.ImpostorRed;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorSupport;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = true,
        Icon = TouRoleIcons.Hypnotist,
        IntroSound = TouAudio.ImpostorIntroSound,
        CanUseVent = OptionGroupSingleton<InverterOptions>.Instance.CanVent
    };

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => new List<Type> { typeof(InverterDisorientButton) };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(TouLocale.GetParsed("ExtensionRoleInverterDisorient", "Disorient"),
            TouLocale.GetParsed("ExtensionRoleInverterDisorientWikiDescription"),
            TouRoleIcons.Hypnotist)
    ];

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    [MethodRpc((uint)ExtensionRpc.InverterDisorient)]
    public static void RpcDisorient(PlayerControl inverter, PlayerControl victim)
    {
        if (victim == null) return;
        var options = OptionGroupSingleton<InverterOptions>.Instance;
        victim.AddModifier<InverterDisorientedModifier>(options.DisorientDuration);

        if (victim.AmOwner)
        {
            // Flash the screen to signal the disorientation
            TouMegaChujoweExtension.Modules.PirateDuelSystem.FlashScreen(Palette.ImpostorRed, 0.5f, 0.3f);
        }
    }
}
