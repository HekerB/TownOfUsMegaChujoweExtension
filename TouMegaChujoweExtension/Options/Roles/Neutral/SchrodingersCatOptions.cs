using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class SchrodingersCatOptions : AbstractOptionGroup<SchrodingersCatRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleSchrodingersCat", "Schrodinger's Cat");

    [ModdedToggleOption("ExtensionOptionCatCanBeAdoptedByNK")]
    public bool CanBeAdoptedByNeutralKillers { get; set; } = true;
}
