using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class GardenerOptions : AbstractOptionGroup<GardenerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleGardener", "Gardener");

    [ModdedNumberOption("ExtensionOptionGardenerRadius", 0.1f, 5f, 0.1f, MiraNumberSuffixes.Multiplier, "0.00")]
    public float Radius { get; set; } = 0.3f;

    [ModdedNumberOption("ExtensionOptionGardenerCooldown", 5f, 120f, 5f, MiraNumberSuffixes.Seconds)]
    public float Cooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionGardenerDuration", 5f, 120f, 5f, MiraNumberSuffixes.Seconds)]
    public float Duration { get; set; } = 15f;

    [ModdedToggleOption("ExtensionOptionGardenerCanKillInGarden")]
    public bool CanKillInGarden { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionGardenerFeedback")]
    public bool Feedback { get; set; } = true;
}

