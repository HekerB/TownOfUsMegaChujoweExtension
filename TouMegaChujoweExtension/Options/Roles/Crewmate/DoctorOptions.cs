using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
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
    Regeneration,
    Cleanse,
    Shield,
    XRay
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

    // Effect selection and chance configuration
    private static readonly string[] EffectTypeValues =
    [
        "ExtensionOptionInjectorEffectTypeEnumSpeedBoost",
        "ExtensionOptionInjectorEffectTypeEnumVisionBoost",
        "ExtensionOptionInjectorEffectTypeEnumRegeneration",
        "ExtensionOptionDoctorEffectTypeEnumCleanse",
        "ExtensionOptionDoctorEffectTypeEnumShield",
        "ExtensionOptionDoctorEffectTypeEnumXRay"
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

    public ModdedNumberOption ChanceRegenerationOption { get; } =
        new("ExtensionOptionDoctorChanceRegeneration", 30f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<DoctorOptions>.Instance.SelectedEffectType.Value == DoctorEffectType.Regeneration
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

    public ModdedNumberOption ChanceXRayOption { get; } =
        new("ExtensionOptionDoctorChanceXRay", 10f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<DoctorOptions>.Instance.SelectedEffectType.Value == DoctorEffectType.XRay
        };

    public float ChanceSpeedBoost => ChanceSpeedBoostOption.Value;
    public float ChanceVisionBoost => ChanceVisionBoostOption.Value;
    public float ChanceRegeneration => ChanceRegenerationOption.Value;
    public float ChanceCleanse => ChanceCleanseOption.Value;
    public float ChanceShield => ChanceShieldOption.Value;
    public float ChanceXRay => ChanceXRayOption.Value;

    public float GetEffectChance(DoctorEffectType effectType)
    {
        return effectType switch
        {
            DoctorEffectType.SpeedBoost => ChanceSpeedBoost,
            DoctorEffectType.VisionBoost => ChanceVisionBoost,
            DoctorEffectType.Regeneration => ChanceRegeneration,
            DoctorEffectType.Cleanse => ChanceCleanse,
            DoctorEffectType.Shield => ChanceShield,
            DoctorEffectType.XRay => ChanceXRay,
            _ => 0f
        };
    }

    public IReadOnlySet<StringNames> WikiHiddenOptionKeys =>
        new HashSet<StringNames>
        {
            SelectedEffectType.StringName,
            ChanceSpeedBoostOption.StringName,
            ChanceVisionBoostOption.StringName,
            ChanceRegenerationOption.StringName,
            ChanceCleanseOption.StringName,
            ChanceShieldOption.StringName,
            ChanceXRayOption.StringName
        };

    public IEnumerable<string> GetWikiOptionSummaryLines()
    {
        var enabledEffects = new List<string>();
        
        if (ChanceSpeedBoost > 0) enabledEffects.Add($"Speed Boost: {ChanceSpeedBoost}%");
        if (ChanceVisionBoost > 0) enabledEffects.Add($"Vision Boost: {ChanceVisionBoost}%");
        if (ChanceRegeneration > 0) enabledEffects.Add($"Regeneration: {ChanceRegeneration}%");
        if (ChanceCleanse > 0) enabledEffects.Add($"Cleanse: {ChanceCleanse}%");
        if (ChanceShield > 0) enabledEffects.Add($"Shield: {ChanceShield}%");
        if (ChanceXRay > 0) enabledEffects.Add($"X-Ray: {ChanceXRay}%");

        if (enabledEffects.Count == 0)
        {
            return new[] { "Heal Chances: None configured" };
        }

        return new[] { $"Heal Chances: {string.Join(", ", enabledEffects)}" };
    }
}
