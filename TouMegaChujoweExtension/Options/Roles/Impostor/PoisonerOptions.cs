using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class PoisonerOptions : AbstractOptionGroup<PoisonerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRolePoisoner", "Poisoner");

    [ModdedNumberOption("ExtensionOptionPoisonerPoisonCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float PoisonCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionPoisonerVineCooldown", 15f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float VineCooldown { get; set; } = 30f;

    [ModdedToggleOption("ExtensionOptionPoisonerCanVent")]
    public bool CanVent { get; set; } = true;

    [ModdedNumberOption("ExtensionOptionPoisonerPoisonDuration", 1f, 15f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float PoisonDuration { get; set; } = 5f;

    [ModdedNumberOption("ExtensionOptionPoisonerVineDuration", 1f, 10f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float VineDuration { get; set; } = 3f;

    [ModdedNumberOption("ExtensionOptionPoisonerVineRange", 1f, 15f, 0.5f, MiraNumberSuffixes.None)]
    public float VineRange { get; set; } = 5f;
}
