using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using System;
using System.Linq;
using UnityEngine;
using TownOfUs.Assets;
using TownOfUs.Events;
using TownOfUs.Modifiers;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class JackalKillButton : TownOfUsKillRoleButton<JackalRole, PlayerControl>, IDiseaseableButton, IKillButton
{
    public override string Name => TouLocale.Get("ExtensionRoleJackalAssassination", "Assassinate");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Jackal;
    public override LoadableAsset<Sprite> Sprite => TouNeutAssets.PestKillSprite;
    public bool Show { get; set; } = true;

    public override bool Enabled(RoleBehaviour? role)
    {
        return base.Enabled(role) && Show;
    }

    public override void SetActive(bool visible, RoleBehaviour role)
    {
        base.SetActive(visible, role);
    }

    public void SetDiseasedTimer(float multiplier)
    {
        Timer = Cooldown * multiplier;
    }

    public override float Cooldown
    {
        get
        {
            var baseCooldown = OptionGroupSingleton<JackalOptions>.Instance.KillCooldown + MapCooldown;
            return Math.Clamp(baseCooldown, 5f, 120f);
        }
    }

    public override float InitialCooldown => Cooldown;

    public override int MaxUses => -1;

    public override bool CanClick()
    {
        return CanUse() && Target != null;
    }

    private bool? _wasSidekickAlive;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        _wasSidekickAlive = null;

        var local = PlayerControl.LocalPlayer;
        if (local != null)
        {
            var sidekicksAlive = PlayerControl.AllPlayerControls.ToArray()
                .Any(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && !p.Data.IsDead && p.TryGetModifier<SidekickModifier>(out var m) && m != null && m.JackalId == local.PlayerId);

            if (!sidekicksAlive)
            {
                Timer = Cooldown;
                UnityEngine.Debug.Log($"[TOUMCE] Recreated Jackal Kill Button without sidekicks. Setting timer to Cooldown: {Cooldown}");
            }
        }
    }

    public override void FixedUpdateHandler(PlayerControl playerControl)
    {
        base.FixedUpdateHandler(playerControl);

        if (playerControl == null || playerControl.Data == null || playerControl.Data.IsDead) return;

        var local = PlayerControl.LocalPlayer;
        if (local != null && local.PlayerId == playerControl.PlayerId)
        {
            var newTarget = GetTarget();
            if (newTarget != Target)
            {
                SetOutline(false);
            }
            Target = IsTargetValid(newTarget) ? newTarget : null;
            SetOutline(true);
        }

        // Check if sidekicks just died to trigger the 10s cooldown (checking both modifiers and pending assignments)
        var sidekicksAlive = PlayerControl.AllPlayerControls.ToArray()
            .Any(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && !p.Data.IsDead && 
                 ((p.TryGetModifier<SidekickModifier>(out var m) && m != null && m.JackalId == playerControl.PlayerId) ||
                  (Patches.Roles.Jackal.JackalStartPatch.PendingAssignments.TryGetValue(p.PlayerId, out var jId) && jId == playerControl.PlayerId)));

        if (!_wasSidekickAlive.HasValue)
        {
            _wasSidekickAlive = sidekicksAlive;
            return;
        }

        if (_wasSidekickAlive.Value && !sidekicksAlive)
        {
            var jackalRole = playerControl.GetRole<JackalRole>();
            if (jackalRole != null && !jackalRole.KillAbilityAlertShown)
            {
                jackalRole.KillAbilityAlertShown = true;
                Timer = 10f;
                UnityEngine.Debug.Log("[TOUMCE] Jackal sidekicks died, applying 10s cooldown and alerts.");

                // Show localized recruit death & shield lost notification
                var alertMsg = TouLocale.Get("ExtensionJackalKillAbilityAlert");
                if (OptionGroupSingleton<JackalOptions>.Instance.ShieldWhileSidekicksAlive)
                {
                    alertMsg += "\n" + TouLocale.Get("ExtensionJackalShieldLostAlert");
                }
                MiraAPI.Utilities.Helpers.CreateAndShowNotification(
                    alertMsg, 
                    TouExtensionColors.Jackal, 
                    new Vector3(0f, 1f, -20f), 
                    spr: TouRoleIcons.Jackal.LoadAsset()
                ).AdjustNotification();
                
                Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Jackal));
            }
        }
        _wasSidekickAlive = sidekicksAlive;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (Button == null) return;

        var local = PlayerControl.LocalPlayer;
        if (local == null) return;

        // Force label to be Assassinate/Zamach instead of default KILL
        OverrideName(TouLocale.Get("ExtensionRoleJackalAssassination", "Assassinate"));

        if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started)
        {
            Button.gameObject.SetActive(false);
            return;
        }

        var sidekicksAlive = PlayerControl.AllPlayerControls.ToArray()
            .Any(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && !p.Data.IsDead && p.TryGetModifier<SidekickModifier>(out var m) && m != null && m.JackalId == local.PlayerId);

        Button.gameObject.SetActive(!sidekicksAlive);
    }

    protected override void OnClick()
    {
        if (Target == null) return;

        var local = PlayerControl.LocalPlayer;
        if (local == null) return;

        UnityEngine.Debug.Log($"[TOUMCE] Jackal {local.Data.PlayerName} is assassinating {Target.Data.PlayerName}");

        try
        {
            local.RpcCustomMurder(Target, MeetingCheck.OutsideMeeting);
            UnityEngine.Debug.Log($"[TOUMCE] Jackal Kill RPC Sent successfully to {Target.Data.PlayerName}");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[TOUMCE] Jackal Kill RPC FAILED: {ex}");
        }

        Timer = Cooldown;
    }

    public override bool CanUse()
    {
        if (Timer > 0f) return false;

        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null || local.Data.IsDead) return false;

        if (TimeLordRewindSystem.IsRewinding) return false;
        if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance) return false;

        if (!local.CanMove || local.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities)) return false;

        var sidekicksAlive = PlayerControl.AllPlayerControls.ToArray()
            .Any(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && !p.Data.IsDead && p.TryGetModifier<SidekickModifier>(out var m) && m != null && m.JackalId == local.PlayerId);

        return !sidekicksAlive;
    }

    public override void SetOutline(bool active)
    {
        if (Target != null && !PlayerControl.LocalPlayer.HasDied())
        {
            Target.cosmetics.SetOutline(active, active ? new Il2CppSystem.Nullable<Color>(TouExtensionColors.Jackal) : new Il2CppSystem.Nullable<Color>());
        }
    }

    public override float Distance
    {
        get
        {
            return OptionGroupSingleton<JackalOptions>.Instance.KillDistance switch
            {
                0 => 1.25f,
                1 => 1.75f,
                2 => 2.5f,
                _ => 1.75f
            };
        }
    }

    public override PlayerControl? GetTarget()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null) return null;
        return local.GetClosestLivingPlayer(false, Distance, false, x => IsTargetValid(x));
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (target == null || target.Data == null || target.Data.IsDead || target.inVent) return false;

        var local = PlayerControl.LocalPlayer;
        if (local == null) return false;

        if (target.PlayerId == local.PlayerId) return false;

        if (target.TryGetModifier<SidekickModifier>(out var mod) && mod != null && mod.JackalId == local.PlayerId) return false;

        if (target.GetModifiers<DisabledModifier>().Any(mod => !mod.CanBeInteractedWith)) return false;
        if (TownOfUs.Roles.Other.SpectatorRole.TrackedSpectators.Contains(target.Data.PlayerName)) return false;

        return true;
    }
}
