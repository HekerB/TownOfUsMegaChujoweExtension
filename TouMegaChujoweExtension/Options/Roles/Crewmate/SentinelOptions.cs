using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class SentinelOptions : AbstractOptionGroup<SentinelRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleSentinel", "Sentinel");

    [ModdedNumberOption("ExtensionOptionSentinelRadius", 0.05f, 1f, 0.05f, MiraNumberSuffixes.Multiplier, "0.00")]
    public float Radius { get; set; } = 0.3f;

    [ModdedNumberOption("ExtensionOptionSentinelCooldown", 5f, 120f, 5f, MiraNumberSuffixes.Seconds)]
    public float Cooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionSentinelDuration", 1f, 60f, 1f, MiraNumberSuffixes.Seconds)]
    public float Duration { get; set; } = 10f;

    [ModdedToggleOption("ExtensionOptionSentinelNotifyEvil")]
    public bool NotifyEvil { get; set; } = true;
}
