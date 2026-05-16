using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Events.Crewmate;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities.Appearances;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Crewmate;

public sealed class DoctorSpeedBoostModifier : TimedModifier, IVisualAppearance
{
    public override string ModifierName => "Doctor Boost (Speed)";
    public override bool HideOnUi => true;

    private readonly float _duration;
    private readonly DoctorEffectDurationType _durationType;

    public DoctorSpeedBoostModifier(float duration, DoctorEffectDurationType durationType)
    {
        _duration = duration;
        _durationType = durationType;
    }

    public override float Duration
    {
        get
        {
            return _durationType switch
            {
                DoctorEffectDurationType.AllRound => -1f,
                DoctorEffectDurationType.AllGame => -1f,
                DoctorEffectDurationType.SetTime => _duration,
                _ => _duration
            };
        }
    }

    public override void OnActivate() => Player.RawSetAppearance(this);

    public override void OnMeetingStart()
    {
        if (_durationType == DoctorEffectDurationType.AllRound)
        {
            Player.RemoveModifier(this);
        }
    }

    public override void OnDeactivate()
    {
        Player?.ResetAppearance(fullReset: true);
    }

    public VisualAppearance GetVisualAppearance()
    {
        var appearance = Player.GetDefaultAppearance();
        appearance.Speed = 1.4f;
        return appearance;
    }
}
