using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class InnocentOptions : AbstractOptionGroup<InnocentRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleInnocent", "Innocent");

    [ModdedNumberOption("ExtensionOptionInnocentTauntCooldown", 5f, 60f, 2.5f, MiraNumberSuffixes.Seconds, "0.0")]
    public float TauntCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionInnocentTauntDuration", 1f, 30f, 1f, MiraNumberSuffixes.Seconds, "0")]
    public float TauntDuration { get; set; } = 8f;

    [ModdedNumberOption("ExtensionOptionInnocentMaxTaunts", 0f, 5f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float MaxTaunts { get; set; } = 1f;

    public ModdedEnumOption TransformAfterTauntsInto { get; } =
        new("ExtensionOptionInnocentTransformAfterTauntsInto", (int)InnocentTransformRole.Amnesiac, typeof(InnocentTransformRole),
            ["TouRoleAmnesiac",
             "TouRoleSurvivor",
             "TouRoleMercenary",
             "TouRoleJester"]);

    [ModdedToggleOption("ExtensionOptionInnocentCanTauntFirstRound")]
    public bool CanTauntFirstRound { get; set; } = true;

    public ModdedEnumOption AfterWin { get; } =
        new("ExtensionOptionInnocentAfterWin", (int)InnocentAfterWin.EndGame, typeof(InnocentAfterWin),
            ["ExtensionOptionInnocentAfterWinEndGame",
             "ExtensionOptionInnocentAfterWinNothing",
             "ExtensionOptionInnocentAfterWinHaunt"]);
}

public enum InnocentTransformRole
{
    Amnesiac,
    Survivor,
    Mercenary,
    Jester
}

public enum InnocentAfterWin
{
    EndGame,
    Nothing,
    Haunt
}
