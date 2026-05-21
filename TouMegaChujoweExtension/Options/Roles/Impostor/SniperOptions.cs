using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;
using TouMegaChujoweExtension.Roles.Classic.Impostor;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class SniperOptions : AbstractOptionGroup<SniperRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleSniper", "Sniper");

    [ModdedNumberOption("ExtensionOptionSniperAimDuration", 3f, 20f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float AimDuration { get; set; } = 8f;

    [ModdedToggleOption("ExtensionOptionSniperCanVent")]
    public bool CanVent { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionSniperCanCancelAiming")]
    public bool CanCancelAiming { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionSniperAimZoomEnabled")]
    public bool AimZoomEnabled { get; set; } = false;

    public ModdedNumberOption ZoomDistanceOption { get; } = new("ExtensionOptionSniperZoomDistance", 6f, 4f, 30f, 0.5f, MiraNumberSuffixes.None)
    {
        Visible = () => OptionGroupSingleton<SniperOptions>.Instance.AimZoomEnabled
    };

    public float ZoomDistance => ZoomDistanceOption.Value;
}
