using MiraAPI.Modifiers.Types;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TownOfUs.Events.Impostor;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities.Appearances;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class InjectedWeaknessModifier : TimedModifier, IVisualAppearance, IInjectedModifier
{
    public override string ModifierName => "Injected (Weakness)";
    public override bool HideOnUi => true;
    public override LoadableAsset<Sprite>? ModifierIcon => null;

    private readonly float _duration;
    private readonly InjectorEffectDurationType _durationType;

    public InjectedWeaknessModifier(float duration, InjectorEffectDurationType durationType)
    {
        _duration = duration;
        _durationType = durationType;
    }

    public Guid InjectionId { get; set; }
    public float SpeedFactor { get; set; } = 0.6f;

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

    public override void OnActivate()
    {
        Player.RawSetAppearance(this);
    }

    public override void OnDeactivate()
    {
        Player?.ResetAppearance(fullReset: true);
    }

    public override void OnMeetingStart()
    {
        if (_durationType == InjectorEffectDurationType.AllRound)
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

    public string GetEffectDescription()
    {
        return TouLocale.GetParsed("ExtensionInjectorEffectDescriptionWeakness", "0.6x speed");
    }
}















