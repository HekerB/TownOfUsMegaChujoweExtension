using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using TownOfUs.Modifiers;
using TownOfUs.Utilities.Appearances;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers;

public sealed class SandwormInvisibleModifier : ConcealedModifier, IVisualAppearance
{
    public override string ModifierName => "Invisible";
    public override bool HideOnUi => true;
    public override bool AutoStart => true;

    public VisualAppearance GetVisualAppearance()
    {
        // Invisible to non-impostors
        var playerColor = (PlayerControl.LocalPlayer.IsImpostorAligned())
            ? new Color(0f, 0f, 0f, 0.1f)
            : Color.clear;

        return new VisualAppearance(Player.GetDefaultModifiedAppearance(), TownOfUsAppearances.Swooper)
        {
            HatId = string.Empty,
            SkinId = string.Empty,
            VisorId = string.Empty,
            PlayerName = string.Empty,
            PetId = string.Empty,
            RendererColor = playerColor,
            NameColor = Color.clear,
            ColorBlindTextColor = Color.clear
        };
    }

    public override void OnActivate()
    {
        Player.RawSetAppearance(this);
        Player.cosmetics.ToggleNameVisible(false);
    }

    public override void OnDeactivate()
    {
        Player.ResetAppearance();
        Player.cosmetics.ToggleNameVisible(true);
    }
}

public sealed class SandwormSpeedModifier : TimedModifier, IVisualAppearance
{
    private readonly float _speedMultiplier;

    public SandwormSpeedModifier(float speedMultiplier)
    {
        _speedMultiplier = speedMultiplier;
    }

    public override string ModifierName => "Speed";
    public override float Duration => 10f; // Managed by role
    public override bool HideOnUi => true;

    public VisualAppearance GetVisualAppearance()
    {
        var appearance = Player.GetDefaultModifiedAppearance();
        appearance.Speed *= _speedMultiplier;
        return appearance;
    }

    public override void OnActivate()
    {
        Player.RawSetAppearance(this);
    }

    public override void OnDeactivate()
    {
        Player.ResetAppearance();
    }
}
