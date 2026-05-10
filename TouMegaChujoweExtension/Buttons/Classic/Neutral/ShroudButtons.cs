using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Networking;
using MiraAPI.Utilities.Assets;
using MiraAPI.Utilities;
using MiraAPI;
using System.Collections;
using System.Linq;
using System;
using TownOfUs.Buttons;
using TownOfUs.Events;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modules;
using TownOfUs.Options;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class ShroudKillButton : TownOfUsKillRoleButton<ShroudRole, PlayerControl>, IKillButton
{
    public override string Name => TranslationController.Instance.GetStringWithDefault(StringNames.KillLabel, "Kill");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Shroud;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<ShroudOptions>.Instance.KillCooldown + MapCooldown, 5f, 120f);
    public override LoadableAsset<Sprite> Sprite => TouExtensionNeuAssets.ShroudKillButtonSprite;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
    }

    public override void ClickHandler()
    {
        if (!CanClick()) return;
        if (Target == null) return;

        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        var beforeMurderEvent = new BeforeMurderEvent(player, Target, MeetingCheck.OutsideMeeting);
        MiraEventManager.InvokeEvent(beforeMurderEvent);
        
        if (beforeMurderEvent.IsCancelled)
        {
            return;
        }

        OnClick();
        Timer = Cooldown;
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || Target == null) return;

        player.RpcSpecialMurder(Target, causeOfDeath: "Shroud");

        player.SetKillTimer(Cooldown);
        if (OptionGroupSingleton<ShroudOptions>.Instance.SharedCooldown)
            ShroudAbilityButton.SyncInternalTimer(Cooldown);
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);
        if (playerControl.killTimer > Timer) Timer = playerControl.killTimer;
        else if (Timer > playerControl.killTimer) playerControl.SetKillTimer(Timer);
    }

    public override PlayerControl? GetTarget()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null) return null;
        return player.GetClosestLivingPlayer(true, Distance);
    }

    public override bool CanUse()
    {
        return base.CanUse() && Target != null && Timer <= 0;
    }

    public static void SyncInternalTimer(float timer)
    {
        var instance = CustomButtonSingleton<ShroudKillButton>.Instance;
        if (instance != null && timer > instance.Timer)
            instance.Timer = timer;
    }
}

public sealed class ShroudAbilityButton : TownOfUsKillRoleButton<ShroudRole, PlayerControl>
{
    public override string Name => "Shroud";
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Shroud;
    public override float Cooldown => OptionGroupSingleton<ShroudOptions>.Instance.ShroudCooldown;
    public override LoadableAsset<Sprite> Sprite => TouExtensionNeuAssets.ShroudAbilitySprite;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
    }

    public override PlayerControl? GetTarget()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null) return null;
        return player.GetClosestLivingPlayer(true, Distance);
    }

    public override bool CanUse()
    {
        return base.CanUse() && Target != null && Timer <= 0;
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (target == null || target.HasDied() || target == PlayerControl.LocalPlayer) return false;

        // Block targeting ONLY for Child
        var child = target.GetModifiers<ChildModifier>().FirstOrDefault();
        if (child != null && !child.IsAdult) return false;

        if (target.TryGetModifier<ShroudedModifier>(out var mod) && mod.ShroudOwnerId == PlayerControl.LocalPlayer.PlayerId)
            return false;

        return base.IsTargetValid(target);
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || Target == null) return;

        // Shroud ability (hexing) is not a kill - no shield checks needed.
        // Kill button already goes through BeforeMurderEvent, which native handlers will block.

        if (Target.TryGetModifier<ShroudedModifier>(out var existingMod) && existingMod.ShroudOwnerId == player.PlayerId)
            return;

        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p != null && p.TryGetModifier<ShroudedModifier>(out var mod) && mod.ShroudOwnerId == player.PlayerId)
            {
                p.RpcRemoveModifier<ShroudedModifier>();
            }
        }

        Target.RpcAddModifier<ShroudedModifier>(player);

        if (OptionGroupSingleton<ShroudOptions>.Instance.SharedCooldown)
        {
            player.SetKillTimer(Cooldown);
            ShroudKillButton.SyncInternalTimer(Cooldown);
        }
        
        Timer = Cooldown;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);
        if (OptionGroupSingleton<ShroudOptions>.Instance.SharedCooldown)
        {
            if (playerControl.killTimer > Timer) Timer = playerControl.killTimer;
            else if (Timer > playerControl.killTimer) playerControl.SetKillTimer(Timer);
        }
    }

    public static void SyncInternalTimer(float timer)
    {
        var instance = CustomButtonSingleton<ShroudAbilityButton>.Instance;
        if (instance != null && timer > instance.Timer)
            instance.Timer = timer;
    }
}

















