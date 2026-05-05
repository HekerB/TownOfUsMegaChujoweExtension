using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class RcXdOptions : AbstractOptionGroup<RcXdRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleRcXd", "RC-XD");

    [ModdedNumberOption("ExtensionOptionRcXdDeployCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float DeployCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionRcXdMaxDeploys", 0f, 10f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float MaxDeploys { get; set; } = 3f;

    [ModdedToggleOption("ExtensionOptionRcXdCanUseInFirstRound")]
    public bool CanUseInFirstRound { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionRcXdCanVent")]
    public bool CanVent { get; set; } = true;

    [ModdedNumberOption("ExtensionOptionRcXdDriveTime", 3f, 15f, 1f, MiraNumberSuffixes.Seconds)]
    public float DriveTime { get; set; } = 7f;

    [ModdedNumberOption("ExtensionOptionRcXdMaxSpeed", 0.5f, 8f, 0.5f, MiraNumberSuffixes.Multiplier)]
    public float CarSpeed { get; set; } = 2.5f;

    [ModdedNumberOption("ExtensionOptionRcXdAcceleration", 1f, 15f, 0.5f, MiraNumberSuffixes.Multiplier)]
    public float CarAcceleration { get; set; } = 6f;

    [ModdedNumberOption("ExtensionOptionRcXdDetonateRadius", 0.05f, 1f, 0.05f, MiraNumberSuffixes.Multiplier, "0.00")]
    public float DetonateRadius { get; set; } = 0.3f;

    [ModdedNumberOption("ExtensionOptionRcXdMaxKills", 1f, 15f, 1f, MiraNumberSuffixes.None, "0")]
    public float MaxKillsInDetonation { get; set; } = 5f;
}
