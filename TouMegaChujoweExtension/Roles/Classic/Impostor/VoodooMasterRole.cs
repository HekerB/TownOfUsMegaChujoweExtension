using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Modifiers.Impostor;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Impostor;

public enum VoodooEffect
{
    Blindness,
    Mute,
    Deafness
}

public sealed class VoodooMasterRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Trickster;
    public string LocaleKey => "VoodooMaster";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => Palette.ImpostorRed;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorSupport;

    public VoodooEffect SelectedEffect { get; set; } = VoodooEffect.Blindness;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = true,
        Icon = TouRoleIcons.Witch,
        IntroSound = TownOfUs.Assets.TouAudio.ScientistIntroSound,
        CanUseVent = OptionGroupSingleton<VoodooMasterOptions>.Instance.CanVent,
        OptionsScreenshot = TouBanners.ImpostorRoleBanner,
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities
    {
        get
        {
            return new List<CustomButtonWikiDescription>
            {
                new(
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}Cast", "Voodoo Doll"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}CastWikiDescription"),
                    TouMegaChujoweExtension.Assets.TouExtensionImpAssets.SpellButtonSprite),
                new(
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}Cycle", "Cycle Curse"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}CycleWikiDescription"),
                    TownOfUs.Assets.TouRoleIcons.Herbalist)
            };
        }
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    [MethodRpc((uint)ExtensionRpc.VoodooDollCast)]
    public static void RpcVoodooDollCast(PlayerControl voodooMaster, PlayerControl target, VoodooEffect effect)
    {
        if (voodooMaster == null || target == null || target.HasDied())
        {
            return;
        }

        if (effect == VoodooEffect.Blindness)
        {
            var duration = OptionGroupSingleton<VoodooMasterOptions>.Instance.BlindDuration;
            target.AddModifier<VoodooBlindModifier>(duration);
        }
        else
        {
            if (target.HasModifier<VoodooScheduledCurseModifier>())
            {
                target.RemoveModifier<VoodooScheduledCurseModifier>();
            }
            target.AddModifier<VoodooScheduledCurseModifier>(effect);
        }
    }
}
