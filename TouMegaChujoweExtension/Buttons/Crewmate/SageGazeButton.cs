using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using MiraAPI.Keybinds;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
using TouMegaChujoweExtension.Assets;  // TouExtensionAssets
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Crewmate;

public sealed class SageGazeButton : TownOfUsRoleButton<SageRole, PlayerControl>
{
    public override string Name => "Gaze";
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Sage;
    public override int MaxUses => (int)OptionGroupSingleton<SageOptions>.Instance.MaxCompares;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<SageOptions>.Instance.SageCooldown + MapCooldown, 5f, 120f);
    public override LoadableAsset<Sprite> Sprite => TouCrewAssets.GazeSprite;

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

        if (Role.GazeTarget != null)
        {
            ++UsesLeft;
            SetUses(UsesLeft);
        }

        Role.GazeTarget = Target;

        CustomButtonSingleton<SageIntuitButton>.Instance.ResetCooldownAndOrEffect();
        if (Role.GazeTarget != null && Role.IntuitTarget != null)
        {
            Role.SageCompare(PlayerControl.LocalPlayer);
        }
        else
        {
            var notif = Helpers.CreateAndShowNotification(
                $"<b>You are gazing at {Target.Data.PlayerName}</b>",
                Color.white, new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.SageRoleIcon.LoadAsset());
            notif.AdjustNotification();
        }
    }
}
