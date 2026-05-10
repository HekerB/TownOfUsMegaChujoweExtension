using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class PelicanOptions : AbstractOptionGroup<PelicanRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRolePelican", "Pelican");

    [ModdedNumberOption("ExtensionOptionPelicanSwallowCooldown", 5f, 120f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float SwallowCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionPelicanMaxSwallowed", 1f, 10f, 1f, MiraNumberSuffixes.None)]
    public float MaxSwallowed { get; set; } = 3f;

    [ModdedToggleOption("ExtensionOptionPelicanCanVent")]
    public bool CanVent { get; set; } = false;
}














