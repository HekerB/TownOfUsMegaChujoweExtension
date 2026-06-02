using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Events;
using TownOfUs.Modifiers;
using TownOfUs.Options.Modifiers.Alliance;
using TownOfUs.Options;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class BerserkerKillButton : TownOfUsKillRoleButton<BerserkerRole, PlayerControl>, IDiseaseableButton, IKillButton
{
    public override string Name => TranslationController.Instance.GetStringWithDefault(StringNames.KillLabel, "Kill");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => Role?.RoleColor ?? TouExtensionColors.Berserker;
    public override float Cooldown => Math.Clamp((Role?.GetKillCooldown() ?? OptionGroupSingleton<BerserkerOptions>.Instance.InitialKillCooldown) + MapCooldown, 5f, 120f);
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

        var wasWar = Role != null && Role.IsWar;
        OnClick();

        if (wasWar && Role != null && Role.IsWar)
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

        if (Role == null ||
            !Role.IsWar ||
            Role.WarSpreeUntil <= 0f ||
            Time.time <= Role.WarSpreeUntil ||
            Timer > 0f)
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
        Role?.OnSuccessfulKill();
    }
}
