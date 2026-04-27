using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Events.Crewmate;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers;

public sealed class DoctorVisionBoostModifier : TimedModifier
{
    public override string ModifierName => "Doctor Boost (Vision)";
    public override bool HideOnUi => true;

    private float _duration;
    private DoctorEffectDurationType _durationType;

    public DoctorVisionBoostModifier(float duration, DoctorEffectDurationType durationType)
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

    public override void OnDeactivate()
    {
        if (Player != null && Player.AmOwner)
        {
            DoctorEvents.ShowNotification(Player, "ExtensionDoctorNotificationWoreOffVisionBoost", "Vision boost wore off");
        }
    }
}

public sealed class DoctorRegenerationModifier : TimedModifier
{
    public override string ModifierName => "Doctor Boost (Regen)";
    public override bool HideOnUi => true;

    private float _duration;
    private DoctorEffectDurationType _durationType;

    public DoctorRegenerationModifier(float duration, DoctorEffectDurationType durationType)
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

    public override void OnDeactivate()
    {
        if (Player != null && Player.AmOwner)
        {
            DoctorEvents.ShowNotification(Player, "ExtensionDoctorNotificationWoreOffRegeneration", "Regeneration wore off");
        }
    }
}
