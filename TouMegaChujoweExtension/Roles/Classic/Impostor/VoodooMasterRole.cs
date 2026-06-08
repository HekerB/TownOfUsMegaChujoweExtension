using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Buttons.Classic.Impostor;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Impostor;

public enum VoodooEffect
{
    Blindness,
    Mute,
    Confuse
}

public sealed class VoodooMasterRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Trickster;
    public string LocaleKey => "VoodooMaster";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Voodoo Master");
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
        Icon = TouExtensionIcons.VoodooRoleIcon,
        IntroSound = TouAudio.GlitchSound,
        CanUseVent = OptionGroupSingleton<VoodooMasterOptions>.Instance.CanVent,
        OptionsScreenshot = TouBanners.ImpostorRoleBanner,
    };

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => [typeof(VoodooDollButton), typeof(VoodooCycleButton)];

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}Cast", "Curse"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}CastWikiDescription"),
            TouImpAssets.BlackmailSprite),
        new(
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}Cycle", "Cycle Curse"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}CycleWikiDescription"),
            TouImpAssets.BlindSprite)
    ];

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    public static void CastVoodooDoll(PlayerControl voodooMaster, PlayerControl target, VoodooEffect effect)
    {
        if (voodooMaster == null || target == null || target.HasDied())
        {
            return;
        }

        switch (effect)
        {
            case VoodooEffect.Blindness:
                if (target.HasModifier<VoodooBlindModifier>())
                {
                    target.RpcRemoveModifier<VoodooBlindModifier>();
                }

                target.RpcAddModifier<VoodooBlindModifier>(voodooMaster, OptionGroupSingleton<VoodooMasterOptions>.Instance.BlindDuration);
                return;
            case VoodooEffect.Confuse:
                if (target.HasModifier<VoodooConfusedModifier>())
                {
                    target.RpcRemoveModifier<VoodooConfusedModifier>();
                }

                target.RpcAddModifier<VoodooConfusedModifier>(voodooMaster, OptionGroupSingleton<VoodooMasterOptions>.Instance.ConfuseDuration);
                return;
        }

        if (target.HasModifier<VoodooScheduledCurseModifier>())
        {
            target.RpcRemoveModifier<VoodooScheduledCurseModifier>();
        }

        target.RpcAddModifier<VoodooScheduledCurseModifier>(effect);
    }
}
