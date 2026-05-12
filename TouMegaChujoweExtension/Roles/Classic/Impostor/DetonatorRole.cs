using System;
using System.Collections.Generic;
using System.Collections;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TouMegaChujoweExtension.Assets;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;
using System;
using System.Collections.Generic;
using TouMegaChujoweExtension.Buttons.Impostor;

namespace TouMegaChujoweExtension.Roles.Impostor;

public sealed class DetonatorRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Relentless;
    public string LocaleKey => "Detonator";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Detonator");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Detonator;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorKilling;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = true,
        Icon = TouExtensionIcons.DetonatorRoleIcon, 
        IntroSound = TouAudio.ArsoIgniteSound,
        CanUseVent = OptionGroupSingleton<DetonatorOptions>.Instance.CanVent
    };

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => new List<Type> { typeof(DetonatorAttachButton) };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed("ExtensionRoleDetonatorAttach", "Attach"),
            TouLocale.GetParsed("ExtensionRoleDetonatorAttachWikiDescription"),
            TouExtensionImpAssets.DetonatorAttachSprite),
        new(
            TouLocale.GetParsed("ExtensionRoleDetonatorDetonate", "Detonate"),
            TouLocale.GetParsed("ExtensionRoleDetonatorDetonateWikiDescription"),
            TouExtensionImpAssets.DetonatorDetonateSprite)
    ];

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    [MethodRpc((uint)ExtensionRpc.DetonatorAttach)]
    public static void RpcAttachBomb(PlayerControl detonator, PlayerControl target)
    {
        if (target == null || target.HasDied()) return;

        DetonatorSystem.AttachBomb(detonator.PlayerId, target.PlayerId);
    }

    [MethodRpc((uint)ExtensionRpc.DetonatorDetonate)]
    public static void RpcDetonate(PlayerControl detonator)
    {
        DetonatorSystem.ManualDetonate(detonator.PlayerId);
    }

    [MethodRpc((uint)ExtensionRpc.DetonatorShowEffect)]
    public static void RpcShowDetonationEffect(PlayerControl sender, Vector2 position, float radius)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null) return;

        // Show only to Impostors and dead players
        if (local.Data.Role.IsImpostor || local.Data.IsDead)
        {
            // Create premium sphere like Kamikaze/RC-XD
            var sphere = CreateRadiusSphere(position, radius, 0.35f);
            
            // Add a brief bomb sprite at the center for extra feedback
            var bombGo = new GameObject("DetonatorVisual");
            bombGo.transform.position = new Vector3(position.x, position.y, position.y / 1000f - 0.1f);
            var renderer = bombGo.AddComponent<SpriteRenderer>();
            renderer.sprite = TouExtensionImpAssets.DetonatorDetonateSprite.LoadAsset();
            renderer.color = new Color(1f, 0.2f, 0.2f, 0.8f); // Reddish bomb
            renderer.transform.localScale = Vector3.one * 0.7f;

            Coroutines.Start(CoDestroyObjAfter(sphere, 0.6f));
            Coroutines.Start(CoDestroyObjAfter(bombGo, 0.6f));
        }
    }

    private static GameObject? CreateRadiusSphere(Vector3 pos, float radius, float alpha = 1f)
    {
        var sphere = MiscUtils.CreateSpherePrimitive(pos, radius);
        var meshRenderer = sphere.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            var mat = new Material(AuAvengersAnims.IgniteMaterial.LoadAsset());
            var color = Color.red; // Detonator is red
            color.a = alpha;
            mat.color = color;
            meshRenderer.material = mat;
        }

        return sphere;
    }

    [MethodRpc((uint)ExtensionRpc.DetonatorPlayBeep)]
    public static void RpcPlayBeep(PlayerControl victim, byte detonatorId, float volume)
    {
        if (PlayerControl.LocalPlayer == null || victim == null) return;
        
        var clip = TouExtensionAudio.C4Beep.LoadAsset();
        if (clip == null) return;

        var local = PlayerControl.LocalPlayer;
        bool isDetonator = local.PlayerId == detonatorId;
        bool isVictim = local.PlayerId == victim.PlayerId;

        // Detonator does not hear the beep
        if (isDetonator) return;

        // Victim always hears it "normally"
        if (isVictim)
        {
            SoundManager.Instance.PlaySound(clip, false, volume);
            return;
        }

        // For others, only audible if VERY close (e.g. 2.0 units)
        float dist = Vector2.Distance(local.transform.position, victim.transform.position);
        if (dist < 3.0f)
        {
            float spatialVol = Mathf.Clamp01(1f - (dist / 3.0f)) * volume * 0.5f;
            SoundManager.Instance.PlaySound(clip, false, spatialVol);
        }
    }

    private static IEnumerator CoDestroyObjAfter(GameObject? obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null) obj.Destroy();
    }
}
