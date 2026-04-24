using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class DoppelgangerOptions : AbstractOptionGroup<DoppelgangerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleDoppelganger", "Doppelganger");

    [ModdedNumberOption("ExtensionOptionDoppelgangerKillCooldown", 5f, 120f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float KillCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionDoppelgangerMaxSteals", 0f, 15f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float MaxSteals { get; set; } = 0f;

    [ModdedToggleOption("ExtensionOptionDoppelgangerStealsPerRound")]
    public bool StealsPerRound { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionDoppelgangerCanVent")]
    public bool CanVent { get; set; } = true;
}