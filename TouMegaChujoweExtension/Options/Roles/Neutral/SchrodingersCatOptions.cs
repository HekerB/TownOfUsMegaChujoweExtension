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

    [ModdedToggleOption("ExtensionOptionCatRevealRoles")]
    public bool RevealRolesToEachOther { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionCatChangeRoleOnOwnerDeath")]
    public bool ChangeRoleOnOwnerDeath { get; set; } = false;

    [ModdedEnumOption("ExtensionOptionCatOwnerDiedBecomes", typeof(CatOwnerDiedBecomesOption),
        ["ExtensionOptionCatBecomesAmnesiac",
         "ExtensionOptionCatBecomesSurvivor",
         "ExtensionOptionCatBecomesJester"])]
    public CatOwnerDiedBecomesOption OwnerDiedBecomes { get; set; } = CatOwnerDiedBecomesOption.Amnesiac;
}

public enum CatOwnerDiedBecomesOption
{
    Amnesiac,
    Survivor,
    Jester
}
