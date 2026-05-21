using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using Reactor.Utilities.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Modifiers;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using MiraAPI.Utilities;
using UnityEngine;
using System;
using System.Collections.Generic;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs.Roles.Neutral;
using TouMegaChujoweExtension.Buttons.Neutral;
using TownOfUs.Buttons;
using TownOfUs;
using TouMegaChujoweExtension;
using TouMegaChujoweExtension.Modifiers;
using MiraAPI.Modifiers;

namespace TouMegaChujoweExtension.Roles.Neutral;

public sealed class IcenbergRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => "Icenberg";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Icenberg");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Icenberg;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralKilling;

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = OptionGroupSingleton<IcenbergOptions>.Instance.CanVent,
        Icon = TouExtensionIcons.IcenbergRoleIcon,
        IntroSound = TouAudio.ChefSound,
    };

    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed("ExtensionRoleIcenbergFreeze", "Freeze"),
            TouLocale.GetParsed("ExtensionRoleIcenbergFreezeWikiDescription"),
            TouExtensionNeuAssets.FreezeButtonSprite),
        new(
            TouLocale.GetParsed("ExtensionRoleIcenbergBlizzard", "Blizzard"),
            TouLocale.GetParsed("ExtensionRoleIcenbergBlizzardWikiDescription"),
            TouExtensionNeuAssets.BlizzardButtonSprite)
    ];

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player)) return false;

        var vent = usable.TryCast<Vent>();
        if (vent != null)
        {
            return OptionGroupSingleton<IcenbergOptions>.Instance.CanVent;
        }

        var console = usable.TryCast<Console>();
        return console == null || console.AllowImpostor;
    }

    [MethodRpc((uint)ExtensionRpc.IcenbergFreeze)]
    public static void RpcFreeze(PlayerControl icenberg, PlayerControl target, float duration)
    {
        if (target == null) return;

        // Apply frozen modifier
        target.AddModifier<IcenbergFrozenModifier>(duration);

        if (target.AmOwner)
        {
            var msg = TouLocale.Get("ExtensionRoleIcenbergFrozenNotification", "You have been frozen!");
            var notif = Helpers.CreateAndShowNotification(
                msg,
                TouExtensionColors.Icenberg,
                new Vector3(0f, 1f, -20f),
                spr: TouMegaChujoweExtension.Assets.TouExtensionNeuAssets.FreezeButtonSprite.LoadAsset());
            notif.AdjustNotification();
        }
    }

    [MethodRpc((uint)ExtensionRpc.IcenbergBlizzard)]
    public static void RpcBlizzard(PlayerControl icenberg, float duration)
    {
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc != null && !pc.HasDied() && pc.PlayerId != icenberg.PlayerId)
            {
                pc.AddModifier(new IcenbergBlizzardModifier(duration));
            }
        }
    }
}
