using MiraAPI.Modifiers.Types;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TownOfUs.Events.Impostor;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class InjectedVisionBoostModifier(float duration, InjectorEffectDurationType durationType, bool isInjected) 
    : TimedModifier, IInjectedModifier
{
    public InjectedVisionBoostModifier(float duration, InjectorEffectDurationType durationType) : this(duration, durationType, true) { }
    public override string ModifierName => GetModifierName();
    public override bool HideOnUi => isInjected;
    public override LoadableAsset<Sprite>? ModifierIcon => null;

    private string GetModifierName()
    {
        if (isInjected) return "Injected (Vision Boost)";
        return durationType == InjectorEffectDurationType.AllGame
            ? TouLocale.Get("ModifierNameVisionBoost", "Vision boost")
            : TouLocale.Get("ModifierNameTemporaryVisionBoost", "Temporary vision boost");
    }

    public Guid InjectionId { get; set; }
    public float VisionPerc { get; set; } = 1.5f;

    public override float Duration
    {
        get
        {
            return durationType switch
            {
                InjectorEffectDurationType.AllRound => 999999f,
                InjectorEffectDurationType.AllGame => 999999f,
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
        return TouLocale.GetParsed("ExtensionInjectorEffectDescriptionVisionBoost", "1.5x vision");
    }
}















