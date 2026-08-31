using MiraAPI.Modifiers.Types;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TownOfUs.Events.Impostor;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities.Appearances;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class InjectedSlownessModifier(float duration, InjectorEffectDurationType durationType, bool isInjected) 
    : TimedModifier, IVisualAppearance, IInjectedModifier
{
    public InjectedSlownessModifier(float duration, InjectorEffectDurationType durationType) : this(duration, durationType, true) { }
    public override string ModifierName => GetModifierName();
    // Never listed in the HUD - see the note on InjectedSpeedBoostModifier.
    public override bool HideOnUi => true;
    public override LoadableAsset<Sprite>? ModifierIcon => null;

    private string GetModifierName()
    {
        if (isInjected) return "Injected (Slowness)";
        return durationType == InjectorEffectDurationType.AllGame
            ? TouLocale.Get("ModifierNameSlowness", "Slowness")
            : TouLocale.Get("ModifierNameTemporarySlowness", "Temporary slowness");
    }

    public Guid InjectionId { get; set; }
    public float SpeedFactor { get; set; } = 0.5f;

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
    }

    public string GetEffectDescription()
    {
        return TouLocale.GetParsed("ExtensionInjectorEffectDescriptionSlowness", "0.5x speed");
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
                // GetDefaultModifiedAppearance, not GetDefaultAppearance: the plain default has no idea the
        // player is a Giant or a Mini, so rebuilding from it threw their size away the moment this
        // effect landed - an Arcanist speed card made a Giant an ordinary-sized player. The modified
        // one already folds Mini/Giant in, and the speed is applied on top of theirs rather than
        // replacing it, so a Giant stays proportionally slower while still getting the boost.
        var appearance = Player.GetDefaultModifiedAppearance();
        appearance.Speed *= SpeedFactor;
        return appearance;
    }
}















