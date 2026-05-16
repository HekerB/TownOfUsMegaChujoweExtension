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

public enum PortalUsageType
{
    CrewmateOnly,
    CrewmateAndNeutral,
    CrewmateAndImpostor,
    Everyone
}

public sealed class PortalmakerOptions : AbstractOptionGroup<PortalmakerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRolePortalmaker", "Portalmaker");

    [ModdedNumberOption("ExtensionOptionPortalmakerCooldown", 5f, 120f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float Cooldown { get; set; } = 15f;

    [ModdedNumberOption("ExtensionOptionPortalmakerTeleportCooldown", 5f, 120f, 5f, MiraNumberSuffixes.Seconds)]
    public float TeleportCooldown { get; set; } = 20f;

    [ModdedEnumOption("ExtensionOptionPortalmakerTeleportMode", typeof(TeleportMode))]
    public TeleportMode Mode { get; set; } = TeleportMode.Interaction;


    [ModdedNumberOption("ExtensionOptionPortalmakerPlacementDelay", 0f, 10f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float PlacementDelay { get; set; } = 5f;

    [ModdedNumberOption("ExtensionOptionPortalmakerUses", 0f, 10f, 2f, MiraNumberSuffixes.None, "0", true)]
    public float PortalUses { get; set; } = 2f;

    [ModdedEnumOption("ExtensionOptionPortalmakerWhoCanUse", typeof(PortalUsageType))]
    public PortalUsageType WhoCanUse { get; set; } = PortalUsageType.CrewmateOnly;
}
