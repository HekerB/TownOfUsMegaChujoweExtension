using MiraAPI.GameOptions;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using MiraAPI.Patches.Stubs;
using System;
using System.Collections.Generic;
using TouMegaChujoweExtension.Buttons.Classic.Impostor;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;
using Il2CppInterop.Runtime.Attributes;

namespace TouMegaChujoweExtension.Roles.Classic.Impostor;

public sealed class TomahawkRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable
{
    public string LocaleKey => "Tomahawk";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Tomahawk");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Tomahawk;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorKilling;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = true,
        Icon = TouExtensionIcons.TomahawkRoleIcon, 
        IntroSound = TownOfUs.Assets.TouAudio.ImpostorIntroSound,
        CanUseVent = OptionGroupSingleton<TomahawkOptions>.Instance.CanVent
    };

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => new List<Type> { typeof(TomahawkThrowButton) };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}Throw", "Throw"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}ThrowWikiDescription"),
            TouExtensionIcons.TomahawkRoleIcon)
    ];

    public bool IsAiming { get; set; }
    public float AimTimer { get; set; }
    private bool _justActivatedAim;

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        IsAiming = false;
        _justActivatedAim = false;
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        IsAiming = false;
        _justActivatedAim = false;
    }

    public void Update()
    {
        if (Player == null || Player.Data == null || Player.Data.IsDead)
        {
            if (IsAiming) DeactivateAim();
            return;
        }

        if (IsAiming)
        {
            if (_justActivatedAim)
            {
                _justActivatedAim = false;
                return;
            }
            AimTimer -= Time.deltaTime;
            if (AimTimer <= 0f)
            {
                DeactivateAim();
                return;
            }

            if (Input.GetMouseButtonDown(0)) // Left click
            {
                if (Camera.main != null)
                {
                    var mouseWorld = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    var direction = (mouseWorld - Player.GetTruePosition()).normalized;
                    RpcThrowTomahawk(Player, direction);
                }
                DeactivateAim();
                
                if (TomahawkThrowButton.Instance != null)
                {
                    TomahawkThrowButton.Instance.Timer = TomahawkThrowButton.Instance.Cooldown;
                }
            }
        }
    }

    [HideFromIl2Cpp]
    public void ActivateAim()
    {
        if (IsAiming) return;
        IsAiming = true;
        AimTimer = 5f;
        _justActivatedAim = true;
        
        MiraAPI.Utilities.Helpers.CreateAndShowNotification(
            "Tomahawk Aim Active! Left click to throw.", 
            Color.white, 
            new Vector3(0f, 1f, -20f), 
            spr: TouExtensionIcons.TomahawkRoleIcon.LoadAsset())?.AdjustNotification();
            
        if (HudManager.Instance != null && HudManager.Instance.ShadowQuad != null)
        {
            HudManager.Instance.ShadowQuad.gameObject.SetActive(false);
        }
    }

    [HideFromIl2Cpp]
    public void DeactivateAim()
    {
        IsAiming = false;
        if (HudManager.Instance != null && HudManager.Instance.ShadowQuad != null)
        {
            HudManager.Instance.ShadowQuad.gameObject.SetActive(Player != null && !Player.Data.IsDead);
        }
    }

    [Reactor.Networking.Attributes.MethodRpc((uint)Networking.ExtensionRpc.TomahawkThrow)]
    public static void RpcThrowTomahawk(PlayerControl sender, Vector2 direction)
    {
        TomahawkSystem.ThrowTomahawk(sender, direction);
    }
}
