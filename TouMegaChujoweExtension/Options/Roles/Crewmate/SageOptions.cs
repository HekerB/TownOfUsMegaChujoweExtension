using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class SageOptions : AbstractOptionGroup<SageRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleSage", "Sage");

    [ModdedNumberOption("ExtensionOptionSageMaxCompares", 0f, 15f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float MaxCompares { get; set; } = 5f;

    [ModdedNumberOption("ExtensionOptionSageCooldown", 5f, 120f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float SageCooldown { get; set; } = 20f;

    public ModdedToggleOption BenignShowFriendly { get; set; } = new("ExtensionOptionSageNeutralBenignFriendly", false);
    public ModdedToggleOption EvilShowFriendly { get; set; } = new("ExtensionOptionSageNeutralEvilFriendly", false);
    public ModdedToggleOption OutlierShowFriendly { get; set; } = new("ExtensionOptionSageNeutralOutlierFriendly", false);
}
