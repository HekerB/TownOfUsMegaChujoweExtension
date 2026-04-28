using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class PirateOptions : AbstractOptionGroup<PirateRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRolePirate", "Pirate");

    [ModdedNumberOption("ExtensionOptionPirateDuelCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float DuelCooldown { get; set; } = 25f;

    [ModdedToggleOption("ExtensionOptionPirateDrawCountsAsWin")]
    public bool DrawCountsAsWin { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionPirateCantDuelSamePersonTwice")]
    public bool CantDuelSamePersonTwiceInARow { get; set; } = false;

    public ModdedNumberOption DuelsToWin { get; } =
        new("ExtensionOptionPirateDuelsToWin", 2f, 1f, 5f, 1f, MiraNumberSuffixes.None, "0");

    [ModdedEnumOption("ExtensionOptionPirateWinMode", typeof(PirateWinMode),
        ["ExtensionOptionPirateWinModePirateWins",
         "ExtensionOptionPirateWinModePirateWinsWithOthers"])]
    public PirateWinMode WinMode { get; set; } = PirateWinMode.PirateWins;
}

public enum PirateWinMode
{
    PirateWins,
    PirateWinsWithOthers
}