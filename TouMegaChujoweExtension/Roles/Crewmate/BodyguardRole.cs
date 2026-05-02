using System.Text;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Networking;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Utilities;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs.Networking;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Crewmate;

public sealed class BodyguardRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Protective;
    public string LocaleKey => "Bodyguard";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") +
               MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}Guard", "Guard"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}GuardWikiDescription"),
            TouExtensionCrewAssets.GuardButtonSprite),
        new(
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}Backlash", "Backlash"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}BacklashWikiDescription"),
            TouExtensionCrewAssets.BacklashButtonSprite),
        new(
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}Kill", "Kill"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}KillWikiDescription"),
            TouExtensionCrewAssets.GuardButtonSprite)
    ];

    public Color RoleColor => TouExtensionColors.Bodyguard;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateProtective;

    public CustomRoleConfiguration Configuration => new(this)
    {
        IntroSound = TownOfUs.Assets.TouAudio.GuardianAngelSound,
        Icon = TouExtensionIcons.BodyguardRoleIcon,
        OptionsScreenshot = TouExtensionBanners.BodyguardBanner,
    };

    [HideFromIl2Cpp] public PlayerControl? Guarded { get; set; }
    [HideFromIl2Cpp] public PlayerControl? LastAttacker { get; set; }

    private bool _backlashReady;
    [HideFromIl2Cpp]
    public bool BacklashReady
    {
        get => _backlashReady;
        set
        {
            if (_backlashReady && !value)
            {
                // ($"[BG-Role] BacklashReady CLEARED! Caller:\n{System.Environment.StackTrace}");
            }
            _backlashReady = value;
        }
    }

    [HideFromIl2Cpp] public float BacklashTimer { get; set; }
    [HideFromIl2Cpp] public bool KillModeActive { get; set; }
    [HideFromIl2Cpp] public float KillModeTimer { get; set; }
    [HideFromIl2Cpp] public bool MarkedAttackerDot { get; set; }

    public override bool IsAffectedByComms => false;

    public void FixedUpdate()
    {
        if (Player == null || Player.Data.Role is not BodyguardRole)
        {
            return;
        }

        if (Guarded != null && Guarded.HasDied())
        {
            // ("[BG-Role] Guarded player died - calling Clear()");
            Clear();
        }

        if (BacklashReady && BacklashTimer > 0f)
        {
            BacklashTimer -= Time.fixedDeltaTime;
            if (BacklashTimer <= 0f)
            {
                // ("[BG-Role] BacklashTimer expired");
                BacklashReady = false;
                LastAttacker = null;
                MarkedAttackerDot = false;
            }
        }

        if (KillModeActive && KillModeTimer > 0f)
        {
            KillModeTimer -= Time.fixedDeltaTime;
            if (KillModeTimer <= 0f)
            {
                RemoveBacklashArrow();

                KillModeActive = false;
                KillModeTimer = 0f;
                LastAttacker = null;
                MarkedAttackerDot = false;

                if (Player.AmOwner && !Player.HasDied())
                {
                    // ("[BG-Role] Kill mode expired - bodyguard dies");
                    Player.RpcSpecialMurder(Player,
                        createDeadBody: true,
                        teleportMurderer: false,
                        showKillAnim: true,
                        playKillSound: false,
                        causeOfDeath: "Bodyguard");
                }
            }
        }
    }

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var sb = ITownOfUsRole.SetNewTabText(this);

        if (Guarded != null)
        {
            sb.AppendLine($"\n<b>{TouLocale.GetParsed("ExtensionRoleBodyguardTabGuarding").Replace("<player>", Guarded.Data.PlayerName)}</b>");
        }

        if (BacklashReady)
        {
            sb.AppendLine($"\n<color=red><b>{TouLocale.Get("ExtensionRoleBodyguardTabBacklashReady")}</b></color>");
        }

        if (KillModeActive)
        {
            sb.AppendLine($"\n<color=red><b>{TouLocale.Get("ExtensionRoleBodyguardTabKillMode")}</b></color>");
        }

        return sb;
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        Clear();
    }

    public override void OnDeath(DeathReason reason)
    {
        RoleBehaviourStubs.OnDeath(this, reason);
        Clear();
    }

    public void Clear()
    {
        // ("[BG-Role] Clear() called");

        RemoveBacklashArrow();

        if (Guarded != null)
        {
            Guarded.RemoveModifier<BodyguardShieldModifier>();
        }

        Guarded = null;
        _backlashReady = false;
        KillModeActive = false;
        LastAttacker = null;
        MarkedAttackerDot = false;
    }

    public void SetGuardedPlayer(PlayerControl? target)
    {
        RemoveBacklashArrow();

        if (Guarded != null)
        {
            Guarded.RemoveModifier<BodyguardShieldModifier>();
        }

        Guarded = target;
        BacklashReady = false;
        KillModeActive = false;
        LastAttacker = null;
        MarkedAttackerDot = false;

        if (target != null && !target.HasModifier<BodyguardShieldModifier>())
        {
            target.AddModifier<BodyguardShieldModifier>(Player);
        }
    }

    public void OnGuardedAttacked(PlayerControl attacker)
    {
        // ($"[BG-Role] OnGuardedAttacked by {attacker?.Data?.PlayerName}");
        LastAttacker = attacker;
        BacklashReady = true;
        BacklashTimer = OptionGroupSingleton<BodyguardOptions>.Instance.BacklashWindow;

        var onlyAttacker = OptionGroupSingleton<BodyguardOptions>.Instance.OnlyTargetAttacker;
        MarkedAttackerDot = onlyAttacker;

        if (Player.AmOwner)
        {
            ShieldUtils.TriggerShieldFlash(Player, ShieldType.Bodyguard);
            
            var notif = Helpers.CreateAndShowNotification(
                TouLocale.Get("ExtensionRoleBodyguardShieldAttacked"),
                TouExtensionColors.Bodyguard,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.BodyguardRoleIcon.LoadAsset());
            notif.AdjustNotification();
        }
    }

    public void ActivateKillMode()
    {
        // ("[BG-Role] ActivateKillMode()");
        BacklashReady = false;
        KillModeActive = true;
        KillModeTimer = OptionGroupSingleton<BodyguardOptions>.Instance.KillWindow;

        if (OptionGroupSingleton<BodyguardOptions>.Instance.ShowBacklashArrow)
        {
            AddBacklashArrow();
        }

        if (Player.AmOwner && Guarded != null)
        {
            Player.NetTransform.RpcSnapTo(Guarded.transform.position);
        }
    }

    public void AddBacklashArrow()
    {
        if (LastAttacker == null || Player == null) return;

        RemoveBacklashArrow();

        if (!LastAttacker.HasModifier<BodyguardBacklashArrowModifier>())
        {
            LastAttacker.AddModifier<BodyguardBacklashArrowModifier>(Player, TouExtensionColors.Bodyguard);
        }
    }

    public void RemoveBacklashArrow()
    {
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc != null && pc.HasModifier<BodyguardBacklashArrowModifier>())
            {
                pc.RemoveModifier<BodyguardBacklashArrowModifier>();
            }
        }
    }

    [MethodRpc((uint)ExtensionRpc.BodyguardGuard)]
    public static void RpcBodyguardGuard(PlayerControl bodyguard, PlayerControl target)
    {
        if (bodyguard.Data.Role is not BodyguardRole role)
        {
            // ("RpcBodyguardGuard - Invalid bodyguard");
            return;
        }

        role.SetGuardedPlayer(target);
    }

    [MethodRpc((uint)ExtensionRpc.BodyguardClearGuard)]
    public static void RpcBodyguardClearGuard(PlayerControl bodyguard)
    {
        if (bodyguard.Data.Role is not BodyguardRole role)
        {
            // ("RpcBodyguardClearGuard - Invalid bodyguard");
            return;
        }

        role.SetGuardedPlayer(null);
    }

    [MethodRpc((uint)ExtensionRpc.BodyguardShieldAttacked)]
    public static void RpcBodyguardShieldAttacked(PlayerControl bodyguard, PlayerControl attacker, PlayerControl guarded)
    {
        if (bodyguard.Data.Role is not BodyguardRole role)
        {
            return;
        }

        role.OnGuardedAttacked(attacker);
        
        // Notify attacker too if they are the local player
        if (attacker != null && attacker.AmOwner)
        {
            ShieldUtils.TriggerShieldFlash(attacker, ShieldType.Bodyguard);
        }
    }

    [MethodRpc((uint)ExtensionRpc.BodyguardBacklash)]
    public static void RpcBodyguardBacklash(PlayerControl bodyguard)
    {
        if (bodyguard.Data.Role is not BodyguardRole role)
        {
            // ("RpcBodyguardBacklash - Invalid bodyguard");
            return;
        }

        role.ActivateKillMode();
    }

    [MethodRpc((uint)ExtensionRpc.BodyguardKill)]
    public static void RpcBodyguardKill(PlayerControl bodyguard, PlayerControl victim)
    {
        if (bodyguard.Data.Role is not BodyguardRole role)
        {
            // ("RpcBodyguardKill - Invalid bodyguard");
            return;
        }

        if (!role.KillModeActive)
        {
            // ("[BG-Role] RpcBodyguardKill called but KillModeActive=false, ignoring");
            return;
        }

        role.RemoveBacklashArrow();

        role.KillModeActive = false;
        role.KillModeTimer = 0f;
        role.LastAttacker = null;
        role.MarkedAttackerDot = false;

        if (!victim.HasDied())
        {
            bodyguard.RpcSpecialMurder(victim,
                createDeadBody: true,
                teleportMurderer: false,
                showKillAnim: true,
                playKillSound: true,
                causeOfDeath: "Terminated");
        }

        role.SetGuardedPlayer(null);

        if (OptionGroupSingleton<BodyguardOptions>.Instance.DiesAfterKill && !bodyguard.HasDied())
        {
            // Bodyguard sacrifice is absolute - no shield (Warden, Medic, etc.) can prevent it
            bodyguard.RpcSpecialMurder(bodyguard,
                isIndirect: true,
                ignoreShield: true,
                createDeadBody: true,
                teleportMurderer: false,
                showKillAnim: true,
                playKillSound: false,
                causeOfDeath: "Bodyguard");
        }
    }
}
