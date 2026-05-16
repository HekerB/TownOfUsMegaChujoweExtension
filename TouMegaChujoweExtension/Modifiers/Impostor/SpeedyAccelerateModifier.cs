using MiraAPI.Modifiers.Types;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using MiraAPI.Hud;
using TownOfUs.Modifiers;
using TownOfUs.Extensions;
using TownOfUs.Utilities.Appearances;
using TownOfUs.Utilities;
using TownOfUs.Assets;
using UnityEngine;
using TouMegaChujoweExtension.Options.Roles.Impostor;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class SpeedyAccelerateModifier : ConcealedModifier, IVisualAppearance
{
    public override string ModifierName => "Accelerated";
    public override float Duration => OptionGroupSingleton<SpeedyOptions>.Instance.AccelerateDuration;
    public override bool AutoStart => true;
    public override bool HideOnUi => true;
    public override bool VisibleToOthers => true;
    public bool VisualPriority => true;

    public override void OnDeath(DeathReason reason)
    {
        Player.RemoveModifier(this);
    }

    public override void OnMeetingStart()
    {
        Player.RemoveModifier(this);
    }

    public override void OnActivate()
    {
        Player.RawSetAppearance(this);
        Player.cosmetics.ToggleNameVisible(false);

        if (Player.AmOwner)
        {
            TouAudio.PlaySound(TouExtensionAudio.WraithDashSound);

            var button = CustomButtonSingleton<SpeedyAccelerateButton>.Instance;
            if (button != null)
            {
                button.EffectActive = true;
                button.Timer = Duration;
            }
        }
    }

    public override void OnDeactivate()
    {
        Player.ResetAppearance();
        Player.cosmetics.ToggleNameVisible(true);

        if (Player.AmOwner)
        {
            var button = CustomButtonSingleton<SpeedyAccelerateButton>.Instance;
            if (button != null)
            {
                button.EffectActive = false;
                button.Timer = button.Cooldown;
            }
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        Player.RawSetAppearance(this);
        Player.cosmetics.ToggleNameVisible(false);
    }

    public VisualAppearance GetVisualAppearance()
    {
        var options = OptionGroupSingleton<SpeedyOptions>.Instance;
        var appearance = Player.GetDefaultAppearance();
        appearance.Speed = options.AccelerationBuff;
        appearance.HatId = "hat_NoHat";
        appearance.SkinId = "skin_None";
        appearance.VisorId = "visor_EmptyVisor";
        appearance.PetId = "pet_EmptyPet";
        appearance.PlayerName = string.Empty;
        appearance.NameVisible = false;
        appearance.PlayerMaterialColor = Color.grey;
        appearance.NameColor = Color.clear;
        appearance.ColorBlindTextColor = Color.clear;
        return appearance;
    }
}
