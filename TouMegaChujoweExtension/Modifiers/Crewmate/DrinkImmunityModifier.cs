using MiraAPI.Modifiers.Types;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TownOfUs.Assets;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Crewmate;

public sealed class DrinkImmunityModifier : TimedModifier
{
    public override string ModifierName => "Drink Immunity";
    public override bool HideOnUi => false;
    public override LoadableAsset<Sprite>? ModifierIcon => TouModifierIcons.Drunk;

    private readonly float _duration;

    public DrinkImmunityModifier(float duration)
    {
        _duration = duration;
    }

    public override float Duration => _duration;

    public override bool AutoStart => true;

    public override void OnMeetingStart()
    {
        Player.RemoveModifier(this);
    }
}
