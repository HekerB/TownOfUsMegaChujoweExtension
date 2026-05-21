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

    [ModdedNumberOption("ExtensionOptionSniperCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float Cooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionSniperMaxRange", 5f, 25f, 1f, MiraNumberSuffixes.None)]
    public float MaxRange { get; set; } = 15f;

    [ModdedToggleOption("ExtensionOptionSniperCanVent")]
    public bool CanVent { get; set; } = true;

    [ModdedNumberOption("Scope Vision Zoom", 1f, 5f, 0.5f, MiraNumberSuffixes.None)]
    public float VisionZoom { get; set; } = 1.0f;

    [ModdedNumberOption("Shoot Time Window", 2f, 15f, 1f, MiraNumberSuffixes.Seconds)]
    public float ShootWindow { get; set; } = 5.0f;
}
