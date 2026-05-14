using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;

using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public enum TeleportMode
{
    Interaction,
    Automatic
}

public sealed class PortalmakerOptions : AbstractOptionGroup<PortalmakerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRolePortalmaker", "Portalmaker");

    [ModdedNumberOption("ExtensionOptionPortalmakerCooldown", 5f, 120f, 5f, MiraNumberSuffixes.Seconds)]
    public float Cooldown { get; set; } = 20f;

    [ModdedNumberOption("ExtensionOptionPortalmakerRadius", 0.1f, 2f, 0.1f, MiraNumberSuffixes.Multiplier, "0.00")]
    public float PortalRadius { get; set; } = 0.8f;

    [ModdedNumberOption("ExtensionOptionPortalmakerTeleportCooldown", 1f, 10f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float TeleportCooldown { get; set; } = 2f;

    [ModdedEnumOption("ExtensionOptionPortalmakerTeleportMode", typeof(TeleportMode))]
    public TeleportMode Mode { get; set; } = TeleportMode.Interaction;

    [ModdedNumberOption("ExtensionOptionPortalmakerDuration", 0f, 300f, 5f, MiraNumberSuffixes.Seconds)]
    public float PortalDuration { get; set; } = 0f; // 0 = Infinite

    [ModdedToggleOption("ExtensionOptionPortalmakerStayAfterMeeting")]
    public bool StayAfterMeeting { get; set; } = false;

    [ModdedNumberOption("ExtensionOptionPortalmakerPlacementDelay", 0f, 10f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float PlacementDelay { get; set; } = 2f;
}
