using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class BountyHunterOptions : AbstractOptionGroup<BountyHunterRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleBountyHunter", "Bounty Hunter");

    public ModdedNumberOption KillCooldown { get; } =
        new("ExtensionOptionBHKillCooldown", 30f, 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds);


    [ModdedToggleOption("ExtensionOptionBHCanVent")]
    public bool CanVent { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionBHShowArrow")]
    public bool ShowArrow { get; set; } = true;

    public ModdedNumberOption TargetsToKill { get; } =
        new("ExtensionOptionBHTargetsToKill", 3f, 1f, 5f, 1f, MiraNumberSuffixes.None);

    [ModdedEnumOption("ExtensionOptionBHWinMode", typeof(BountyHunterWinMode),
        ["ExtensionOptionBHWinModeEnumSoloWin",
         "ExtensionOptionBHWinModeEnumWinWithWinners"])]
    public BountyHunterWinMode WinMode { get; set; } = BountyHunterWinMode.SoloWin;
}

public enum BountyHunterWinMode
{
    SoloWin,
    WinWithWinners
}