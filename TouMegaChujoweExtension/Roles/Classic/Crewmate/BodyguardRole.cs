using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using System.Collections;
using System.Text;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Crewmate;

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

    [HideFromIl2Cpp] public bool BacklashReady { get; set; }

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
            Clear();
        }

        if (BacklashReady && BacklashTimer > 0f)
        {
            BacklashTimer -= Time.fixedDeltaTime;
            if (BacklashTimer <= 0f)
            {
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
            sb.AppendLine(TownOfUsPlugin.Culture, $"\n<b>{TouLocale.GetParsed("ExtensionRoleBodyguardTabGuarding").Replace("<player>", Guarded.Data.PlayerName)}</b>");
        }

        if (BacklashReady)
        {
            sb.AppendLine(TownOfUsPlugin.Culture, $"\n<color=red><b>{TouLocale.Get("ExtensionRoleBodyguardTabBacklashReady")}</b></color>");
        }

        if (KillModeActive)
        {
            sb.AppendLine(TownOfUsPlugin.Culture, $"\n<color=red><b>{TouLocale.Get("ExtensionRoleBodyguardTabKillMode")}</b></color>");
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
        RemoveBacklashArrow();

        Guarded?.RemoveModifier<BodyguardShieldModifier>();

        Guarded = null;
        BacklashReady = false;
        KillModeActive = false;
        LastAttacker = null;
        MarkedAttackerDot = false;
    }

    public void SetGuardedPlayer(PlayerControl? target)
    {
        RemoveBacklashArrow();

        Guarded?.RemoveModifier<BodyguardShieldModifier>();

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
        LastAttacker = attacker;
        BacklashReady = true;
        BacklashTimer = OptionGroupSingleton<BodyguardOptions>.Instance.BacklashWindow;

        var onlyAttacker = OptionGroupSingleton<BodyguardOptions>.Instance.OnlyTargetAttacker;
        MarkedAttackerDot = onlyAttacker;

        if (Player.AmOwner)
        {
            TriggerBodyguardFlash(Player);

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

    public static void RemoveBacklashArrow()
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
            return;
        }

        role.SetGuardedPlayer(target);
    }

    [MethodRpc((uint)ExtensionRpc.BodyguardClearGuard)]
    public static void RpcBodyguardClearGuard(PlayerControl bodyguard)
    {
        if (bodyguard.Data.Role is not BodyguardRole role)
        {
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

        if (guarded == null || attacker == null) return;
        Info($"[Bodyguard] Shield on {guarded.Data.PlayerName} attacked by {attacker.Data.PlayerName}");
        role.OnGuardedAttacked(attacker);

        if (attacker != null && attacker.AmOwner)
        {
            TriggerBodyguardFlash(attacker);
        }
    }

    [MethodRpc((uint)ExtensionRpc.BodyguardBacklash)]
    public static void RpcBodyguardBacklash(PlayerControl bodyguard)
    {
        if (bodyguard.Data.Role is not BodyguardRole role)
        {
            return;
        }

        role.ActivateKillMode();
    }

    [MethodRpc((uint)ExtensionRpc.BodyguardKill)]
    public static void RpcBodyguardKill(PlayerControl bodyguard, PlayerControl victim)
    {
        if (bodyguard.Data.Role is not BodyguardRole role)
        {
            return;
        }

        if (!role.KillModeActive)
        {
            return;
        }

        RemoveBacklashArrow();

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

    private static SpriteRenderer? _flashRenderer;

    public static void TriggerBodyguardFlash(PlayerControl player)
    {
        if (player == null || !player.AmOwner) return;
        var color = new Color(0.412f, 0.647f, 1f);
        Info($"[Bodyguard] Triggering shield flash for {player.Data.PlayerName}");
        Coroutines.Start(CoFlash(color));
    }

    private static IEnumerator CoFlash(Color color)
    {
        if (HudManager.Instance == null || HudManager.Instance.FullScreen == null) yield break;

        if (_flashRenderer == null)
        {
            _flashRenderer = UnityEngine.Object.Instantiate(HudManager.Instance.FullScreen, HudManager.Instance.FullScreen.transform.parent);
            _flashRenderer.transform.localScale *= 25f;
            _flashRenderer.name = "BodyguardFlashRenderer";
        }

        var flashColor = color;
        flashColor.a = 0.5f;
        _flashRenderer.color = flashColor;
        _flashRenderer.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        _flashRenderer?.gameObject.SetActive(false);
    }
}

