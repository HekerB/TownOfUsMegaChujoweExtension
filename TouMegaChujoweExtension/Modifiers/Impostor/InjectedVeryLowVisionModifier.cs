using MiraAPI.Modifiers.Types;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TownOfUs.Events.Impostor;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class InjectedVeryLowVisionModifier(float duration, InjectorEffectDurationType durationType, bool isInjected) 
    : TimedModifier, IInjectedModifier
{
    public InjectedVeryLowVisionModifier(float duration, InjectorEffectDurationType durationType) : this(duration, durationType, true) { }
    public override string ModifierName => GetModifierName();
    public override bool HideOnUi => isInjected;
    public override LoadableAsset<Sprite>? ModifierIcon => null;

    private string GetModifierName()
    {
        if (isInjected) return "Injected (Very Low Vision)";
        return durationType == InjectorEffectDurationType.AllGame
            ? TouLocale.Get("ModifierNameLowVision", "Low vision")
            : TouLocale.Get("ModifierNameTemporaryLowVision", "Temporary low vision");
    }

    public Guid InjectionId { get; set; }
    public float VisionPerc { get; set; } = 0.1f;

    public override float Duration
    {
        get
        {
            return durationType switch
            {
                InjectorEffectDurationType.AllRound => -1f,
                InjectorEffectDurationType.AllGame => -1f,
                InjectorEffectDurationType.SetTime => duration,
                _ => duration
            };
        }
    }

    public override bool AutoStart => true;

    public override void OnMeetingStart()
    {
        if (durationType == InjectorEffectDurationType.AllRound)
        {
            Player.RemoveModifier(this);
        }
    }

    public override void OnDeactivate()
    {
        VisionPerc = 1f;
    }

    public string GetEffectDescription()
    {
        return TouLocale.GetParsed("ExtensionInjectorEffectDescriptionVeryLowVision", "0.1x vision");
    }
}















