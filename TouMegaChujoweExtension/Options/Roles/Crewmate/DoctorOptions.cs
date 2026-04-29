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

    [ModdedNumberOption("ExtensionOptionDoctorHealCooldown", 5f, 120f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float HealCooldown { get; set; } = 30f;

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

    [ModdedNumberOption("ExtensionOptionDoctorEffectDuration", 5f, 200f, 5f, MiraNumberSuffixes.Seconds)]
    public float EffectDuration { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionDoctorInitialUses", 0, 15)]
    public float InitialUses { get; set; } = 3f;

    [ModdedToggleOption("ExtensionOptionDoctorCanGiveNegativeEffects")]
    public bool CanGiveNegativeEffects { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionDoctorSeesShieldFlash")]
    public bool DoctorSeesShieldFlash { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionDoctorTargetSeesShield")]
    public bool TargetSeesShield { get; set; } = true;

    // Effect selection and chance configuration
    private static readonly string[] EffectTypeValues =
    [
        "ExtensionOptionInjectorEffectTypeEnumSpeedBoost",
        "ExtensionOptionInjectorEffectTypeEnumVisionBoost",
        "ExtensionOptionDoctorEffectTypeEnumCleanse",
        "ExtensionOptionDoctorEffectTypeEnumShield",
        "ExtensionOptionDoctorEffectTypeEnumCanVent",
        "ExtensionOptionDoctorEffectTypeEnumRegeneration"
    ];

    public ModdedEnumOption<DoctorEffectType> SelectedEffectType { get; } =
        new("ExtensionOptionDoctorSelectedEffectType", DoctorEffectType.SpeedBoost, EffectTypeValues);

    public ModdedNumberOption ChanceSpeedBoostOption { get; } =
        new("ExtensionOptionDoctorChanceSpeedBoost", 30f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<DoctorOptions>.Instance.SelectedEffectType.Value == DoctorEffectType.SpeedBoost
        };

    public ModdedNumberOption ChanceVisionBoostOption { get; } =
        new("ExtensionOptionDoctorChanceVisionBoost", 30f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<DoctorOptions>.Instance.SelectedEffectType.Value == DoctorEffectType.VisionBoost
        };

    public ModdedNumberOption ChanceCleanseOption { get; } =
        new("ExtensionOptionDoctorChanceCleanse", 40f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<DoctorOptions>.Instance.SelectedEffectType.Value == DoctorEffectType.Cleanse
        };

    public ModdedNumberOption ChanceShieldOption { get; } =
        new("ExtensionOptionDoctorChanceShield", 20f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<DoctorOptions>.Instance.SelectedEffectType.Value == DoctorEffectType.Shield
        };

    public ModdedNumberOption ChanceCanVentOption { get; } =
        new("ExtensionOptionDoctorChanceCanVent", 10f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<DoctorOptions>.Instance.SelectedEffectType.Value == DoctorEffectType.CanVent
        };

    public ModdedNumberOption ChanceRegenerationOption { get; } =
        new("ExtensionOptionDoctorChanceRegeneration", 10f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<DoctorOptions>.Instance.SelectedEffectType.Value == DoctorEffectType.Regeneration
        };

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
            SelectedEffectType.StringName,
            ChanceSpeedBoostOption.StringName,
            ChanceVisionBoostOption.StringName,
            ChanceCleanseOption.StringName,
            ChanceShieldOption.StringName,
            ChanceCanVentOption.StringName,
            ChanceRegenerationOption.StringName
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

        if (enabledEffects.Count == 0)
        {
            return new[] { "Heal Chances: None configured" };
        }

        return new[] { $"Heal Chances: {string.Join(", ", enabledEffects)}" };
    }
}
