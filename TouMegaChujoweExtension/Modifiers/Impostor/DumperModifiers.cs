using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using TownOfUs.Utilities.Appearances;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class DumperSpeedModifier : TimedModifier, IVisualAppearance
{
    private readonly float _speedMultiplier;

    public DumperSpeedModifier(float speedMultiplier)
    {
        _speedMultiplier = speedMultiplier;
    }

    public override string ModifierName => "DumperSpeed";
    public override float Duration => 999f; // Managed manually
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
