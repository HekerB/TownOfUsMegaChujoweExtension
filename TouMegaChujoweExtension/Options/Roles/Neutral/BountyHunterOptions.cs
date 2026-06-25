using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class BountyHunterOptions : AbstractOptionGroup<BountyHunterRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleBountyHunter", "Bounty Hunter");

    public ModdedNumberOption KillCooldown { get; } =
        new("ExtensionOptionBHKillCooldown", 30f, 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds);

    [ModdedToggleOption("ExtensionOptionBHHasImpostorVision")]
    public bool HasImpostorVision { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionBHCanKillInRoundOne")]
    public bool CanKillInRoundOne { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionBHShowArrow")]
    public bool ShowArrow { get; set; } = true;

    public ModdedEnumOption<BountyHunterArrowRange> ArrowRange { get; } =
        new("ExtensionOptionBHArrowRange", BountyHunterArrowRange.Short,
            ["ExtensionOptionBHArrowRangeShort",
             "ExtensionOptionBHArrowRangeMedium",
             "ExtensionOptionBHArrowRangeLong",
             "ExtensionOptionBHArrowRangeInfinite"])
        {
            Visible = () => OptionGroupSingleton<BountyHunterOptions>.Instance.ShowArrow
        };

    public ModdedNumberOption TargetsToKill { get; } =
        new("ExtensionOptionBHTargetsToKill", 3f, 1f, 5f, 1f, MiraNumberSuffixes.None);

    [ModdedEnumOption("ExtensionOptionBHWinMode", typeof(BountyHunterWinMode),
        ["ExtensionOptionBHWinModeEnumSoloWin",
         "ExtensionOptionBHWinModeEnumLeavesInVictory"])]
    public BountyHunterWinMode WinMode { get; set; } = BountyHunterWinMode.SoloWin;
}

public enum BountyHunterWinMode
{
    SoloWin,
    LeavesInVictory
}

public enum BountyHunterArrowRange
{
    Short,
    Medium,
    Long,
    Infinite
}
