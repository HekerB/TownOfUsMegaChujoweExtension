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
    /// <summary>
    /// Never listed in the HUD. An Injector's victim is not supposed to know, and the Arcanist - who
    /// gets these from a tarot card - was shown a modifier counting down "30 / 999999", because an
    /// all-game effect has no meaningful duration to display. The card notification already says what
    /// they drew.
    /// </summary>
    public override bool HideOnUi => true;
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

        // Only when an Injector actually did this. The Arcanist hands out the same effect from a card,
        // and firing the Injector's own wear-off line there told the player there was an Injector in
        // the game when there was not.
        if (isInjected && Player != null && Player.AmOwner)
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
        return TouLocale.GetParsed("ExtensionInjectorEffectDescriptionSpeedBoost", "1.5x speed");
    }
}















