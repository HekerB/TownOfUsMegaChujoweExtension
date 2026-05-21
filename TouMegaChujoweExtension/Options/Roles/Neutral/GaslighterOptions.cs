using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class GaslighterOptions : AbstractOptionGroup<GaslighterRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleGaslighter", "Gaslighter");

    [ModdedNumberOption("ExtensionOptionGaslighterKillCooldown", 5f, 60f, 1f, MiraNumberSuffixes.Seconds, "0")]
    public float KillCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionGaslighterKnightCooldown", 5f, 60f, 1f, MiraNumberSuffixes.Seconds, "0")]
    public float KnightCooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionGaslighterCurseCooldown", 5f, 60f, 1f, MiraNumberSuffixes.Seconds, "0")]
    public float CurseCooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionGaslighterCurseCastingDuration", 0.5f, 5f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float CurseCastingDuration { get; set; } = 2f;

    [ModdedNumberOption("ExtensionOptionGaslighterShieldCooldown", 5f, 60f, 1f, MiraNumberSuffixes.Seconds, "0")]
    public float ShieldCooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionGaslighterMaxKnights", 0f, 15f, 1f, zeroInfinity: true)]
    public float MaxKnights { get; set; } = 3f;

    [ModdedToggleOption("ExtensionOptionGaslighterCanVent")]
    public bool CanVent { get; set; } = false;

    [ModdedEnumOption("ExtensionOptionGaslighterWinCondition", typeof(GaslighterWinMode))]
    public GaslighterWinMode WinCondition { get; set; } = GaslighterWinMode.CrewmateLose;
}

public enum GaslighterWinMode
{
    CrewmateLose,
    LastStanding,
    AliveAtEnd
}
