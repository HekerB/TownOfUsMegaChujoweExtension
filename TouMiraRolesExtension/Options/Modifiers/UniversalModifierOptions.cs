using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using CluelessCensorTypeEnum = TouMiraRolesExtension.Options.Modifiers.CluelessCensorType;
using SpitefulEffectTypeEnum = TouMiraRolesExtension.Options.Modifiers.SpitefulEffectType;
using SpitefulDurationTypeEnum = TouMiraRolesExtension.Options.Modifiers.SpitefulDurationType;

namespace TouMiraRolesExtension.Options.Modifiers;

public enum CluelessCensorType
{
    WhiteBars,
    Asterisks,
    QuestionMarks,
    Remove
}

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

public sealed class UniversalModifierOptions : AbstractOptionGroup
{
    public override string GroupName => "Universal Modifiers (Extension)";
    public override bool ShowInModifiersMenu => true;
    public override uint GroupPriority => 1;

    [ModdedNumberOption("ExtensionModifierCluelessAmount", 0, 15)]
    public float CluelessAmount { get; set; } = 0;

    public ModdedNumberOption CluelessChance { get; } =
        new("ExtensionModifierCluelessChance", 50f, 0, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<UniversalModifierOptions>.Instance.CluelessAmount > 0
        };

    private static readonly string[] CluelessCensorTypeValues =
    [
        "ExtensionModifierCluelessCensorTypeEnumWhiteBars",
        "ExtensionModifierCluelessCensorTypeEnumAsterisks",
        "ExtensionModifierCluelessCensorTypeEnumQuestionMarks",
        "ExtensionModifierCluelessCensorTypeEnumRemove"
    ];

    public ModdedEnumOption<CluelessCensorType> CluelessCensorType { get; } =
        new("ExtensionModifierCluelessCensorType", CluelessCensorTypeEnum.Asterisks, CluelessCensorTypeValues)
        {
            Visible = () => OptionGroupSingleton<UniversalModifierOptions>.Instance.CluelessAmount > 0
        };

    [ModdedNumberOption("ExtensionModifierSpitefulAmount", 0, 15)]
    public float SpitefulAmount { get; set; } = 0;

    public ModdedNumberOption SpitefulChance { get; } =
        new("ExtensionModifierSpitefulChance", 50f, 0, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<UniversalModifierOptions>.Instance.SpitefulAmount > 0
        };

    [ModdedNumberOption("ExtensionModifierSpitefulImpact", 25f, 300f, 25f, MiraNumberSuffixes.Percent)]
    public float SpitefulImpact { get; set; } = 100f;

    private static readonly string[] SpitefulEffectTypeValues =
    [
        "ExtensionModifierSpitefulEffectTypeEnumLowerVision",
        "ExtensionModifierSpitefulEffectTypeEnumSlowness",
        "ExtensionModifierSpitefulEffectTypeEnumIncreasedCooldowns"
    ];

    public ModdedEnumOption<SpitefulEffectType> SpitefulEffectType { get; } =
        new("ExtensionModifierSpitefulEffectType", SpitefulEffectTypeEnum.LowerVision, SpitefulEffectTypeValues)
        {
            Visible = () => OptionGroupSingleton<UniversalModifierOptions>.Instance.SpitefulAmount > 0
        };

    private static readonly string[] SpitefulDurationTypeValues =
    [
        "ExtensionModifierSpitefulDurationTypeEnumNextRounds",
        "ExtensionModifierSpitefulDurationTypeEnumRestOfGame"
    ];

    public ModdedEnumOption<SpitefulDurationType> SpitefulDurationType { get; } =
        new("ExtensionModifierSpitefulDurationType", SpitefulDurationTypeEnum.NextRounds, SpitefulDurationTypeValues)
        {
            Visible = () => OptionGroupSingleton<UniversalModifierOptions>.Instance.SpitefulAmount > 0
        };

    public ModdedNumberOption SpitefulRoundCount { get; } =
        new("ExtensionModifierSpitefulRoundCount", 1, 1, 5, 1)
        {
            Visible = () => OptionGroupSingleton<UniversalModifierOptions>.Instance.SpitefulAmount > 0 &&
                             OptionGroupSingleton<UniversalModifierOptions>.Instance.SpitefulDurationType.Value == Options.Modifiers.SpitefulDurationType.NextRounds
        };
}