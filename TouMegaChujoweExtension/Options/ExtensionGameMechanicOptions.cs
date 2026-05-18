using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options;

public sealed class ExtensionGameMechanicOptions : AbstractOptionGroup
{
    public override string GroupName => TouLocale.Get("ExtensionGameMechanicOptionsGroupName", "Game Mechanics");
    public override uint GroupPriority => 100;

    [ModdedToggleOption("ExtensionOptionGeneralBlockFirstRoundEmergency")]
    public bool BlockFirstRoundEmergency { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionGeneralPreventVampiresWithJackal")]
    public bool PreventVampiresWithJackal { get; set; } = false;
}
