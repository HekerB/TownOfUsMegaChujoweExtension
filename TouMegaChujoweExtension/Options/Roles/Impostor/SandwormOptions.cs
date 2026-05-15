using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class SandwormOptions : AbstractOptionGroup<SandwormRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleSandworm", "Sandworm");

    [ModdedNumberOption("ExtensionOptionSandwormUndergroundSpeed", 0.5f, 3f, 0.1f, MiraNumberSuffixes.Multiplier, "0.0")]
    public float UndergroundSpeed { get; set; } = 1.5f;

    [ModdedNumberOption("ExtensionOptionSandwormDigDuration", 2f, 30f, 1f, MiraNumberSuffixes.Seconds, "0")]
    public float DigDuration { get; set; } = 10f;

    [ModdedNumberOption("ExtensionOptionSandwormEmergeKillRadius", 0.5f, 5f, 0.1f, MiraNumberSuffixes.None, "0.0")]
    public float EmergeKillRadius { get; set; } = 1.5f;

    [ModdedNumberOption("ExtensionOptionSandwormKillCooldown", 5f, 60f, 1f, MiraNumberSuffixes.Seconds, "0")]
    public float KillCooldown { get; set; } = 25f;
}
