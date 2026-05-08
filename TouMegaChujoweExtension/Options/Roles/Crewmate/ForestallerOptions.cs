using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class ForestallerOptions : AbstractOptionGroup<ForestallerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleForestaller", "Forestaller");

    [ModdedEnumOption("ExtensionOptionForestallerRevealTiming", typeof(ForestallerRevealTiming),
        ["ExtensionOptionForestallerRevealTimingEnumNextMeeting", "ExtensionOptionForestallerRevealTimingEnumInstant"])]
    public ForestallerRevealTiming RevealTiming { get; set; } = ForestallerRevealTiming.Instant;

    [ModdedNumberOption("ExtensionOptionForestallerExtraShortTasks", 0f, 5f, 1f, MiraNumberSuffixes.None, "0")]
    public float ExtraShortTasks { get; set; } = 1f;

    [ModdedNumberOption("ExtensionOptionForestallerExtraLongTasks", 0f, 5f, 1f, MiraNumberSuffixes.None, "0")]
    public float ExtraLongTasks { get; set; } = 1f;
}

public enum ForestallerRevealTiming
{
    NextMeeting,
    Instant
}
