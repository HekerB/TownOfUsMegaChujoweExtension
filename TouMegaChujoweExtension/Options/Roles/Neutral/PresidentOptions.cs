using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class PresidentOptions : AbstractOptionGroup<PresidentRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRolePresident", "President");

    [ModdedNumberOption("ExtensionOptionPresidentStartingVoteBank", 0f, 10f, 1f, MiraNumberSuffixes.None)]
    public float StartingVoteBank { get; set; } = 2f;

    [ModdedNumberOption("ExtensionOptionPresidentMaxVoteBank", 1f, 20f, 1f, MiraNumberSuffixes.None)]
    public float MaxVoteBank { get; set; } = 10f;

    [ModdedNumberOption("ExtensionOptionPresidentAbstainBonus", 1f, 5f, 1f, MiraNumberSuffixes.None)]
    public float AbstainBonus { get; set; } = 1f;
}