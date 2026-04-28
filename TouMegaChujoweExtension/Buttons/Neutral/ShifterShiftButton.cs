using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modifiers;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class ShifterShiftButton : TownOfUsKillRoleButton<ShifterRole, PlayerControl>
{
    public override string Name => "Shift";
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Shifter;
    public override float Cooldown => OptionGroupSingleton<ShifterOptions>.Instance.ShiftCooldown;
    public override float EffectDuration => 0f;
    public override bool HasEffect => false;
    public override LoadableAsset<Sprite> Sprite => TouNeutAssets.ShiftSprite;

    public override PlayerControl? GetTarget()
    {
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance);
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (!base.IsTargetValid(target) || target == null)
            return false;

        var player = PlayerControl.LocalPlayer;
        if (player == null)
            return false;

        if (target.PlayerId == player.PlayerId)
            return false;

        return true;
    }

    public override bool CanUse()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied())
            return false;

        if (player.Data?.Role is ShifterRole shifterRole && shifterRole.ShiftUsed)
            return false;

        return base.CanUse();
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || Target == null)
            return;

        if (player.Data?.Role is not ShifterRole)
            return;

        var shieldType = Target.GetShieldType();
        if (shieldType == ShieldType.Warden || shieldType == ShieldType.FirstDead || shieldType == ShieldType.Cleric)
        {
            ShieldUtils.TriggerShieldFlash(player, shieldType);
            Timer = Cooldown;
            return;
        }

        if (!ShifterRole.IsValidShiftTarget(Target))
        {
            ShifterRole.RpcShifterDie(player);
            return;
        }

        ShifterRole.RpcSetShiftTarget(player, Target.PlayerId);
        Timer = Cooldown;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        if (MeetingHud.Instance)
            return;

        var player = PlayerControl.LocalPlayer;
        if (player?.Data?.Role is ShifterRole shifterRole)
        {
            if (shifterRole.ShiftUsed)
            {
                Button?.gameObject.SetActive(false);
                return;
            }
        }

        Button?.gameObject.SetActive(
            HudManager.Instance.UseButton.isActiveAndEnabled ||
            HudManager.Instance.PetButton.isActiveAndEnabled);

        base.FixedUpdate(playerControl);
    }
}
