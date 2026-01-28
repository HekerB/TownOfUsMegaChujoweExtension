using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMiraRolesExtension.Roles.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMiraRolesExtension.Options.Roles.Impostor;

public enum InjectorEffectDurationType
{
    AllRound,
    AllGame,
    SetTime
}

public sealed class InjectorOptions : AbstractOptionGroup<InjectorRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleInjector", "Injector");

    [ModdedNumberOption("ExtensionOptionInjectorInjectCooldown", 5f, 120f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float InjectCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionInjectorEffectDelay", 0f, 30f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float EffectDelay { get; set; } = 5f;

    private static readonly string[] EffectDurationTypeValues =
    [
        "ExtensionOptionInjectorEffectDurationTypeEnumAllRound",
        "ExtensionOptionInjectorEffectDurationTypeEnumAllGame",
        "ExtensionOptionInjectorEffectDurationTypeEnumSetTime"
    ];

    public ModdedEnumOption<InjectorEffectDurationType> EffectDurationType { get; } =
        new("ExtensionOptionInjectorEffectDurationType", InjectorEffectDurationType.SetTime, EffectDurationTypeValues);

    [ModdedNumberOption("ExtensionOptionInjectorEffectDuration", 5f, 200f, 5f, MiraNumberSuffixes.Seconds)]
    public float EffectDuration { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionInjectorInitialUses", 0, 15)]
    public float InitialUses { get; set; } = 3f;

    [ModdedNumberOption("ExtensionOptionInjectorUsesPerKill", 0, 5)]
    public float UsesPerKill { get; set; } = 1f;

    // Negative Effect Chances
    [ModdedNumberOption("ExtensionOptionInjectorChanceInvertedControls", 0f, 100f, 10f, MiraNumberSuffixes.Percent)]
    public float ChanceInvertedControls { get; set; } = 20f;

    [ModdedNumberOption("ExtensionOptionInjectorChanceLowVision", 0f, 100f, 10f, MiraNumberSuffixes.Percent)]
    public float ChanceLowVision { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionInjectorChanceSlowness", 0f, 100f, 10f, MiraNumberSuffixes.Percent)]
    public float ChanceSlowness { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionInjectorChanceVeryLowVision", 0f, 100f, 10f, MiraNumberSuffixes.Percent)]
    public float ChanceVeryLowVision { get; set; } = 10f;

    [ModdedNumberOption("ExtensionOptionInjectorChanceConfused", 0f, 100f, 10f, MiraNumberSuffixes.Percent)]
    public float ChanceConfused { get; set; } = 40f;

    [ModdedNumberOption("ExtensionOptionInjectorChanceNoVent", 0f, 100f, 10f, MiraNumberSuffixes.Percent)]
    public float ChanceNoVent { get; set; } = 20f;

    [ModdedNumberOption("ExtensionOptionInjectorChanceNoUse", 0f, 100f, 10f, MiraNumberSuffixes.Percent)]
    public float ChanceNoUse { get; set; } = 20f;

    [ModdedNumberOption("ExtensionOptionInjectorChanceNoReport", 0f, 100f, 10f, MiraNumberSuffixes.Percent)]
    public float ChanceNoReport { get; set; } = 20f;

    [ModdedNumberOption("ExtensionOptionInjectorChanceNausea", 0f, 100f, 10f, MiraNumberSuffixes.Percent)]
    public float ChanceNausea { get; set; } = 40f;

    [ModdedNumberOption("ExtensionOptionInjectorChanceWeakness", 0f, 100f, 10f, MiraNumberSuffixes.Percent)]
    public float ChanceWeakness { get; set; } = 20f;

    [ModdedToggleOption("ExtensionOptionInjectorPositiveEffectsEnabled")]
    public bool PositiveEffectsEnabled { get; set; } = true;

    // Positive Effect Chances
    public ModdedNumberOption ChanceSpeedBoost { get; } =
        new("ExtensionOptionInjectorChanceSpeedBoost", 10f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<InjectorOptions>.Instance.PositiveEffectsEnabled
        };

    public ModdedNumberOption ChanceVisionBoost { get; } =
        new("ExtensionOptionInjectorChanceVisionBoost", 10f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<InjectorOptions>.Instance.PositiveEffectsEnabled
        };

    public ModdedNumberOption ChanceRegeneration { get; } =
        new("ExtensionOptionInjectorChanceRegeneration", 10f, 0f, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<InjectorOptions>.Instance.PositiveEffectsEnabled
        };
}