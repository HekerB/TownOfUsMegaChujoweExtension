using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Buttons.Classic.Impostor;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace TouMegaChujoweExtension.Roles.Impostor;

public sealed class DumperRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable
{
    public string LocaleKey => "Dumper";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Dumper");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Dumper;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorSupport;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = true,
        Icon = TouExtensionIcons.DumperRoleIcon,
    };

    public int MeetingCount { get; set; } = 0;

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => new List<Type> { typeof(DumperDragButton) };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(TouLocale.Get("ExtensionRoleDumperDrag", "Drag/Dump"),
            TouLocale.GetParsed("ExtensionRoleDumperDragWikiDescription"),
            TownOfUs.Assets.TouImpAssets.DragSprite)
    ];

    public byte? DraggingBodyId { get; set; }
    public float? AutoDumpTime { get; set; }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        DraggingBodyId = null;
        AutoDumpTime = null;
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    [Reactor.Networking.Attributes.MethodRpc((uint)ExtensionRpc.DumperPickupBody, LocalHandling = Reactor.Networking.Rpc.RpcLocalHandling.Before)]
    public static void RpcPickupBody(PlayerControl player, byte bodyId)
    {
        DumperSystem.PickupBody(player, bodyId);
    }

    [Reactor.Networking.Attributes.MethodRpc((uint)ExtensionRpc.DumperDropBody, LocalHandling = Reactor.Networking.Rpc.RpcLocalHandling.Before)]
    public static void RpcDropBody(PlayerControl player)
    {
        DumperSystem.DropBody(player);
    }
}
