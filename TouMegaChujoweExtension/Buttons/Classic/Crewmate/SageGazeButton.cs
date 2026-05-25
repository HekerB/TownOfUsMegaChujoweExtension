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

public sealed class SageGazeButton : TownOfUsRoleButton<SageRole, PlayerControl>
{
    public static readonly Color AbilityColor = new(0.46f, 0.86f, 1f, 1f);

    public override string Name => "Gaze";
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => AbilityColor;
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
                $"<b><color=#{ColorUtility.ToHtmlStringRGBA(AbilityColor)}>You are gazing at {Target.Data.PlayerName}</color></b>",
                Color.white, new Vector3(0f, 1f, -20f),
                spr: TouCrewAssets.GazeSprite.LoadAsset());
            notif.AdjustNotification();
        }
    }
}
