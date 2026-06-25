using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using Reactor.Utilities.Extensions;
using Reactor.Utilities;
using System.Collections;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Networking;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Modules;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Impostor;

public sealed class KamikazeRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Relentless;
    public string LocaleKey => "Kamikaze";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") +
               MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => Palette.ImpostorRed;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorKilling;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = true,
        Icon = TouExtensionIcons.KamikazeRole,
        CanUseVent = OptionGroupSingleton<KamikazeOptions>.Instance.CanVent,
        OptionsScreenshot = TouBanners.ImpostorRoleBanner,
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities
    {
        get
        {
            return
            [
                new(
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}Suicide", "Suicide"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}SuicideWikiDescription"),
                    TouExtensionImpAssets.KamikazeSuicideButtonSprite),
            ];
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

    private static GameObject? CreateRadiusSphere(Vector3 pos, float radius, float alpha = 1f)
    {
        var sphere = MiscUtils.CreateSpherePrimitive(pos, radius);
        var meshRenderer = sphere.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            var mat = new Material(AuAvengersAnims.IgniteMaterial.LoadAsset());
            var color = mat.color;
            color.a = alpha;
            mat.color = color;
            meshRenderer.material = mat;
        }

        return sphere;
    }

    [MethodRpc((uint)ExtensionRpc.KamikazeDetonate)]
    public static void RpcKamikazeDetonate(PlayerControl kamikaze)
    {
        if (LobbyBehaviour.Instance)
        {
            return;
        }

        if (!kamikaze.IsRole<KamikazeRole>())
        {
            return;
        }

        var opts = OptionGroupSingleton<KamikazeOptions>.Instance;
        var radius = opts.DetonateRadius * ShipStatus.Instance.MaxLightRadius;
        var pos = kamikaze.transform.position;
        var localPlayer = PlayerControl.LocalPlayer;

        if (localPlayer != null && localPlayer.IsImpostorAligned())
        {
            var sphere = CreateRadiusSphere(pos, opts.DetonateRadius, alpha: 0.35f);
            Coroutines.Start(CoDestroySphere(sphere));
        }

        if (localPlayer != null)
        {
            var isKamikaze = localPlayer.PlayerId == kamikaze.PlayerId;
            var inRadius = Vector2.Distance((Vector2)pos, (Vector2)localPlayer.transform.position) <= radius;

            if (!opts.DisableExplosionSound && (isKamikaze || inRadius))
            {
                TouAudio.PlaySound(TouExtensionAudio.KamikazeExplodeSound);
            }
        }

        if (!AmongUsClient.Instance.AmHost)
        {
            return;
        }

        if (MeetingHud.Instance || ExileController.Instance)
        {
            return;
        }

        var maxKills = (int)opts.MaxKills;

        JokerCloneSystem.TriggerClonesInRadius(kamikaze, pos, radius);

        var victims = PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p != null
                        && !p.HasDied()
                        && p.PlayerId != kamikaze.PlayerId
                        && !p.HasModifier<InvulnerabilityModifier>()
                        && Vector2.Distance((Vector2)pos, (Vector2)p.transform.position) <= radius)
            .OrderBy(p => Vector2.Distance((Vector2)pos, (Vector2)p.transform.position))
            .Take(maxKills)
            .ToList();

        Coroutines.Start(CoProcessKills(kamikaze, victims));
    }

    [HideFromIl2Cpp]
    private static IEnumerator CoProcessKills(PlayerControl kamikaze, List<PlayerControl> victims)
    {
        foreach (var victim in victims.Where(v => v != null && !v.HasDied()))
        {
            kamikaze.RpcSpecialMurder(
                victim,
                createDeadBody: true,
                teleportMurderer: false,
                showKillAnim: false,
                playKillSound: false,
                causeOfDeath: "Seppuku");

            yield return null;
            yield return null;
        }

        yield return null;
        yield return null;

        if (kamikaze != null && !kamikaze.HasDied())
        {
            kamikaze.RpcSpecialMurder(
                kamikaze,
                createDeadBody: true,
                teleportMurderer: false,
                showKillAnim: false,
                playKillSound: false,
                causeOfDeath: "Seppuku");
        }
    }

    [HideFromIl2Cpp]
    private static IEnumerator CoDestroySphere(GameObject? sphere)
    {
        yield return new WaitForSeconds(0.5f);
        sphere?.Destroy();
    }
}
