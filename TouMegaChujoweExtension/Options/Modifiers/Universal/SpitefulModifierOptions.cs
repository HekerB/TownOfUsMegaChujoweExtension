using MiraAPI.GameOptions;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Modifiers.Universal;
using UnityEngine;
using SpitefulDurationTypeEnum = TouMegaChujoweExtension.Options.Modifiers.SpitefulDurationType;
using SpitefulEffectTypeEnum = TouMegaChujoweExtension.Options.Modifiers.SpitefulEffectType;

namespace TouMegaChujoweExtension.Options.Modifiers;

public enum SpitefulEffectType
{
    LowerVision,
    Slowness,
    IncreasedCooldowns
}

public enum SpitefulDurationType
{
    NextRounds,
    RestOfGame
}

public sealed class SpitefulModifierOptions : AbstractOptionGroup<SpitefulModifier>
{
    public override string GroupName => "Spiteful";
    public override Color GroupColor => Palette.ImpostorRoleHeaderRed;
    public override bool ShowInModifiersMenu => true;
    public override uint GroupPriority => 51;

    public ModdedNumberOption SpitefulImpact { get; } =
        new("ExtensionModifierSpitefulImpact", 35f, 15f, 120f, 5f, MiraNumberSuffixes.Percent);

    private static readonly string[] SpitefulEffectTypeValues =
    [
        "ExtensionModifierSpitefulEffectTypeEnumLowerVision",
        "ExtensionModifierSpitefulEffectTypeEnumSlowness",
        "ExtensionModifierSpitefulEffectTypeEnumIncreasedCooldowns"
    ];

    public ModdedEnumOption<SpitefulEffectType> SpitefulEffectType { get; } =
        new("ExtensionModifierSpitefulEffectType", SpitefulEffectTypeEnum.IncreasedCooldowns, SpitefulEffectTypeValues);

    private static readonly string[] SpitefulDurationTypeValues =
    [
        "ExtensionModifierSpitefulDurationTypeEnumNextRounds",
        "ExtensionModifierSpitefulDurationTypeEnumRestOfGame"
    ];

    public ModdedEnumOption<SpitefulDurationType> SpitefulDurationType { get; } =
        new("ExtensionModifierSpitefulDurationType", SpitefulDurationTypeEnum.RestOfGame, SpitefulDurationTypeValues);

    public ModdedNumberOption SpitefulRoundCount { get; } =
        new("ExtensionModifierSpitefulRoundCount", 1f, 1f, 5f, 1f, MiraNumberSuffixes.None)
        {
            Visible = () => OptionGroupSingleton<SpitefulModifierOptions>.Instance.SpitefulDurationType.Value == SpitefulDurationTypeEnum.NextRounds
        };
}
