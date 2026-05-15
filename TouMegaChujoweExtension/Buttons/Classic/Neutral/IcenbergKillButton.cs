using System;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Events;
using TownOfUs.Modifiers;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class IcenbergKillButton : TownOfUsKillRoleButton<IcenbergRole, PlayerControl>, IDiseaseableButton, IKillButton
{
    public override string Name => TranslationController.Instance.GetStringWithDefault(StringNames.KillLabel, "Kill");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Icenberg;
    public override float Cooldown
    {
        get
        {
            var cooldown = OptionGroupSingleton<IcenbergOptions>.Instance.KillCooldown;
            return Math.Clamp(cooldown + MapCooldown, 5f, 120f);
        }
    }
    public override LoadableAsset<Sprite> Sprite => TouAssets.KillSprite;
    public override bool ZeroIsInfinite { get; set; } = true;

    public void SetDiseasedTimer(float multiplier)
    {
        SetTimer(Cooldown * multiplier);
    }

    public override PlayerControl? GetTarget()
    {
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance);
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (!base.IsTargetValid(target) || target == null)
        {
            return false;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return false;
        }

        if (player.IsImpostorAligned() && target.IsImpostorAligned())
        {
            return false;
        }

        if (target.HasModifier<FirstDeadShield>())
        {
            return false;
        }

        return true;
    }

    protected override void OnClick()
    {
        if (Target == null)
        {
            Error("Icenberg Kill: Target is null");
            return;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            Error("Icenberg Kill: LocalPlayer is null");
            return;
        }

        CustomTouMurderRpcs.RpcSpecialMurder(player, Target, causeOfDeath: "Frozen");
    }

    public override void ClickHandler()
    {
        if (!CanClick() || Target == null)
        {
            return;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return;
        }

        var beforeMurderEvent = new BeforeMurderEvent(player, Target, MeetingCheck.OutsideMeeting);
        MiraEventManager.InvokeEvent(beforeMurderEvent);

        if (beforeMurderEvent.IsCancelled)
        {
            return;
        }

        OnClick();
        Button?.SetDisabled();
        Timer = Cooldown;
    }
}
