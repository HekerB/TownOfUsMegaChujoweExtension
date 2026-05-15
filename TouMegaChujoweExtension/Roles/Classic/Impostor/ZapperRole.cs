using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TouMegaChujoweExtension.Networking;
using Reactor.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TownOfUs.Interfaces;

namespace TouMegaChujoweExtension.Roles.Impostor;

public sealed class ZapperRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Relentless;
    public string LocaleKey => "Zapper";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Zapper");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");
    public Color RoleColor => Palette.ImpostorRed;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorKilling;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = true,
        Icon = TouExtensionIcons.ZapperRoleIcon,
        IntroSound = TouExtensionAudio.ElectricitySound,
        CanUseVent = OptionGroupSingleton<ZapperOptions>.Instance.CanVent
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed("ExtensionRoleZapperZap", "Zap"),
            TouLocale.GetParsed("ExtensionRoleZapperZapWikiDescription"),
            TouExtensionIcons.ZapperRoleIcon)
    ];

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    [MethodRpc((uint)ExtensionRpc.ZapperZap)]
    public static void RpcZap(PlayerControl zapper, PlayerControl target)
    {
        if (zapper == null || target == null) return;

        var options = OptionGroupSingleton<ZapperOptions>.Instance;
        int maxJumps = (int)options.MaxJumps;
        float radius = options.Radius * ShipStatus.Instance.MaxLightRadius;

        Coroutines.Start(CoZapChain(zapper, target, maxJumps, radius));
    }

    private static IEnumerator CoZapChain(PlayerControl zapper, PlayerControl target, int remainingJumps, float radius)
    {
        var currentTarget = target;
        var jumpsLeft = remainingJumps;
        var excluded = new List<PlayerControl> { zapper };

        while (currentTarget != null && !currentTarget.HasDied())
        {
            currentTarget.AddModifier(new ZapperZapModifier());
            
            yield return new WaitForSeconds(1f);

            if (zapper == null || zapper.HasDied()) yield break;
            if (currentTarget == null || currentTarget.HasDied()) yield break;

            // Kill the current target
            zapper.RpcSpecialMurder(
                currentTarget,
                createDeadBody: true,
                teleportMurderer: false,
                showKillAnim: true,
                playKillSound: true,
                causeOfDeath: "Conducted"
            );

            excluded.Add(currentTarget);
            jumpsLeft--;

            if (jumpsLeft <= 0) yield break;

            // Find next target
            var next = GetNearestPlayerInRadius(currentTarget.transform.position, radius, excluded.ToArray());
            if (next == null) yield break;

            currentTarget = next;
        }
    }

    private static PlayerControl? GetNearestPlayerInRadius(Vector2 position, float radius, PlayerControl[] exclude)
    {
        return PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p != null && !p.HasDied() && !exclude.Contains(p))
            .Where(p => Vector2.Distance(p.transform.position, position) <= radius)
            .OrderBy(p => Vector2.Distance(p.transform.position, position))
            .FirstOrDefault();
    }
}
