using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options;

public sealed class ExtensionGameMechanicOptions : AbstractOptionGroup
{
    public override string GroupName => TouLocale.Get("ExtensionGameMechanicOptionsGroupName", "Game Mechanics");
    public override uint GroupPriority => 98;

    [ModdedToggleOption("ExtensionOptionGameMechanicBakerFaminePlagueAlliance")]
    public bool BakerFaminePlagueAlliance { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionGeneralBlockFirstRoundEmergency")]
    public bool BlockFirstRoundEmergency { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionGeneralPreventVampiresWithJackal")]
    public bool PreventVampiresWithJackal { get; set; } = true;
}
