using AmongUs.GameOptions;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Events;
using TownOfUs.Options;
using TownOfUs.Options.Modifiers.Alliance;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class WarKillButton : TownOfUsKillRoleButton<WarRole, PlayerControl>, IDiseaseableButton, IKillButton
{
    private bool _lastKillSucceeded;

    public override string Name => TranslationController.Instance.GetStringWithDefault(StringNames.KillLabel, "Kill");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.War;
    public override float Cooldown => BerserkerRole.GetKillCooldownForKills((int)OptionGroupSingleton<BerserkerOptions>.Instance.KillsNeededToTransform) + MapCooldown;
    public override LoadableAsset<Sprite> Sprite => TouAssets.KillSprite;
    public override bool ShouldPauseInVent => true;

    public void SetDiseasedTimer(float multiplier)
    {
        SetTimer(Cooldown * multiplier);
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

        _lastKillSucceeded = false;
        OnClick();

        if (_lastKillSucceeded)
        {
            var spreeDuration = OptionGroupSingleton<BerserkerOptions>.Instance.WarKillingSpreeDuration;
            if (spreeDuration > 0f)
            {
                Role.WarSpreeUntil = Time.time + spreeDuration;
                Timer = 0f;
                return;
            }
        }

        Timer = Cooldown;
    }

    public override PlayerControl? GetTarget()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return null;
        }

        if (!OptionGroupSingleton<LoversOptions>.Instance.LoversKillEachOther && player.IsLover())
        {
            return player.GetClosestLivingPlayer(true, Distance, false, x => !x.IsLover() && IsTargetValid(x));
        }

        return player.GetClosestLivingPlayer(true, Distance, predicate: IsTargetValid);
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        return target != null &&
               base.IsTargetValid(target) &&
               !ApocalypseUtils.AreAllied(PlayerControl.LocalPlayer, target);
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (Role.WarSpreeUntil <= 0f || Time.time <= Role.WarSpreeUntil || Timer > 0f)
        {
            return;
        }

        Role.WarSpreeUntil = 0f;
        Timer = Cooldown;
    }

    protected override void OnClick()
    {
        if (Target == null)
        {
            return;
        }

        PlayerControl.LocalPlayer.RpcCustomMurder(Target);
        _lastKillSucceeded = true;
    }
}
