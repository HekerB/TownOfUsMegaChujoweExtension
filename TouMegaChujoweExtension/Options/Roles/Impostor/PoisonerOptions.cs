using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class PoisonerOptions : AbstractOptionGroup<PoisonerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRolePoisoner", "Poisoner");

    [ModdedToggleOption("ExtensionOptionPoisonerCanVent")]
    public bool CanVent { get; set; } = true;

    [ModdedNumberOption("ExtensionOptionPoisonerPoisonDuration", 1f, 15f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float PoisonDuration { get; set; } = 5f;

    [ModdedToggleOption("ExtensionOptionPoisonerVineEnabled")]
    public bool VineEnabled { get; set; } = true;

    public ModdedNumberOption VineDurationOption { get; } = new("ExtensionOptionPoisonerVineDuration", 3f, 1f, 10f, 0.5f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<PoisonerOptions>.Instance.VineEnabled
    };

    public float VineDuration => VineDurationOption.Value;

    public ModdedNumberOption VineSeekingDurationOption { get; } = new("ExtensionOptionPoisonerVineSeekingDuration", 10f, 1f, 20f, 0.5f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<PoisonerOptions>.Instance.VineEnabled
    };

    public float VineSeekingDuration => VineSeekingDurationOption.Value;

    public ModdedToggleOption CanCancelVineSeekingOption { get; } = new("ExtensionOptionPoisonerCanCancelVineSeeking", true)
    {
        Visible = () => OptionGroupSingleton<PoisonerOptions>.Instance.VineEnabled
    };

    public bool CanCancelVineSeeking => CanCancelVineSeekingOption.Value;
}
