using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options;

public sealed class RoundStartOptions : AbstractOptionGroup
{
    public override string GroupName => TouLocale.Get("ExtensionRoundStartOptionsGroupName", "Round Start Options");
    public override uint GroupPriority => 100;

    [ModdedToggleOption("ExtensionOptionGeneralBlockFirstRoundEmergency")]
    public bool BlockFirstRoundEmergency { get; set; } = false;
}
