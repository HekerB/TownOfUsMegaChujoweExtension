using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Networking;
using MiraAPI.Utilities.Assets;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using Reactor.Utilities;
using System;
using System.Linq;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using TownOfUs.Modifiers;
using TownOfUs.Roles.Other;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class JackalKillButton : TownOfUsKillRoleButton<JackalRole, PlayerControl>, IDiseaseableButton, IKillButton
{
    public override string Name => TouLocale.Get("ExtensionRoleJackalAssassination", "Assassinate");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Jackal;
    public override LoadableAsset<Sprite> Sprite => TouNeutAssets.PestKillSprite;

    public override float Cooldown => Math.Clamp(
        OptionGroupSingleton<JackalOptions>.Instance.KillCooldown + MapCooldown, 5f, 120f);



    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        Coroutines.Start(MiscUtils.CoMoveButtonIndex(this, false));
    }

    public void SetDiseasedTimer(float multiplier)
    {
        SetTimer(Cooldown * multiplier);
    }

    public override bool CanUse()
    {
        return base.CanUse() && !AreSidekicksAlive();
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
            Timer = Cooldown;
            return;
        }

        OnClick();
        
        if (HasEffect)
        {
            EffectActive = true;
            Timer = EffectDuration;
        }
        else
        {
            Timer = Cooldown;
        }
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || Target == null)
        {
            Error("Jackal Kill: Target or player is null");
            return;
        }

        player.RpcCustomMurder(Target, MeetingCheck.OutsideMeeting);
    }

    public override PlayerControl? GetTarget()
    {
        if (AreSidekicksAlive()) return null;

        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance, false, x => IsTargetValid(x));
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (AreSidekicksAlive()) return false;

        var local = PlayerControl.LocalPlayer;
        if (target == null || local == null || target == local || target.HasDied()) return false;

        if (target.inVent) return false;

        if (target.GetModifiers<DisabledModifier>().Any(mod => !mod.CanBeInteractedWith && mod.GetType().Name != "JailedModifier")) return false;

        if (SpectatorRole.TrackedSpectators.Contains(target.Data.PlayerName)) return false;

        if (target.TryGetModifier<SidekickModifier>(out var mod) && mod != null && mod.JackalId == local.PlayerId)
            return false;

        if (!OptionGroupSingleton<TownOfUs.Options.Modifiers.Alliance.LoversOptions>.Instance.LoversKillEachOther && local.IsLover() && target.IsLover())
            return false;

        var distance = Vector2.Distance(local.GetTruePosition(), target.GetTruePosition());
        return distance <= Distance;
    }


    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (Button == null) return;

        if (AreSidekicksAlive())
        {
            Button.gameObject.SetActive(false);
        }

        OverrideName(TouLocale.Get("ExtensionRoleJackalAssassination", "Assassinate"));
    }

    private static bool AreSidekicksAlive()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null) return false;

        return PlayerControl.AllPlayerControls.ToArray()
            .Any(p => p != null && p.Pointer != IntPtr.Zero && !p.HasDied() &&
                 p.TryGetModifier<SidekickModifier>(out var m) && m != null && m.JackalId == local.PlayerId);
    }
}
