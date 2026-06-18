using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using Reactor.Utilities;
using System.Collections;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Networking;

namespace TouMegaChujoweExtension.Roles.Classic.Impostor;

public sealed class SniperRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Death;
    public string LocaleKey => "Sniper";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Sniper");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => Palette.ImpostorRed;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorKilling;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = true,
        Icon = TouExtensionIcons.SniperRoleIcon,
        CanUseVent = OptionGroupSingleton<SniperOptions>.Instance.CanVent,
        OptionsScreenshot = TouBanners.ImpostorRoleBanner,
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}Shoot", "Shoot"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}ShootWikiDescription"),
            TouExtensionImpAssets.SniperShootButtonSprite)
    ];

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    [MethodRpc((uint)ExtensionRpc.SniperShoot)]
    public static void RpcSniperShoot(PlayerControl sniper, byte targetId)
    {
        if (sniper == null) return;
        var target = MiscUtils.PlayerById(targetId);
        if (target == null || target.Data.IsDead) return;

        if (sniper.AmOwner)
        {
            bool inMeeting = MeetingHud.Instance != null;
            sniper.RpcSpecialMurder(
                target,
                resetKillTimer: false,
                createDeadBody: !inMeeting,
                teleportMurderer: false,
                showKillAnim: !inMeeting,
                causeOfDeath: "Sniped");
        }
    }

    [MethodRpc((uint)ExtensionRpc.SniperPlaySound)]
    public static void RpcSniperPlaySound(PlayerControl sniper, byte targetId)
    {
        if (sniper == null) return;
        var local = PlayerControl.LocalPlayer;
        if (local == null) return;

        var clip = TouExtensionAudio.SniperShootSound.LoadAsset();
        if (clip == null) return;

        var target = MiscUtils.PlayerById(targetId);
        var sniperPos = sniper.transform.position;
        var targetPos = target != null ? target.transform.position : sniperPos;
        var localPos = local.transform.position;

        const float shooterHearRange = 4f;
        const float bodyHearRange = 1.35f;

        float distToShooter = Vector2.Distance(localPos, sniperPos);
        float distToVictim = Vector2.Distance(localPos, targetPos);

        float shooterVolume = GetRangedVolume(distToShooter, shooterHearRange, 1f);
        float bodyVolume = GetRangedVolume(distToVictim, bodyHearRange, 0.45f);
        float volume = Mathf.Max(shooterVolume, bodyVolume);

        if (volume <= 0f) return;

        if (local.PlayerId == sniper.PlayerId)
        {
            volume = 1f;
        }

        if (!Constants.ShouldPlaySfx()) return;

        var source = SoundManager.Instance.PlaySound(clip, false, Mathf.Clamp(volume, 0.05f, 1f));
        if (source != null)
        {
            Coroutines.Start(CoFadeOutSound(source, clip.length, 0.55f));
        }
    }

    private static float GetRangedVolume(float distance, float range, float maxVolume)
    {
        if (distance >= range) return 0f;
        var t = 1f - (distance / range);
        return t * t * maxVolume;
    }

    private static IEnumerator CoFadeOutSound(AudioSource source, float clipLength, float fadeDuration)
    {
        float waitTime = clipLength - fadeDuration;
        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);

        float startVol = source.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (source != null)
                source.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
            else
                yield break;
            yield return null;
        }

        if (source != null)
            source.Stop();
    }
}
