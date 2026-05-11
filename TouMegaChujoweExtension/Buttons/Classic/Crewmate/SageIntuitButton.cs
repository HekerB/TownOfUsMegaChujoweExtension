using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using MiraAPI.Utilities;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Utilities;
using UnityEngine;

// TouExtensionAssets

namespace TouMegaChujoweExtension.Buttons.Classic.Crewmate;

public sealed class SageIntuitButton : TownOfUsRoleButton<SageRole, PlayerControl>
{
    public override string Name => "Intuit";
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Sage;
    public override int MaxUses => (int)OptionGroupSingleton<SageOptions>.Instance.MaxCompares;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<SageOptions>.Instance.SageCooldown + MapCooldown, 5f, 120f);
    public override LoadableAsset<Sprite> Sprite => TouCrewAssets.IntuitSprite;

    public void SetDiseasedTimer(float multiplier)
    {
        SetTimer(Cooldown * multiplier);
    }

    public override PlayerControl? GetTarget()
    {
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance,
            predicate: x => Role.GazeTarget != x && Role.IntuitTarget != x);
    }

    protected override void OnClick()
    {
        if (Target == null) return;

        if (Role.IntuitTarget != null)
        {
            ++UsesLeft;
            SetUses(UsesLeft);
        }

        Role.IntuitTarget = Target;

        CustomButtonSingleton<SageGazeButton>.Instance.ResetCooldownAndOrEffect();
        if (Role.GazeTarget != null && Role.IntuitTarget != null)
        {
            Role.SageCompare(PlayerControl.LocalPlayer);
        }
        else
        {
            var notif = Helpers.CreateAndShowNotification(
                $"<b>You are intuiting {Target.Data.PlayerName}</b>",
                Color.white, new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.SageRoleIcon.LoadAsset());
            notif.AdjustNotification();
        }
    }
}