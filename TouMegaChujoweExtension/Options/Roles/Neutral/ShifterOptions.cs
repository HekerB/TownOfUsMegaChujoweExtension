using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class ShifterOptions : AbstractOptionGroup<ShifterRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleShifter", "Shifter");

    [ModdedNumberOption("ExtensionOptionShifterCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float ShiftCooldown { get; set; } = 25f;

    [ModdedToggleOption("ExtensionOptionShifterStealModifiers")]
    public bool StealModifiers { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionShifterCanShiftNeutralBenign")]
    public bool CanShiftNeutralBenign { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionShifterWinsWithCrew")]
    public bool WinsWithCrew { get; set; } = true;

    [ModdedEnumOption("ExtensionOptionShifterShiftedBecomes", typeof(ShiftedBecomesOption),
        ["ExtensionOptionShifterBecomesAmnesiac",
         "ExtensionOptionShifterBecomesJester",
         "ExtensionOptionShifterBecomesSurvivor",
         "ExtensionOptionShifterBecomesMercenary",
         "ExtensionOptionShifterBecomesCrewmate",
         "ExtensionOptionShifterBecomesShifter"])]
    public ShiftedBecomesOption ShiftedBecomes { get; set; } = ShiftedBecomesOption.Crewmate;
}

public enum ShiftedBecomesOption
{
    Amnesiac,
    Jester,
    Survivor,
    Mercenary,
    Crewmate,
    Shifter
}