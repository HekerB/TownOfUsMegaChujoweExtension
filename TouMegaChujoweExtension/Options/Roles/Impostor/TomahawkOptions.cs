using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class TomahawkOptions : AbstractOptionGroup<TomahawkRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleTomahawk", "Tomahawk");

    [ModdedNumberOption("ExtensionOptionTomahawkSpeed", 1f, 15f, 0.5f, MiraNumberSuffixes.Multiplier)]
    public float Speed { get; set; } = 5f;

    [ModdedNumberOption("ExtensionOptionTomahawkKillRadius", 0.1f, 2f, 0.1f, MiraNumberSuffixes.None)]
    public float KillRadius { get; set; } = 0.5f;

    [ModdedNumberOption("ExtensionOptionTomahawkCooldown", 10f, 60f, 1f, MiraNumberSuffixes.Seconds)]
    public float Cooldown { get; set; } = 30f;

    [ModdedToggleOption("ExtensionOptionTomahawkCanVent")]
    public bool CanVent { get; set; } = true;
}
