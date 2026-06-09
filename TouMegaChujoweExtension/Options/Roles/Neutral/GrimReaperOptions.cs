using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class GrimReaperOptions : AbstractOptionGroup<GrimReaperRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleGrimReaper", "Grim Reaper");

    [ModdedNumberOption("ExtensionOptionGrimReaperSoulsToWin", 1f, 10f, 1f, MiraNumberSuffixes.None)]
    public float SoulsToWin { get; set; } = 3f;

    [ModdedToggleOption("ExtensionOptionGrimReaperSoulsDisappearOnMeeting")]
    public bool SoulsDisappearOnMeeting { get; set; } = true;

    [ModdedNumberOption("ExtensionOptionGrimReaperSoulDurationRounds", 0f, 15f, 1f, zeroInfinity: true)]
    public float SoulDurationRounds { get; set; } = 1f;

    [ModdedEnumOption("ExtensionOptionGrimReaperWinMode", typeof(GrimReaperWinMode),
        ["ExtensionOptionGrimReaperWinModeSolo",
         "ExtensionOptionGrimReaperWinModeWithWinners"])]
    public GrimReaperWinMode WinMode { get; set; } = GrimReaperWinMode.GrimReaperWins;

    [ModdedNumberOption("ExtensionOptionGrimReaperReapRange", 0.5f, 4f, 0.25f, MiraNumberSuffixes.None)]
    public float ReapRange { get; set; } = 1.5f;

    [ModdedNumberOption("ExtensionOptionGrimReaperReapCooldown", 0f, 30f, 1f, MiraNumberSuffixes.Seconds)]
    public float ReapCooldown { get; set; } = 2f;

    [ModdedToggleOption("ExtensionOptionGrimReaperCanVent")]
    public bool CanVent { get; set; } = false;
}

public enum GrimReaperWinMode
{
    GrimReaperWins,
    GrimReaperWinsWithOthers
}
