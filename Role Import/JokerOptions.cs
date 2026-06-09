using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Neutral;
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
    public float KillsToWin { get; set; } = 4f;

    [ModdedNumberOption("ExtensionOptionJokerCloneCooldown", 5f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float CloneCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionJokerCloneSpeed", 0.5f, 3f, 0.05f, MiraNumberSuffixes.Multiplier, "0.00")]
    public float CloneSpeed { get; set; } = 1.25f;

    [ModdedEnumOption("ExtensionOptionJokerWinMode", typeof(JokerWinOptions),
        ["JokerWinOptionsSoloWin",
         "JokerWinOptionsWinWithWinners"])]
    public JokerWinOptions WinMode { get; set; } = JokerWinOptions.SoloWin;
}