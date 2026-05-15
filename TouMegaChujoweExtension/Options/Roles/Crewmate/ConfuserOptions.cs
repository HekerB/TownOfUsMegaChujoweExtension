using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class ConfuserOptions : AbstractOptionGroup<ConfuserRole>
{
    public override string GroupName => "Confuser";

    [ModdedNumberOption("ExtensionOptionConfuserDuration", 1f, 30f, 1f, MiraNumberSuffixes.Seconds, "0")]
    public float ConfuseDuration { get; set; } = 10f;

    [ModdedNumberOption("ExtensionOptionConfuserCooldown", 5f, 60f, 1f, MiraNumberSuffixes.Seconds, "0")]
    public float ConfuseCooldown { get; set; } = 30f;
}
