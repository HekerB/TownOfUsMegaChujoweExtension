using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class RcXdOptions : AbstractOptionGroup<RcXdRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleRcXd", "RC-XD");

    [ModdedNumberOption("ExtensionOptionRcXdMaxDeploys", 1f, 15f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float MaxDeploys { get; set; } = 2f;

    [ModdedNumberOption("ExtensionOptionRcXdDriveTime", 3f, 20f, 1f, MiraNumberSuffixes.Seconds)]
    public float DriveTime { get; set; } = 10f;

    [ModdedNumberOption("ExtensionOptionRcXdDetonateRadius", 0.1f, 3f, 0.1f, MiraNumberSuffixes.Multiplier, "0.00")]
    public float DetonateRadius { get; set; } = 0.5f;

    [ModdedNumberOption("ExtensionOptionRcXdMaxKills", 1f, 15f, 1f, MiraNumberSuffixes.None, "0")]
    public float MaxKillsInDetonation { get; set; } = 5f;

    [ModdedNumberOption("ExtensionOptionRcXdMaxSpeed", 0.5f, 6f, 0.25f, MiraNumberSuffixes.Multiplier)]
    public float CarSpeed { get; set; } = 1.5f;

    [ModdedToggleOption("ExtensionOptionRcXdAllowEarlyDetonation")]
    public bool AllowEarlyDetonation { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionRcXdCanUseInFirstRound")]
    public bool CanUseInFirstRound { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionRcXdCanVent")]
    public bool CanVent { get; set; } = true;

}















