using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class InverterOptions : AbstractOptionGroup<InverterRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleInverter", "Inverter");

    [ModdedNumberOption("ExtensionOptionInverterDisorientCooldown", 5f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float DisorientCooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionInverterDisorientDuration", 3f, 30f, 1f, MiraNumberSuffixes.Seconds)]
    public float DisorientDuration { get; set; } = 10f;

    [ModdedNumberOption("ExtensionOptionInverterMaxDisorients", 0f, 15f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float MaxDisorients { get; set; } = 0f;

    [ModdedToggleOption("ExtensionOptionInverterDisorientSamePersonTwice")]
    public bool DisorientSamePersonTwice { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionInverterApplyDrunk")]
    public bool ApplyDrunk { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionInverterApplyHerbalistConfuse")]
    public bool ApplyHerbalistConfuse { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionInverterMoveWhileMenu")]
    public bool MoveWhileMenu { get; set; } = true;
}
