using System.Collections;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Networking;
using TouMegaChujoweExtension.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Impostor;
using TownOfUs.Utilities;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Reactor.Utilities;

namespace TouMegaChujoweExtension.Roles.Classic.Impostor;

public sealed class SandwormRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable
{
    public string LocaleKey => "Sandworm";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Sandworm");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Sandworm;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorConcealing;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouExtensionIcons.SandwormRoleIcon,
        OptionsScreenshot = TouBanners.ImpostorRoleBanner
    };

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => new List<Type> { typeof(SandwormDigButton) };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(TouLocale.GetParsed("ExtensionRoleSandwormDig", "Dig"),
            TouLocale.GetParsed("ExtensionRoleSandwormDigWikiDescription"),
            TouExtensionImpAssets.SandwormDigButtonSprite)
    ];

    public bool IsUnderground { get; set; }
    public bool IsDigging { get; set; }
    public float DigEndTime { get; set; }
    public float EmergeTime { get; set; }
    public Vector2? PlacedVentPosition { get; set; }
    public int NextVentIndex { get; set; }

    [HideFromIl2Cpp]
    public Vent? FirstVent { get; set; }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        IsUnderground = false;
        IsDigging = false;
        PlacedVentPosition = null;
        FirstVent = null;
        NextVentIndex = 0;
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        TouMegaChujoweExtension.Modules.SandwormSystem.Reset();
    }

    public void Update()
    {
        if (Player == null || !Player.AmOwner || !IsDigging) return;

        if (Time.time >= DigEndTime)
        {
            SandwormRole.RpcEmerge(Player, Player.GetTruePosition());
        }
    }



    [MethodRpc((uint)ExtensionRpc.SandwormUnderground)]
    public static void RpcUnderground(PlayerControl player, Vector2 position)
    {
        if (!player.IsRole<SandwormRole>()) return;
        var role = player.GetRole<SandwormRole>();
        if (role == null || role.IsUnderground) return; // Prevent double trigger

        role.IsUnderground = true;
        role.IsDigging = true;
        role.DigEndTime = Time.time + OptionGroupSingleton<SandwormOptions>.Instance.DigDuration;
        
        // Spawn first vent with a unique ID based on how many times player has dug
        var ventId = 5000 + player.PlayerId * 100 + role.NextVentIndex * 2;
        var vent1 = TouMegaChujoweExtension.Modules.SandwormSystem.SpawnVent(player, ventId, position);
        role.FirstVent = vent1;

        player.AddModifier<SandwormInvisibleModifier>();
        player.AddModifier<SandwormSpeedModifier>(OptionGroupSingleton<SandwormOptions>.Instance.UndergroundSpeed);

        if (player.AmOwner && player.MyPhysics != null)
        {
            player.MyPhysics.RpcEnterVent(ventId);
        }
    }

    [MethodRpc((uint)ExtensionRpc.SandwormEmerge)]
    public static void RpcEmerge(PlayerControl player, Vector2 position)
    {
        if (!player.IsRole<SandwormRole>()) return;
        var role = player.GetRole<SandwormRole>();
        if (role == null || !role.IsUnderground) return;

        role.IsUnderground = false;
        role.IsDigging = false;
        
        // Spawn second vent with a unique ID
        var ventId = 5000 + player.PlayerId * 100 + role.NextVentIndex * 2 + 1;
        var vent2 = TouMegaChujoweExtension.Modules.SandwormSystem.SpawnVent(player, ventId, position);
        role.NextVentIndex++;

        // Link them bidirectionally!
        var vent1 = role.FirstVent;
        if (vent1 != null)
        {
            vent1.Left = vent2;
            vent1.Right = vent2;
            vent2.Left = vent1;
            vent2.Right = vent1;
        }

        player.RemoveModifier<SandwormInvisibleModifier>();
        player.RemoveModifier<SandwormSpeedModifier>();

        if (player.AmOwner)
        {
            role.EmergeTime = Time.time;

            if (player.MyPhysics != null)
            {
                player.MyPhysics.RpcExitVent(ventId);
            }

            if (TouMegaChujoweExtension.Buttons.Classic.Impostor.SandwormDigButton.Instance != null)
            {
                TouMegaChujoweExtension.Buttons.Classic.Impostor.SandwormDigButton.Instance.Timer = TouMegaChujoweExtension.Buttons.Classic.Impostor.SandwormDigButton.Instance.Cooldown;
            }
        }
    }
}
