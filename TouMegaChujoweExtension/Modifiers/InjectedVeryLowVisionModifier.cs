using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Events.Impostor;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Events.Impostor;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers;

public sealed class InjectedVeryLowVisionModifier : TimedModifier, IInjectedModifier
{
    public override string ModifierName => "Injected (Very Low Vision)";
    public override bool HideOnUi => true;
    public override LoadableAsset<Sprite>? ModifierIcon => null;

    private float _duration;
    private InjectorEffectDurationType _durationType;

    public InjectedVeryLowVisionModifier(float duration, InjectorEffectDurationType durationType)
    {
        _duration = duration;
        _durationType = durationType;
    }

    public Guid InjectionId { get; set; }
    public float VisionPerc { get; set; } = 0.1f;

    public override float Duration
    {
        get
        {
            return _durationType switch
            {
                InjectorEffectDurationType.AllRound => -1f,
                InjectorEffectDurationType.AllGame => -1f,
                InjectorEffectDurationType.SetTime => _duration,
                _ => _duration
            };
        }
    }

    public override bool AutoStart => true;

    public override void OnMeetingStart()
    {
        if (_durationType == InjectorEffectDurationType.AllRound)
        {
            Player.RemoveModifier(this);
        }
    }

    public override void OnDeactivate()
    {
        VisionPerc = 1f;
        if (Player != null && Player.AmOwner)
        {
            InjectorEvents.ShowEffectWoreOffNotification(Player, "ExtensionInjectorNotificationWoreOffVeryLowVision");
        }
    }

    public string GetEffectDescription()
    {
        return TouLocale.GetParsed("ExtensionInjectorEffectDescriptionVeryLowVision", "0.1x vision");
    }
}