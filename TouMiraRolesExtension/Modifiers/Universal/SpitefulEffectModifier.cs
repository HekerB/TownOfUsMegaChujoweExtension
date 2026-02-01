using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using TouMiraRolesExtension.Options.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using TownOfUs.Utilities.Appearances;
using UnityEngine;

namespace TouMiraRolesExtension.Modifiers.Universal;

public sealed class SpitefulEffectModifier : TimedModifier, IVisualAppearance
{
    public override string ModifierName => "Spiteful (Effect)";
    public override bool HideOnUi => true;
    public override bool AutoStart => true;

    private SpitefulEffectType _effectType;
    private SpitefulDurationType _durationType;
    private int _roundsRemaining;
    private float _impact;

    public SpitefulEffectModifier(SpitefulEffectType effectType, SpitefulDurationType durationType, int rounds, float impact)
    {
        _effectType = effectType;
        _durationType = durationType;
        _roundsRemaining = rounds;
        _impact = impact;
    }

    public SpitefulEffectType EffectType => _effectType;
    public float ImpactMultiplier => _impact / 100f;

    public override float Duration => -1f; // Managed by rounds or rest of game

    public override void OnActivate()
    {
        if (_effectType == SpitefulEffectType.Slowness)
        {
            Player.RawSetAppearance(this);
        }
    }

    public override void OnDeactivate()
    {
        if (_effectType == SpitefulEffectType.Slowness)
        {
            Player?.ResetAppearance(fullReset: true);
        }
    }

    public override void OnMeetingStart()
    {
        if (_durationType == SpitefulDurationType.NextRounds)
        {
            _roundsRemaining--;
            if (_roundsRemaining <= 0)
            {
                Player.RemoveModifier(this);
            }
        }
    }

    public VisualAppearance GetVisualAppearance()
    {
        var appearance = Player.GetDefaultAppearance();
        if (_effectType == SpitefulEffectType.Slowness)
        {
            appearance.Speed = 0.5f; // Configurable? User said "Slowness", I'll use 0.5f as default like other slowness effects
        }
        return appearance;
    }

    public string GetEffectDescription()
    {
        return _effectType switch
        {
            SpitefulEffectType.LowerVision => TouLocale.Get("ExtensionModifierSpitefulEffectLowerVision"),
            SpitefulEffectType.Slowness => TouLocale.Get("ExtensionModifierSpitefulEffectSlowness"),
            SpitefulEffectType.IncreasedCooldowns => TouLocale.Get("ExtensionModifierSpitefulEffectIncreasedCooldowns"),
            _ => string.Empty
        };
    }
}