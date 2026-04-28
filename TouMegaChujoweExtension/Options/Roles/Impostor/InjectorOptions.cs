using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public enum InjectorEffectDurationType
{
    AllRound,
    AllGame,
    SetTime
}

public enum InjectorEffectType
{
    InvertedControls,
    LowVision,
    Slowness,
    VeryLowVision,
    Confused,
    NoVent,
    NoUse,
    NoReport,
    Nausea,
    Weakness,
    SpeedBoost,
    VisionBoost,
    Regeneration
}

public sealed class InjectorOptions : AbstractOptionGroup<InjectorRole>, IWikiOptionsSummaryProvider
{
    public override string GroupName => TouLocale.Get("ExtensionRoleInjector", "Injector");

    [ModdedNumberOption("ExtensionOptionInjectorInjectCooldown", 5f, 120f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float InjectCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionInjectorEffectDelay", 0f, 30f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float EffectDelay { get; set; } = 5f;

    public ModdedNumberOption EffectDurationOption { get; } = new("ExtensionOptionInjectorEffectDuration", 45f, 5f, 200f, 5f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<InjectorOptions>.Instance.EffectDurationType.Value == InjectorEffectDurationType.SetTime
    };

    public float EffectDuration => EffectDurationOption.Value;

    public InjectorOptions()
    {
    }

    [ModdedNumberOption("ExtensionOptionInjectorInitialUses", 0, 15)]
    public float InitialUses { get; set; } = 4f;

    [ModdedNumberOption("ExtensionOptionInjectorUsesPerKill", 0, 5)]
    public float UsesPerKill { get; set; } = 1f;

    [ModdedToggleOption("ExtensionOptionInjectorSharedCooldown")]
    public bool SharedCooldown { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionInjectorPositiveEffectsEnabled")]
    public bool PositiveEffectsEnabled { get; set; } = true;

    private static readonly string[] EffectDurationTypeValues =
    [
        "ExtensionOptionInjectorEffectDurationTypeEnumAllRound",
        "ExtensionOptionInjectorEffectDurationTypeEnumAllGame",
        "ExtensionOptionInjectorEffectDurationTypeEnumSetTime"
    ];

    public ModdedEnumOption<InjectorEffectDurationType> EffectDurationType { get; } =
        new("ExtensionOptionInjectorEffectDurationType", InjectorEffectDurationType.SetTime, EffectDurationTypeValues);

    private static readonly string[] EffectTypeValues =
    [
        "ExtensionOptionInjectorEffectTypeEnumInvertedControls",
        "ExtensionOptionInjectorEffectTypeEnumLowVision",
        "ExtensionOptionInjectorEffectTypeEnumSlowness",
        "ExtensionOptionInjectorEffectTypeEnumVeryLowVision",
        "ExtensionOptionInjectorEffectTypeEnumConfused",
        "ExtensionOptionInjectorEffectTypeEnumNoVent",
        "ExtensionOptionInjectorEffectTypeEnumNoUse",
        "ExtensionOptionInjectorEffectTypeEnumNoReport",
        "ExtensionOptionInjectorEffectTypeEnumNausea",
        "ExtensionOptionInjectorEffectTypeEnumWeakness",
        "ExtensionOptionInjectorEffectTypeEnumSpeedBoost",
        "ExtensionOptionInjectorEffectTypeEnumVisionBoost",
        "ExtensionOptionInjectorEffectTypeEnumRegeneration"
    ];

    public ModdedEnumOption<InjectorEffectType> SelectedEffectType { get; } =
        new("ExtensionOptionInjectorSelectedEffectType", InjectorEffectType.InvertedControls, EffectTypeValues);

    // Individual chance options for each effect type - only the selected one is visible
    public ModdedNumberOption ChanceInvertedControlsOption { get; } =
        new("ExtensionOptionInjectorChanceInvertedControls", 30f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<InjectorOptions>.Instance.SelectedEffectType.Value == InjectorEffectType.InvertedControls
        };

    public ModdedNumberOption ChanceLowVisionOption { get; } =
        new("ExtensionOptionInjectorChanceLowVision", 30f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<InjectorOptions>.Instance.SelectedEffectType.Value == InjectorEffectType.LowVision
        };

    public ModdedNumberOption ChanceSlownessOption { get; } =
        new("ExtensionOptionInjectorChanceSlowness", 30f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<InjectorOptions>.Instance.SelectedEffectType.Value == InjectorEffectType.Slowness
        };

    public ModdedNumberOption ChanceVeryLowVisionOption { get; } =
        new("ExtensionOptionInjectorChanceVeryLowVision", 50f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<InjectorOptions>.Instance.SelectedEffectType.Value == InjectorEffectType.VeryLowVision
        };

    public ModdedNumberOption ChanceConfusedOption { get; } =
        new("ExtensionOptionInjectorChanceConfused", 40f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<InjectorOptions>.Instance.SelectedEffectType.Value == InjectorEffectType.Confused
        };

    public ModdedNumberOption ChanceNoVentOption { get; } =
        new("ExtensionOptionInjectorChanceNoVent", 60f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<InjectorOptions>.Instance.SelectedEffectType.Value == InjectorEffectType.NoVent
        };

    public ModdedNumberOption ChanceNoUseOption { get; } =
        new("ExtensionOptionInjectorChanceNoUse", 30f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<InjectorOptions>.Instance.SelectedEffectType.Value == InjectorEffectType.NoUse
        };

    public ModdedNumberOption ChanceNoReportOption { get; } =
        new("ExtensionOptionInjectorChanceNoReport", 30f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<InjectorOptions>.Instance.SelectedEffectType.Value == InjectorEffectType.NoReport
        };

    public ModdedNumberOption ChanceNauseaOption { get; } =
        new("ExtensionOptionInjectorChanceNausea", 50f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<InjectorOptions>.Instance.SelectedEffectType.Value == InjectorEffectType.Nausea
        };

    public ModdedNumberOption ChanceWeaknessOption { get; } =
        new("ExtensionOptionInjectorChanceWeakness", 20f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<InjectorOptions>.Instance.SelectedEffectType.Value == InjectorEffectType.Weakness
        };

    public ModdedNumberOption ChanceSpeedBoostOption { get; } =
        new("ExtensionOptionInjectorChanceSpeedBoost", 10f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<InjectorOptions>.Instance.SelectedEffectType.Value == InjectorEffectType.SpeedBoost &&
                            OptionGroupSingleton<InjectorOptions>.Instance.PositiveEffectsEnabled
        };

    public ModdedNumberOption ChanceVisionBoostOption { get; } =
        new("ExtensionOptionInjectorChanceVisionBoost", 10f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<InjectorOptions>.Instance.SelectedEffectType.Value == InjectorEffectType.VisionBoost &&
                            OptionGroupSingleton<InjectorOptions>.Instance.PositiveEffectsEnabled
        };

    public ModdedNumberOption ChanceRegenerationOption { get; } =
        new("ExtensionOptionInjectorChanceRegeneration", 10f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<InjectorOptions>.Instance.SelectedEffectType.Value == InjectorEffectType.Regeneration &&
                            OptionGroupSingleton<InjectorOptions>.Instance.PositiveEffectsEnabled
        };

    // Properties for backward compatibility with InjectorEvents
    public float GetEffectChance(InjectorEffectType effectType)
    {
        return effectType switch
        {
            InjectorEffectType.InvertedControls => ChanceInvertedControlsOption.Value,
            InjectorEffectType.LowVision => ChanceLowVisionOption.Value,
            InjectorEffectType.Slowness => ChanceSlownessOption.Value,
            InjectorEffectType.VeryLowVision => ChanceVeryLowVisionOption.Value,
            InjectorEffectType.Confused => ChanceConfusedOption.Value,
            InjectorEffectType.NoVent => ChanceNoVentOption.Value,
            InjectorEffectType.NoUse => ChanceNoUseOption.Value,
            InjectorEffectType.NoReport => ChanceNoReportOption.Value,
            InjectorEffectType.Nausea => ChanceNauseaOption.Value,
            InjectorEffectType.Weakness => ChanceWeaknessOption.Value,
            InjectorEffectType.SpeedBoost => ChanceSpeedBoostOption.Value,
            InjectorEffectType.VisionBoost => ChanceVisionBoostOption.Value,
            InjectorEffectType.Regeneration => ChanceRegenerationOption.Value,
            _ => 0f
        };
    }

    // Public properties for backward compatibility with InjectorEvents
    public float ChanceInvertedControls => ChanceInvertedControlsOption.Value;
    public float ChanceLowVision => ChanceLowVisionOption.Value;
    public float ChanceSlowness => ChanceSlownessOption.Value;
    public float ChanceVeryLowVision => ChanceVeryLowVisionOption.Value;
    public float ChanceConfused => ChanceConfusedOption.Value;
    public float ChanceNoVent => ChanceNoVentOption.Value;
    public float ChanceNoUse => ChanceNoUseOption.Value;
    public float ChanceNoReport => ChanceNoReportOption.Value;
    public float ChanceNausea => ChanceNauseaOption.Value;
    public float ChanceWeakness => ChanceWeaknessOption.Value;
    public float ChanceSpeedBoost => ChanceSpeedBoostOption.Value;
    public float ChanceVisionBoost => ChanceVisionBoostOption.Value;
    public float ChanceRegeneration => ChanceRegenerationOption.Value;

    // IWikiOptionsSummaryProvider implementation
    public IReadOnlySet<StringNames> WikiHiddenOptionKeys =>
        new HashSet<StringNames>
        {
            SelectedEffectType.StringName,
            ChanceInvertedControlsOption.StringName,
            ChanceLowVisionOption.StringName,
            ChanceSlownessOption.StringName,
            ChanceVeryLowVisionOption.StringName,
            ChanceConfusedOption.StringName,
            ChanceNoVentOption.StringName,
            ChanceNoUseOption.StringName,
            ChanceNoReportOption.StringName,
            ChanceNauseaOption.StringName,
            ChanceWeaknessOption.StringName,
            ChanceSpeedBoostOption.StringName,
            ChanceVisionBoostOption.StringName,
            ChanceRegenerationOption.StringName
        };

    public IEnumerable<string> GetWikiOptionSummaryLines()
    {
        var enabledEffects = new List<string>();
        
        // Negative effects
        if (GetEffectChance(InjectorEffectType.InvertedControls) > 0)
            enabledEffects.Add($"Inverted Controls: {GetEffectChance(InjectorEffectType.InvertedControls)}%");
        if (GetEffectChance(InjectorEffectType.LowVision) > 0)
            enabledEffects.Add($"Low Vision: {GetEffectChance(InjectorEffectType.LowVision)}%");
        if (GetEffectChance(InjectorEffectType.Slowness) > 0)
            enabledEffects.Add($"Slowness: {GetEffectChance(InjectorEffectType.Slowness)}%");
        if (GetEffectChance(InjectorEffectType.VeryLowVision) > 0)
            enabledEffects.Add($"Very Low Vision: {GetEffectChance(InjectorEffectType.VeryLowVision)}%");
        if (GetEffectChance(InjectorEffectType.Confused) > 0)
            enabledEffects.Add($"Confused: {GetEffectChance(InjectorEffectType.Confused)}%");
        if (GetEffectChance(InjectorEffectType.NoVent) > 0)
            enabledEffects.Add($"No Vent: {GetEffectChance(InjectorEffectType.NoVent)}%");
        if (GetEffectChance(InjectorEffectType.NoUse) > 0)
            enabledEffects.Add($"No Use: {GetEffectChance(InjectorEffectType.NoUse)}%");
        if (GetEffectChance(InjectorEffectType.NoReport) > 0)
            enabledEffects.Add($"No Report: {GetEffectChance(InjectorEffectType.NoReport)}%");
        if (GetEffectChance(InjectorEffectType.Nausea) > 0)
            enabledEffects.Add($"Nausea: {GetEffectChance(InjectorEffectType.Nausea)}%");
        if (GetEffectChance(InjectorEffectType.Weakness) > 0)
            enabledEffects.Add($"Weakness: {GetEffectChance(InjectorEffectType.Weakness)}%");

        // Positive effects (only if enabled)
        if (PositiveEffectsEnabled)
        {
            if (GetEffectChance(InjectorEffectType.SpeedBoost) > 0)
                enabledEffects.Add($"Speed Boost: {GetEffectChance(InjectorEffectType.SpeedBoost)}%");
            if (GetEffectChance(InjectorEffectType.VisionBoost) > 0)
                enabledEffects.Add($"Vision Boost: {GetEffectChance(InjectorEffectType.VisionBoost)}%");
            if (GetEffectChance(InjectorEffectType.Regeneration) > 0)
                enabledEffects.Add($"Regeneration: {GetEffectChance(InjectorEffectType.Regeneration)}%");
        }

        if (enabledEffects.Count == 0)
        {
            return new[] { "Effect Chances: None configured" };
        }

        return new[] { $"Effect Chances: {string.Join(", ", enabledEffects)}" };
    }
}
