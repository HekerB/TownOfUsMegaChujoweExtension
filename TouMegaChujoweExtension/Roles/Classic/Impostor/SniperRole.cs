using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Roles;
using MiraAPI.Patches.Stubs;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Buttons.Classic.Impostor;

namespace TouMegaChujoweExtension.Roles.Classic.Impostor;

public sealed class SniperRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable
{
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
        IntroSound = TouAudio.ViperIntroSound,
        CanUseVent = OptionGroupSingleton<SniperOptions>.Instance.CanVent,
        OptionsScreenshot = TouBanners.ImpostorRoleBanner
    };

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => new List<Type> { typeof(SniperButton) };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed("ExtensionRoleSniperSnipe", "Snipe"),
            TouLocale.GetParsed("ExtensionRoleSniperSnipeWikiDescription"),
            TouExtensionIcons.SniperRoleIcon)
    ];

    public bool IsScopeActive { get; set; }
    private bool _justActivatedScope;

    private float _originalLightRadius = 3f;
    private bool _originalDontUnload;
    public float ScopeTimer { get; set; }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        IsScopeActive = false;
        _justActivatedScope = false;
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        IsScopeActive = false;
        _justActivatedScope = false;
    }

    public void Update()
    {
        if (Player == null || !Player.AmOwner || Player.Data.IsDead) return;

        if (IsScopeActive)
        {
            if (_justActivatedScope)
            {
                _justActivatedScope = false;
                return;
            }
            // Countdown shoot window timer
            ScopeTimer -= Time.deltaTime;
            if (ScopeTimer <= 0f)
            {
                // Timeout! End scope and trigger cooldown
                DeactivateScope();
                var sniperBtn = SniperButton.Instance;
                if (sniperBtn != null)
                {
                    sniperBtn.Timer = sniperBtn.Cooldown;
                }
                return;
            }

            // Lock on and listen for shooting clicks!
            if (Input.GetMouseButtonDown(0))
            {
                var target = GetMouseTarget();
                if (target != null)
                {
                    // Fire!
                    var sniperBtn = SniperButton.Instance;
                    if (sniperBtn != null)
                    {
                        RpcFireSniper(Player, target);
                        DeactivateScope();
                        sniperBtn.Timer = sniperBtn.Cooldown;
                    }
                }
            }
        }
    }

    [HideFromIl2Cpp]
    public void ActivateScope()
    {
        IsScopeActive = true;
        var shootWindow = OptionGroupSingleton<SniperOptions>.Instance?.ShootWindow ?? 5f;
        if (shootWindow <= 0f) shootWindow = 5f;
        ScopeTimer = shootWindow;
        _justActivatedScope = true;
        
        // Show scoped notification
        MiraAPI.Utilities.Helpers.CreateAndShowNotification(
            "Sniper Scope Active! Left click a target to shoot.", 
            Color.white, 
            new Vector3(0f, 1f, -20f), 
            spr: TouExtensionIcons.SniperRoleIcon.LoadAsset())?.AdjustNotification();
        
        // Deactivate shadows so sniper can see through walls
        if (HudManager.Instance != null && HudManager.Instance.ShadowQuad != null)
        {
            HudManager.Instance.ShadowQuad.gameObject.SetActive(false);
        }
    }

    [HideFromIl2Cpp]
    public void DeactivateScope()
    {
        IsScopeActive = false;
        
        // Restore shadows if alive
        if (HudManager.Instance != null && HudManager.Instance.ShadowQuad != null)
        {
            HudManager.Instance.ShadowQuad.gameObject.SetActive(Player != null && !Player.Data.IsDead);
        }
    }

    [HideFromIl2Cpp]
    private PlayerControl? GetMouseTarget()
    {
        if (Camera.main == null) return null;
        var mouseWorld = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var maxRange = OptionGroupSingleton<SniperOptions>.Instance?.MaxRange ?? 15f;
        if (maxRange <= 0f) maxRange = 15f;

        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc == null || pc.Data.IsDead || pc.PlayerId == Player.PlayerId) continue;
            if (pc.IsImpostorAligned()) continue; // Do not shoot teammates

            var pos = pc.GetTruePosition();
            var distToSniper = Vector2.Distance(Player.GetTruePosition(), pos);
            if (distToSniper > maxRange) continue;

            var distToMouse = Vector2.Distance(mouseWorld, pos);
            if (distToMouse <= 0.8f) // Hovering near player body
            {
                return pc;
            }
        }

        return null;
    }

    [Reactor.Networking.Attributes.MethodRpc((uint)Networking.ExtensionRpc.SniperShoot)]
    public static void RpcFireSniper(PlayerControl sniper, PlayerControl target)
    {
        if (sniper == null || target == null) return;

        // Visual tracer bullet
        var start = sniper.GetTruePosition();
        var end = target.GetTruePosition();
        DrawTracer(start, end);

        // Sound effect of high-caliber gunshot
        PlayGunshotSound(start);

        // Host performs the murder
        if (AmongUsClient.Instance.AmHost)
        {
            sniper.RpcSpecialMurder(target, createDeadBody: true, teleportMurderer: false, causeOfDeath: "SniperRifle");
        }
    }

    private static void DrawTracer(Vector2 start, Vector2 end)
    {
        var go = new GameObject("BulletTracer");
        var lr = go.AddComponent<LineRenderer>();
        lr.startWidth = 0.06f;
        lr.endWidth = 0.02f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.red;
        lr.endColor = new Color(1f, 0.4f, 0f, 0.4f);
        lr.SetPositions(new Vector3[] { new Vector3(start.x, start.y, -1f), new Vector3(end.x, end.y, -1f) });
        
        UnityEngine.Object.Destroy(go, 0.25f);
    }

    private static void PlayGunshotSound(Vector2 pos)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null) return;

        var listenerPos = (Vector2)(Camera.main?.transform.position ?? Vector3.zero);
        var dist = Vector2.Distance(pos, listenerPos);

        const float maxDist = 20f;
        if (dist <= maxDist)
        {
            var clip = TouExtensionAudio.ShootSound.LoadAsset(); // Rich loud impactful pop sound
            if (clip == null) return;

            var volume = Mathf.Clamp01(1f - (dist / maxDist)) * 1.0f;
            SoundManager.Instance.PlaySound(clip, false, volume);
        }
    }
}
