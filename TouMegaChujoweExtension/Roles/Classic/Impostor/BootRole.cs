using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Buttons.Classic.Impostor;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Impostor;

public sealed class BootRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable
{
    public string LocaleKey => "Boot";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Boot");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Boot;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorSupport;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = true,
        Icon = TouExtensionIcons.BootRoleIcon,
        IntroSound = TownOfUs.Assets.TouAudio.ImpostorIntroSound,
        CanUseVent = OptionGroupSingleton<BootOptions>.Instance.CanVent
    };

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => new List<Type> { typeof(BootButton) };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}Boot", "Boot"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}BootWikiDescription"),
            TouExtensionIcons.BootRoleIcon)
    ];

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    [Reactor.Networking.Attributes.MethodRpc((uint)Networking.ExtensionRpc.BootTeleportBody)]
    public static void RpcTeleportBody(PlayerControl sender, byte bodyId, Vector2 newPos)
    {
        var body = UnityEngine.Object.FindObjectsOfType<DeadBody>().FirstOrDefault(b => b.ParentId == bodyId);
        if (body != null)
        {
            body.transform.position = new Vector3(newPos.x, newPos.y, newPos.y / 1000f);
        }
    }
}
