using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class IcenbergRole(IntPtr cppPtr) : NeutralRole(cppPtr), IExtensionRole, IWikiDiscoverable, IDoomable, ICrewVariant
{
    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => "Icenberg";
    public RoleBehaviour CrewVariant => CrewVariants.Of<TownOfUs.Roles.Crewmate.WardenRole>();
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
    public bool HasImpostorVision => true;

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => [typeof(IcenbergKillButton), typeof(IcenbergFreezeButton), typeof(IcenbergBlizzardButton)];

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = OptionGroupSingleton<IcenbergOptions>.Instance.CanVent,
        UseVanillaKillButton = false,
        Icon = TouExtensionIcons.IcenbergRoleIcon,
        IntroSound = TouExtensionAudio.IcenbergIntroSound,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>(),
        OptionsScreenshot = TouBanners.NeutralRoleBanner,
    };

    /// <summary>
    /// Solo neutral-killer win, the TOU-Mira 1.7.3 way (see GlitchRole): without it the interface default is
    /// false and a lobby down to the Icenberg and its victims never ended.
    /// </summary>
    public bool WinConditionMet()
    {
        var icenbergs = CustomRoleUtils.GetActiveRolesOfType<IcenbergRole>().Count(x => !x.Player.HasDied());
        if (icenbergs == 0 || MiscUtils.KillersAliveCount > icenbergs || MiscUtils.KillersAliveCount == 0)
        {
            return false;
        }

        return icenbergs >= MiscUtils.GetImpactfulLivingPlayers().Count - icenbergs;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        return WinConditionMet();
    }

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
        if (icenberg == null || target == null || target.HasDied() || target.HasModifier<IcenbergFrozenModifier>())
        {
            return;
        }

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
        if (icenberg == null)
        {
            return;
        }

        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc == null || pc.HasDied() || pc.PlayerId == icenberg.PlayerId)
            {
                continue;
            }

            // A second blizzard while the first still blows only restarts its clock.
            if (pc.TryGetModifier<IcenbergBlizzardModifier>(out var current))
            {
                current.ResetTimer();
                current.StartTimer();
                continue;
            }

            pc.AddModifier(new IcenbergBlizzardModifier(duration));
        }
    }
}
