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
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class JackalKillButton : TownOfUsKillRoleButton<JackalRole, PlayerControl>, IDiseaseableButton, IKillButton
{
    public JackalKillButton() : base() { }

    public override string Name => "Assassination";
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Jackal;
    public override LoadableAsset<Sprite> Sprite => TouNeutAssets.PestKillSprite;

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

    public override int MaxUses => -1;

    public override bool CanUse()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null || local.Data.IsDead) return false;

        if (AmongUsClient.Instance == null || AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return false;
        if (MeetingHud.Instance != null) return false;

        var sidekicksAlive = PlayerControl.AllPlayerControls.ToArray()
            .Any(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && !p.Data.IsDead && p.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == local.PlayerId);

        return !sidekicksAlive;
    }

    public override bool CanClick()
    {
        return CanUse() && Target != null;
    }

    public override void ClickHandler()
    {
        if (!CanClick()) 
        {
            UnityEngine.Debug.Log("[TOUMCE] Jackal ClickHandler: Cannot click (CanClick=false)");
            return;
        }
        if (Target == null) return;

        var local = PlayerControl.LocalPlayer;
        if (local == null) return;

        UnityEngine.Debug.Log($"[TOUMCE] Jackal ClickHandler: Attempting to kill {Target.Data.PlayerName}");

        var beforeMurderEvent = new BeforeMurderEvent(local, Target, MeetingCheck.OutsideMeeting);
        MiraEventManager.InvokeEvent(beforeMurderEvent);

        if (beforeMurderEvent.IsCancelled)
        {
            UnityEngine.Debug.Log($"[TOUMCE] Jackal Kill CANCELLED by event system for target {Target.Data.PlayerName}");
            Timer = Cooldown;
            return;
        }

        OnClick();
    }

    public override void FixedUpdateHandler(PlayerControl playerControl)
    {
        base.FixedUpdateHandler(playerControl);

        if (playerControl == null || playerControl.Data == null || playerControl.Data.IsDead) return;

        var newTarget = GetTarget();
        if (newTarget != Target)
        {
            SetOutline(false);
        }
        Target = IsTargetValid(newTarget) ? newTarget : null;
        SetOutline(true);
    }

    protected override void OnClick()
    {
        if (Target == null) return;

        var local = PlayerControl.LocalPlayer;
        if (local == null) return;

        UnityEngine.Debug.Log($"[TOUMCE] Jackal {local.Data.PlayerName} is assassinating {Target.Data.PlayerName}");

        try
        {
            local.RpcCustomMurder(Target);
            UnityEngine.Debug.Log($"[TOUMCE] Jackal Kill RPC Sent successfully to {Target.Data.PlayerName}");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[TOUMCE] Jackal Kill RPC FAILED: {ex}");
        }

        DeathHandlerModifier.UpdateDeathHandlerImmediate(
            Target,
            causeOfDeath: TouLocale.Get("DiedToJackal", "Assassinated"),
            roundOfDeath: DeathEventHandlers.CurrentRound,
            diedThisRound: DeathHandlerOverride.SetTrue,
            killedBy: Role.RoleName,
            lockInfo: DeathHandlerOverride.SetTrue);

        Timer = Cooldown;
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
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance);
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (target == null || target.Data == null || target.Data.IsDead) return false;

        var local = PlayerControl.LocalPlayer;
        if (local == null) return false;

        if (target.PlayerId == local.PlayerId) return false;

        if (target.TryGetModifier<SidekickModifier>(out var mod) && mod != null && mod.JackalId == local.PlayerId) return false;

        return true;
    }
}
