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

    public override int MaxUses => -1; // Infinite, usually hides the counter in TOU

    public override bool CanUse()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null || local.Data.IsDead) return false;

        // Check if any sidekicks are alive
        var sidekicksAlive = PlayerControl.AllPlayerControls.ToArray()
            .Any(p => p != null && p.Pointer != IntPtr.Zero && p.Data != null && !p.Data.IsDead && p.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == local.PlayerId);

        if (sidekicksAlive) return false;

        if (AmongUsClient.Instance == null || AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return false;
        if (MeetingHud.Instance != null) return false;

        // Standard timer check
        if (Timer > 0) return false;

        return true;
    }

    public override bool CanClick()
    {
        // Reuse same logic – prevents clicking even if button somehow appears ready
        return CanUse() && Target != null;
    }

    public override void ClickHandler()
    {
        if (!CanClick()) return;
        if (Target == null) return;

        var local = PlayerControl.LocalPlayer;
        if (local == null) return;

        var beforeMurderEvent = new BeforeMurderEvent(local, Target, MeetingCheck.OutsideMeeting);
        MiraEventManager.InvokeEvent(beforeMurderEvent);

        if (beforeMurderEvent.IsCancelled)
        {
            Timer = Cooldown;
            return;
        }

        OnClick();
    }

    protected override void OnClick()
    {
        if (Target == null) return;

        var local = PlayerControl.LocalPlayer;
        
        UnityEngine.Debug.Log($"[TOUMCE] Jackal {local.Data.PlayerName} is assassinating {Target.Data.PlayerName}");
        
        local.RpcCustomMurder(Target);
        
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
                0 => 1.25f, // Short
                1 => 1.75f, // Normal
                2 => 2.5f,  // Long
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
        if (!base.IsTargetValid(target) || target == null) return false;
        if (target.Data == null || target.Data.IsDead) return false;

        var local = PlayerControl.LocalPlayer;
        if (local == null) return false;

        // Jackal cannot target themselves
        if (target.PlayerId == local.PlayerId) return false;

        return true;
    }
}
