using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class InverterOptions : AbstractOptionGroup<InverterRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleInverter", "Inverter");

    [ModdedNumberOption("ExtensionOptionInverterDisorientCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds, "0.0")]
    public float DisorientCooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionInverterDisorientDuration", 3f, 30f, 1f, MiraNumberSuffixes.Seconds)]
    public float DisorientDuration { get; set; } = 10f;

    [ModdedToggleOption("ExtensionOptionInverterCanVent")]
    public bool CanVent { get; set; } = true;
}
