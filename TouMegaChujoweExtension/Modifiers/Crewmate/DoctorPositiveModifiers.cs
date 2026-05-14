using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using TouMegaChujoweExtension.Events.Crewmate;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities.Appearances;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Crewmate;

public sealed class DoctorVisionBoostModifier : TimedModifier
{
    public override string ModifierName => TouLocale.Get("ExtensionModifierDoctorVisionBoost", "Vision Boost");
    public override bool HideOnUi => true;

    private float _duration;
    private DoctorEffectDurationType _durationType;

    public DoctorVisionBoostModifier(float duration, DoctorEffectDurationType durationType)
    {
        _duration = duration;
        _durationType = durationType;
    }

    public float VisionPerc { get; set; } = 1.8f;

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

    public override void OnMeetingStart()
    {
        if (_durationType == DoctorEffectDurationType.AllRound)
        {
            Player.RemoveModifier(this);
        }
    }

    public override void OnDeactivate()
    {
        VisionPerc = 1f;
    }
}

public sealed class DoctorRegenerationModifier : TimedModifier
{
    public override string ModifierName => TouLocale.Get("ExtensionModifierDoctorRegeneration", "Regeneration");
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

    public override void OnMeetingStart()
    {
        if (_durationType == DoctorEffectDurationType.AllRound)
        {
            Player.RemoveModifier(this);
        }
    }

    public override void FixedUpdate()
    {
        if (Player == null || !Player.AmOwner) return;

        // Speed up kill timer (and potentially other timers if they use killTimer)
        if (Player.killTimer > 0f)
        {
            // Speed up by 2x (so we subtract an extra 1x deltaTime)
            Player.killTimer -= Time.fixedDeltaTime;
        }
    }

    public override void OnDeactivate()
    {
    }
}