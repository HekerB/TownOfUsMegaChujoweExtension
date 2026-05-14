using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public enum DoctorEffectDurationType
{
    AllRound,
    AllGame,
    SetTime
}

public enum DoctorEffectType
{
    SpeedBoost,
    VisionBoost,
    Cleanse,
    Shield,
    CanVent,
    Regeneration
}

public sealed class DoctorOptions : AbstractOptionGroup<DoctorRole>, IWikiOptionsSummaryProvider
{
    public override string GroupName => TouLocale.Get("ExtensionRoleDoctor", "Doctor");

    [ModdedNumberOption("ExtensionOptionDoctorInjectCooldown", 5f, 120f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float InjectCooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionDoctorEffectDelay", 0f, 30f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float EffectDelay { get; set; } = 3f;

    private static readonly string[] EffectDurationTypeValues =
    [
        "ExtensionOptionInjectorEffectDurationTypeEnumAllRound",
        "ExtensionOptionInjectorEffectDurationTypeEnumAllGame",
        "ExtensionOptionInjectorEffectDurationTypeEnumSetTime"
    ];

    public ModdedEnumOption<DoctorEffectDurationType> EffectDurationType { get; } =
        new("ExtensionOptionDoctorEffectDurationType", DoctorEffectDurationType.SetTime, EffectDurationTypeValues);

    public ModdedNumberOption EffectDurationOption { get; } = new("ExtensionOptionDoctorEffectDuration", 30f, 5f, 200f, 5f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<DoctorOptions>.Instance.EffectDurationType.Value == DoctorEffectDurationType.SetTime
    };

    public float EffectDuration => EffectDurationOption.Value;

    [ModdedNumberOption("ExtensionOptionDoctorInitialUses", 0f, 15f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float InitialUses { get; set; } = 3f;

    [ModdedToggleOption("ExtensionOptionDoctorCanGiveNegativeEffects")]
    public bool CanGiveNegativeEffects { get; set; } = false;

    public ModdedNumberOption ChanceNegativeSlownessOption { get; } =
        new("ExtensionOptionDoctorChanceNegativeSlowness", 10f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<DoctorOptions>.Instance.CanGiveNegativeEffects
        };

    public ModdedNumberOption ChanceNegativeLowVisionOption { get; } =
        new("ExtensionOptionDoctorChanceNegativeLowVision", 10f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<DoctorOptions>.Instance.CanGiveNegativeEffects
        };

    public ModdedNumberOption ChanceNegativeConfusedOption { get; } =
        new("ExtensionOptionDoctorChanceNegativeConfused", 10f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<DoctorOptions>.Instance.CanGiveNegativeEffects
        };

    public float ChanceNegativeSlowness => ChanceNegativeSlownessOption.Value;
    public float ChanceNegativeLowVision => ChanceNegativeLowVisionOption.Value;
    public float ChanceNegativeConfused => ChanceNegativeConfusedOption.Value;

    [ModdedToggleOption("ExtensionOptionDoctorSeesShield")]
    public bool DoctorSeesShield { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionDoctorTargetSeesShield")]
    public bool TargetSeesShield { get; set; } = true;

    public ModdedNumberOption ChanceSpeedBoostOption { get; } =
        new("ExtensionOptionDoctorChanceSpeedBoost", 30f, 0f, 100f, 10f, MiraNumberSuffixes.Percent);

    public ModdedNumberOption ChanceVisionBoostOption { get; } =
        new("ExtensionOptionDoctorChanceVisionBoost", 30f, 0f, 100f, 10f, MiraNumberSuffixes.Percent);

    public ModdedNumberOption ChanceCleanseOption { get; } =
        new("ExtensionOptionDoctorChanceCleanse", 40f, 0f, 100f, 10f, MiraNumberSuffixes.Percent);

    public ModdedNumberOption ChanceShieldOption { get; } =
        new("ExtensionOptionDoctorChanceShield", 20f, 0f, 100f, 10f, MiraNumberSuffixes.Percent);

    public ModdedNumberOption ChanceCanVentOption { get; } =
        new("ExtensionOptionDoctorChanceCanVent", 10f, 0f, 100f, 10f, MiraNumberSuffixes.Percent);

    public ModdedNumberOption ChanceRegenerationOption { get; } =
        new("ExtensionOptionDoctorChanceRegeneration", 10f, 0f, 100f, 10f, MiraNumberSuffixes.Percent);

    public float ChanceSpeedBoost => ChanceSpeedBoostOption.Value;
    public float ChanceVisionBoost => ChanceVisionBoostOption.Value;
    public float ChanceCleanse => ChanceCleanseOption.Value;
    public float ChanceShield => ChanceShieldOption.Value;
    public float ChanceCanVent => ChanceCanVentOption.Value;
    public float ChanceRegeneration => ChanceRegenerationOption.Value;

    public float GetEffectChance(DoctorEffectType effectType)
    {
        return effectType switch
        {
            DoctorEffectType.SpeedBoost => ChanceSpeedBoost,
            DoctorEffectType.VisionBoost => ChanceVisionBoost,
            DoctorEffectType.Cleanse => ChanceCleanse,
            DoctorEffectType.Shield => ChanceShield,
            DoctorEffectType.CanVent => ChanceCanVent,
            DoctorEffectType.Regeneration => ChanceRegeneration,
            _ => 0f
        };
    }

    public IReadOnlySet<StringNames> WikiHiddenOptionKeys =>
        new HashSet<StringNames>
        {
            ChanceSpeedBoostOption.StringName,
            ChanceVisionBoostOption.StringName,
            ChanceCleanseOption.StringName,
            ChanceShieldOption.StringName,
            ChanceCanVentOption.StringName,
            ChanceRegenerationOption.StringName,
            ChanceNegativeSlownessOption.StringName,
            ChanceNegativeLowVisionOption.StringName,
            ChanceNegativeConfusedOption.StringName
        };

    public IEnumerable<string> GetWikiOptionSummaryLines()
    {
        var enabledEffects = new List<string>();
        
        if (ChanceSpeedBoost > 0) enabledEffects.Add($"Speed Boost: {ChanceSpeedBoost}%");
        if (ChanceVisionBoost > 0) enabledEffects.Add($"Vision Boost: {ChanceVisionBoost}%");
        if (ChanceCleanse > 0) enabledEffects.Add($"Cleanse: {ChanceCleanse}%");
        if (ChanceShield > 0) enabledEffects.Add($"Shield: {ChanceShield}%");
        if (ChanceCanVent > 0) enabledEffects.Add($"Can Vent: {ChanceCanVent}%");
        if (ChanceRegeneration > 0) enabledEffects.Add($"Regeneration: {ChanceRegeneration}%");
        
        if (CanGiveNegativeEffects)
        {
            if (ChanceNegativeSlowness > 0) enabledEffects.Add($"Negative Slowness: {ChanceNegativeSlowness}%");
            if (ChanceNegativeLowVision > 0) enabledEffects.Add($"Negative Low Vision: {ChanceNegativeLowVision}%");
            if (ChanceNegativeConfused > 0) enabledEffects.Add($"Negative Confused: {ChanceNegativeConfused}%");
        }

        if (enabledEffects.Count == 0)
        {
            return new[] { "Injection Chances: None configured" };
        }

        return new[] { $"Injection Chances: {string.Join(", ", enabledEffects)}" };
    }
}
