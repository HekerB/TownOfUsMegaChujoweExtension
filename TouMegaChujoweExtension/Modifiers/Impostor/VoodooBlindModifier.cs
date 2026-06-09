using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class VoodooBlindModifier : TimedModifier
{
    private float _duration;

    public VoodooBlindModifier(float duration)
    {
        _duration = duration;
    }

    public float VisionPerc { get; set; } = 0.3f;

    public override string ModifierName => "Voodoo Blindness";
    public override bool HideOnUi => true;
    public override LoadableAsset<Sprite>? ModifierIcon => null;

    public override float Duration => _duration;
    public override bool AutoStart => true;

    public override void OnMeetingStart()
    {
        Player.RemoveModifier(this);
    }

    public override void OnDeactivate()
    {
        VisionPerc = 1f;
    }
}
