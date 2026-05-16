using MiraAPI.Modifiers.Types;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TownOfUs.Events.Impostor;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities.Appearances;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class InjectedSpeedBoostModifier(float duration, InjectorEffectDurationType durationType, bool isInjected) 
    : TimedModifier, IVisualAppearance, IInjectedModifier
{
    public InjectedSpeedBoostModifier(float duration, InjectorEffectDurationType durationType) : this(duration, durationType, true) { }
    public override string ModifierName => GetModifierName();
    public override bool HideOnUi => isInjected;
    public override LoadableAsset<Sprite>? ModifierIcon => null;

    private string GetModifierName()
    {
        if (isInjected) return "Injected (Speed Boost)";
        return durationType == InjectorEffectDurationType.AllGame
            ? TouLocale.Get("ModifierNameSpeedBoost", "Speed boost")
            : TouLocale.Get("ModifierNameTemporarySpeedBoost", "Temporary speed boost");
    }

    public Guid InjectionId { get; set; }
    public float SpeedFactor { get; set; } = 1.5f;

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

    public override void OnActivate()
    {
        Player.RawSetAppearance(this);
    }

    public override void OnDeactivate()
    {
        Player?.ResetAppearance(fullReset: true);
        if (Player != null && Player.AmOwner)
        {
            InjectorEvents.ShowEffectWoreOffNotification(Player, "ExtensionInjectorNotificationWoreOffSpeedBoost");
        }
    }

    public override void OnMeetingStart()
    {
        if (durationType == InjectorEffectDurationType.AllRound)
        {
            Player.RemoveModifier(this);
        }
    }

    public VisualAppearance GetVisualAppearance()
    {
        var appearance = Player.GetDefaultAppearance();
        appearance.Speed = SpeedFactor;
        return appearance;
    }

    public string GetEffectDescription()
    {
        return TouLocale.GetParsed("ExtensionInjectorEffectDescriptionSpeedBoost", "1.5x speed");
    }
}















