using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class BurrowerOptions : AbstractOptionGroup<BurrowerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleBurrower", "Burrower");

    [ModdedNumberOption("ExtensionOptionBurrowerUndergroundSpeed", 1.05f, 2.5f, 0.05f, MiraNumberSuffixes.Multiplier, "0.00")]
    public float UndergroundSpeed { get; set; } = 1.25f;

    [ModdedNumberOption("ExtensionOptionBurrowerDigDuration", 2f, 30f, 1f, MiraNumberSuffixes.Seconds, "0")]
    public float DigDuration { get; set; } = 10f;

    [ModdedNumberOption("ExtensionOptionBurrowerDigCooldown", 5f, 60f, 1f, MiraNumberSuffixes.Seconds, "0")]
    public float DigCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionBurrowerMaxBurrows", 0f, 10f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float MaxBurrows { get; set; } = 0f;

    [ModdedNumberOption("ExtensionOptionBurrowerEmergeVisionDuration", 0f, 30f, 1f, MiraNumberSuffixes.Seconds, "0")]
    public float EmergeVisionDuration { get; set; } = 5f;

    [ModdedToggleOption("ExtensionOptionBurrowerVentsStayAfterMeeting")]
    public bool VentsStayAfterMeeting { get; set; } = false;
}
