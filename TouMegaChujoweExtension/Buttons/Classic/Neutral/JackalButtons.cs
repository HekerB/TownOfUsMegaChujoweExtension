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
    public override string Name => "Kill";
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Jackal;
    public override LoadableAsset<Sprite> Sprite => TouNeutAssets.PestKillSprite;
    public override bool ShouldPauseInVent => false;
    public bool Show { get; set; } = true;

    public override bool Enabled(RoleBehaviour? role) => base.Enabled(role) && Show;

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
        if (!base.CanUse()) return false;

        var local = PlayerControl.LocalPlayer;
        if (local == null) return false;

        var sidekicksAlive = PlayerControl.AllPlayerControls.ToArray()
            .Any(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && !p.Data.IsDead && p.TryGetModifier<SidekickModifier>(out var m) && m != null && m.JackalId == local.PlayerId);

        return !sidekicksAlive;
    }

    public override bool CanClick()
    {
        return CanUse() && Target != null;
    }

    private bool _needsInitialCooldown;
    private bool _wasSidekickAlive = true;

    public override void FixedUpdateHandler(PlayerControl playerControl)
    {
        base.FixedUpdateHandler(playerControl);

        if (playerControl == null || playerControl.Data == null || playerControl.Data.IsDead) return;

        // Check if sidekicks just died to trigger the 10s cooldown
        var sidekicksAlive = PlayerControl.AllPlayerControls.ToArray()
            .Any(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && !p.Data.IsDead && p.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == playerControl.PlayerId);

        if (_wasSidekickAlive && !sidekicksAlive)
        {
            _needsInitialCooldown = true;
            UnityEngine.Debug.Log("[TOUMCE] Jackal sidekicks died, flagging for 10s cooldown.");
        }
        _wasSidekickAlive = sidekicksAlive;

        if (_needsInitialCooldown && Timer < 10f)
        {
            Timer = 10f;
            _needsInitialCooldown = false;
            UnityEngine.Debug.Log("[TOUMCE] Applied 10s cooldown to Jackal Kill Button.");
        }
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
        return local.GetClosestLivingPlayer(true, Distance, false, x => IsTargetValid(x));
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (!base.IsTargetValid(target)) return false;
        if (target == null || target.Data == null || target.Data.IsDead) return false;

        var local = PlayerControl.LocalPlayer;
        if (local == null) return false;

        if (target.PlayerId == local.PlayerId) return false;

        if (target.TryGetModifier<SidekickModifier>(out var mod) && mod != null && mod.JackalId == local.PlayerId) return false;

        return true;
    }
}
