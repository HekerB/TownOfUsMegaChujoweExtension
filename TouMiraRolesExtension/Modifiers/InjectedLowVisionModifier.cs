using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using MiraAPI.Utilities.Assets;
using TouMiraRolesExtension.Events.Impostor;
using TouMiraRolesExtension.Options.Roles.Impostor;
using TownOfUs.Events.Impostor;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMiraRolesExtension.Modifiers;

public sealed class InjectedLowVisionModifier : TimedModifier, IInjectedModifier
{
    public override string ModifierName => "Injected (Low Vision)";
    public override bool HideOnUi => true;
    public override LoadableAsset<Sprite>? ModifierIcon => null;

    private float _duration;
    private InjectorEffectDurationType _durationType;

    public InjectedLowVisionModifier(float duration, InjectorEffectDurationType durationType)
    {
        _duration = duration;
        _durationType = durationType;
    }

    public Guid InjectionId { get; set; }
    public float VisionPerc { get; set; } = 0.5f;

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
            InjectorEvents.ShowEffectWoreOffNotification(Player, "ExtensionInjectorNotificationWoreOffLowVision");
        }
    }

    public string GetEffectDescription()
    {
        return TouLocale.GetParsed("ExtensionInjectorEffectDescriptionLowVision", "0.5x vision");
    }
}