using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TouMegaChujoweExtension.Roles.Neutral;

public sealed class ZombieRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable
{
    public string LocaleKey => "Zombie";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Zombie");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Zombie;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom; // Using Custom team for Horde
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralEvil;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = false,
        CanUseVent = OptionGroupSingleton<ZombieOptions>.Instance.CanVent,
        Icon = TouRoleIcons.Vampire, // Placeholder
        IntroSound = TouAudio.VampIntroSound
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(TouLocale.Get("ExtensionRoleZombieBite", "Bite"),
            TouLocale.GetParsed("ExtensionRoleZombieBiteWikiDescription"),
            TouRoleIcons.Vampire)
    ];

    public int MeetingCount { get; set; } = 0;

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        if (Player.AmOwner)
        {
            HudManager.Instance.ImpostorVentButton.graphic.sprite = TouNeutAssets.VampVentSprite.LoadAsset();
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        if (Player.AmOwner)
        {
            HudManager.Instance.ImpostorVentButton.graphic.sprite = TouAssets.VentSprite.LoadAsset();
        }
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player)) return false;
        var console = usable.TryCast<Console>();
        return console == null || console.AllowImpostor;
    }

    [MethodRpc((uint)ExtensionRpc.ZombieConvert)]
    public static void RpcZombieConvert(PlayerControl player, PlayerControl target)
    {
        if (player.Data.Role is not ZombieRole) return;
        
        target.RpcChangeRole(RoleId.Get<ZombieRole>());
        target.AddModifier<ZombieModifier>();
    }

    [MethodRpc((uint)ExtensionRpc.ZombieHordeChat)]
    public static void RpcSendHordeChat(PlayerControl player, string message)
    {
        // Handle Horde private chat
        if (!OptionGroupSingleton<ZombieOptions>.Instance.PrivateChat) return;
        
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc != null && pc.IsRole<ZombieRole>())
            {
                // Show message only to zombies
            }
        }
    }
}
