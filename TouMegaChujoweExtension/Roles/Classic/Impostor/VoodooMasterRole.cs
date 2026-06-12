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
using Reactor.Utilities;
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
    public int BlindUsesLeft { get; set; }
    public int ConfuseUsesLeft { get; set; }
    public int MuteUsesLeft { get; set; }

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = true,
        Icon = TouExtensionIcons.VoodooRoleIcon,
        IntroSound = TouAudio.GlitchSound,
        CanUseVent = true,
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
        ResetCurseUses();
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    public void ResetCurseUses()
    {
        var options = OptionGroupSingleton<VoodooMasterOptions>.Instance;
        BlindUsesLeft = (int)options.MaxBlindCurses;
        ConfuseUsesLeft = (int)options.MaxConfuseCurses;
        MuteUsesLeft = (int)options.MaxMuteCurses;
    }

    public int GetMaxUses(VoodooEffect effect)
    {
        var options = OptionGroupSingleton<VoodooMasterOptions>.Instance;
        return effect switch
        {
            VoodooEffect.Mute => (int)options.MaxMuteCurses,
            VoodooEffect.Confuse => (int)options.MaxConfuseCurses,
            _ => (int)options.MaxBlindCurses
        };
    }

    public int GetUsesLeft(VoodooEffect effect)
    {
        return effect switch
        {
            VoodooEffect.Mute => MuteUsesLeft,
            VoodooEffect.Confuse => ConfuseUsesLeft,
            _ => BlindUsesLeft
        };
    }

    public bool TrySpendUse(VoodooEffect effect)
    {
        if (GetMaxUses(effect) == 0)
        {
            return true;
        }

        switch (effect)
        {
            case VoodooEffect.Mute when MuteUsesLeft > 0:
                MuteUsesLeft--;
                return true;
            case VoodooEffect.Confuse when ConfuseUsesLeft > 0:
                ConfuseUsesLeft--;
                return true;
            case VoodooEffect.Blindness when BlindUsesLeft > 0:
                BlindUsesLeft--;
                return true;
            default:
                return false;
        }
    }

    private static System.Collections.IEnumerator CoApplyBlindAfterDelay(PlayerControl voodooMaster, PlayerControl target, float duration, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (MeetingHud.Instance == null && target != null && !target.HasDied() && voodooMaster != null && !voodooMaster.HasDied())
        {
            foreach (var player in PlayerControl.AllPlayerControls.ToArray())
            {
                if (player.HasModifier<VoodooBlindModifier>())
                {
                    player.RpcRemoveModifier<VoodooBlindModifier>();
                }
            }
            target.RpcAddModifier<VoodooBlindModifier>(voodooMaster, duration);
        }
    }

    private static System.Collections.IEnumerator CoApplyConfuseAfterDelay(PlayerControl voodooMaster, PlayerControl target, float duration, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (MeetingHud.Instance == null && target != null && !target.HasDied() && voodooMaster != null && !voodooMaster.HasDied())
        {
            if (target.HasModifier<VoodooConfusedModifier>())
            {
                target.RpcRemoveModifier<VoodooConfusedModifier>();
            }
            target.RpcAddModifier<VoodooConfusedModifier>(voodooMaster, duration);
        }
    }

    public static void CastVoodooDoll(PlayerControl voodooMaster, PlayerControl target, VoodooEffect effect)
    {
        if (voodooMaster == null || target == null || target.HasDied())
        {
            return;
        }

        var options = OptionGroupSingleton<VoodooMasterOptions>.Instance;
        var lockRounds = (int)options.TargetLockDurationRounds;
        if (lockRounds > 0 && !voodooMaster.HasModifier<VoodooTargetLockModifier>())
        {
            voodooMaster.RpcAddModifier<VoodooTargetLockModifier>(target.PlayerId, lockRounds);
        }

        switch (effect)
        {
            case VoodooEffect.Blindness:
                if (options.EclipseDelay > 0f)
                {
                    Coroutines.Start(CoApplyBlindAfterDelay(voodooMaster, target, options.BlindDuration, options.EclipseDelay));
                }
                else
                {
                    foreach (var player in PlayerControl.AllPlayerControls.ToArray())
                    {
                        if (player.HasModifier<VoodooBlindModifier>())
                        {
                            player.RpcRemoveModifier<VoodooBlindModifier>();
                        }
                    }
                    target.RpcAddModifier<VoodooBlindModifier>(voodooMaster, options.BlindDuration);
                }
                return;
            case VoodooEffect.Confuse:
                if (options.ConfuseDelay > 0f)
                {
                    Coroutines.Start(CoApplyConfuseAfterDelay(voodooMaster, target, options.ConfuseDuration, options.ConfuseDelay));
                }

                else
                {
                    if (target.HasModifier<VoodooConfusedModifier>())
                    {
                        target.RpcRemoveModifier<VoodooConfusedModifier>();
                    }
                    target.RpcAddModifier<VoodooConfusedModifier>(voodooMaster, options.ConfuseDuration);
                }
                return;
        }

        foreach (var player in PlayerControl.AllPlayerControls.ToArray())
        {
            if (player.HasModifier<VoodooScheduledCurseModifier>())
            {
                player.RpcRemoveModifier<VoodooScheduledCurseModifier>();
            }

            if (player.HasModifier<VoodooMutedModifier>())
            {
                player.RpcRemoveModifier<VoodooMutedModifier>();
            }
        }

        target.RpcAddModifier<VoodooScheduledCurseModifier>(effect);
    }
}
