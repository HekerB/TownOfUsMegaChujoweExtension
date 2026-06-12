using MiraAPI.Modifiers.Types;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TownOfUs.Assets;
using TownOfUs.Modifiers;
using UnityEngine;


namespace TouMegaChujoweExtension.Modifiers.Crewmate;

public sealed class RoleblockedModifier : DisabledModifier
{
    public override string ModifierName => "Roleblocked";
    public override bool HideOnUi => false;
    public override LoadableAsset<Sprite>? ModifierIcon => TouModifierIcons.Drunk;

    public bool InvertControls { get; }
    public bool ApplyImmunity { get; }
    private readonly float _duration;
    public float ImmunityDuration { get; }

    public RoleblockedModifier(bool invertControls, bool applyImmunity, float duration, float immunityDuration)
    {
        InvertControls = invertControls;
        ApplyImmunity = applyImmunity;
        _duration = duration;
        ImmunityDuration = immunityDuration;
    }

    public override bool CanUseAbilities => false;

    public override float Duration => _duration;

    public override bool AutoStart => true;

    public override void OnMeetingStart()
    {
        Player.RemoveModifier(this);
    }

    public override void OnDeactivate()
    {
        if (ApplyImmunity && Player != null)
        {
            Player.AddModifier<DrinkImmunityModifier>(ImmunityDuration);
        }
    }
}
