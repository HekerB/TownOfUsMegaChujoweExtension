using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public class InnocentOptions : AbstractOptionGroup<InnocentRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleInnocent", "Innocent");

    [ModdedNumberOption("ExtensionOptionInnocentTauntCooldown", 5f, 60f, 2.5f, MiraNumberSuffixes.Seconds, "0.0")]
    public float TauntCooldown { get; set; } = 25f;

    [ModdedToggleOption("ExtensionOptionInnocentCanTauntFirstRound")]
    public bool CanTauntFirstRound { get; set; } = false;
    
    [ModdedToggleOption("ExtensionOptionInnocentForceReport")]
    public bool ForceReport { get; set; } = false;
}
