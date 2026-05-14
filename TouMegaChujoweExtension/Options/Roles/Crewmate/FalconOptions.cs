using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class FalconOptions : AbstractOptionGroup<FalconRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleFalcon", "Falcon");

    [ModdedNumberOption("ExtensionOptionFalconZoomCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float ZoomCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionFalconZoomDuration", 3f, 30f, 1f, MiraNumberSuffixes.Seconds)]
    public float ZoomDuration { get; set; } = 10f;

    [ModdedNumberOption("ExtensionOptionFalconZoomDistance", 4f, 30f, 0.5f, MiraNumberSuffixes.None)]
    public float ZoomDistance { get; set; } = 6f;

    [ModdedNumberOption("ExtensionOptionFalconMaxUses", 0f, 10f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float MaxUses { get; set; } = 0f;
}














