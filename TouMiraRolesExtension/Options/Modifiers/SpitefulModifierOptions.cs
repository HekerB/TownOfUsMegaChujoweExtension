using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMiraRolesExtension.Modifiers.Universal;
using SpitefulEffectTypeEnum = TouMiraRolesExtension.Options.Modifiers.SpitefulEffectType;
using SpitefulDurationTypeEnum = TouMiraRolesExtension.Options.Modifiers.SpitefulDurationType;

namespace TouMiraRolesExtension.Options.Modifiers;

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
    public override string GroupName => "Spiteful (Extension)";
    public override bool ShowInModifiersMenu => true;
    public override uint GroupPriority => 1;

    [ModdedNumberOption("ExtensionModifierSpitefulAmount", 0, 15)]
    public float SpitefulAmount { get; set; } = 0;

    public ModdedNumberOption SpitefulChance { get; } =
        new("ExtensionModifierSpitefulChance", 50f, 0, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<SpitefulModifierOptions>.Instance.SpitefulAmount > 0
        };

    public ModdedNumberOption SpitefulImpact { get; } =
        new("ExtensionModifierSpitefulImpact", 25f, 15f, 75f, 5f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<SpitefulModifierOptions>.Instance.SpitefulAmount > 0
        };

    private static readonly string[] SpitefulEffectTypeValues =
    [
        "ExtensionModifierSpitefulEffectTypeEnumLowerVision",
        "ExtensionModifierSpitefulEffectTypeEnumSlowness",
        "ExtensionModifierSpitefulEffectTypeEnumIncreasedCooldowns"
    ];

    public ModdedEnumOption<SpitefulEffectType> SpitefulEffectType { get; } =
        new("ExtensionModifierSpitefulEffectType", SpitefulEffectTypeEnum.IncreasedCooldowns, SpitefulEffectTypeValues)
        {
            Visible = () => OptionGroupSingleton<SpitefulModifierOptions>.Instance.SpitefulAmount > 0
        };

    private static readonly string[] SpitefulDurationTypeValues =
    [
        "ExtensionModifierSpitefulDurationTypeEnumNextRounds",
        "ExtensionModifierSpitefulDurationTypeEnumRestOfGame"
    ];

    public ModdedEnumOption<SpitefulDurationType> SpitefulDurationType { get; } =
        new("ExtensionModifierSpitefulDurationType", SpitefulDurationTypeEnum.RestOfGame, SpitefulDurationTypeValues)
        {
            Visible = () => OptionGroupSingleton<SpitefulModifierOptions>.Instance.SpitefulAmount > 0
        };

    public ModdedNumberOption SpitefulRoundCount { get; } =
        new("ExtensionModifierSpitefulRoundCount", 1f, 1f, 5f, 1f, MiraNumberSuffixes.None)
        {
            Visible = () => OptionGroupSingleton<SpitefulModifierOptions>.Instance.SpitefulAmount > 0 &&
                             OptionGroupSingleton<SpitefulModifierOptions>.Instance.SpitefulDurationType.Value == SpitefulDurationTypeEnum.NextRounds
        };
}