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

    [ModdedNumberOption("ExtensionOptionGaslighterShieldCooldown", 5f, 60f, 1f, MiraNumberSuffixes.Seconds, "0")]
    public float ShieldCooldown { get; set; } = 30f;

    [ModdedEnumOption("ExtensionOptionGaslighterWinCondition", typeof(GaslighterWinMode))]
    public GaslighterWinMode WinCondition { get; set; } = GaslighterWinMode.CrewmateLose;
}

public enum GaslighterWinMode
{
    CrewmateLose,
    LastStanding,
    AliveAtEnd
}
