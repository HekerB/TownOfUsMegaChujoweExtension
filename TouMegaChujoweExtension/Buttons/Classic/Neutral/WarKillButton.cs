using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using System.Globalization;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Options;
using TownOfUs.Options.Modifiers.Alliance;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class WarKillButton : TownOfUsRoleButton<WarRole, PlayerControl>, IDiseaseableButton, IKillButton
{
    private bool _lastKillSucceeded;

    public override string Name => TranslationController.Instance.GetStringWithDefault(StringNames.KillLabel, "Kill");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.War;
    public override float Cooldown => BerserkerRole.GetKillCooldownForKills((int)OptionGroupSingleton<BerserkerOptions>.Instance.KillsNeededToTransform) + MapCooldown;
    public override LoadableAsset<Sprite> Sprite => TouNeutAssets.WerewolfKillSprite;
    public override float Distance => GameManager.Instance.LogicOptions.GetKillDistance();
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
        var local = PlayerControl.LocalPlayer;
        if (target == null || local == null || target == local || target.HasDied())
        {
            return false;
        }

        if (ApocalypseUtils.AreAllied(local, target))
        {
            return false;
        }

        var distance = Vector2.Distance(local.GetTruePosition(), target.GetTruePosition());
        return distance <= Distance;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (Role.WarSpreeUntil > 0f && Time.time <= Role.WarSpreeUntil && Timer <= 0f)
        {
            var spreeDuration = OptionGroupSingleton<BerserkerOptions>.Instance.WarKillingSpreeDuration;
            var remaining = Mathf.Max(0f, Role.WarSpreeUntil - Time.time);

            if (Button != null)
            {
                Button.SetFillUp(remaining, spreeDuration);
                Button.cooldownTimerText.text = remaining.ToString(CooldownTimerFormatString, NumberFormatInfo.InvariantInfo);
                Button.cooldownTimerText.gameObject.SetActive(true);
                Button.buttonLabelText.text = TouLocale.GetParsed("ExtensionRoleWarSpreeKill", "Spree Kill");
            }

            return;
        }

        if (Button != null && Button.buttonLabelText.text != Name)
        {
            Button.buttonLabelText.text = Name;
        }

        if (Role.WarSpreeUntil <= 0f || Time.time <= Role.WarSpreeUntil || Timer > 0f)
        {
            return;
        }

        Role.WarSpreeUntil = 0f;
        Timer = Cooldown;
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || Target == null || !IsTargetValid(Target))
        {
            return;
        }

        if (PoisonSystem.CheckAndTriggerShields(player, Target))
        {
            Timer = Cooldown;
            return;
        }

        player.RpcCustomMurder(Target, MeetingCheck.OutsideMeeting);
        _lastKillSucceeded = true;
    }
}
