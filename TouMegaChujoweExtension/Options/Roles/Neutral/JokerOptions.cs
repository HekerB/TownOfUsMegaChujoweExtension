using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public enum JokerWinOptions
{
    SoloWin,
    WinWithWinners
}

public sealed class JokerOptions : AbstractOptionGroup<JokerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleJoker", "Joker");

    [ModdedNumberOption("ExtensionOptionJokerKillsToWin", 1f, 10f, 1f, MiraNumberSuffixes.None)]
    public float KillsToWin { get; set; } = 3f;

    [ModdedNumberOption("ExtensionOptionJokerCloneCooldown", 5f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float CloneCooldown { get; set; } = 15f;

    [ModdedNumberOption("ExtensionOptionJokerMaxClones", 1f, 10f, 1f, MiraNumberSuffixes.None)]
    public float MaxClones { get; set; } = 4f;

    [ModdedToggleOption("ExtensionOptionJokerResetClonesEachMeeting")]
    public bool ResetClonesEachMeeting { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionJokerMoveWithTablet")]
    public bool MoveWithTablet { get; set; } = false;

    [ModdedEnumOption("ExtensionOptionJokerWinMode", typeof(JokerWinOptions),
        ["JokerWinOptionsSoloWin",
         "JokerWinOptionsWinWithWinners"])]
    public JokerWinOptions WinMode { get; set; } = JokerWinOptions.SoloWin;
}
