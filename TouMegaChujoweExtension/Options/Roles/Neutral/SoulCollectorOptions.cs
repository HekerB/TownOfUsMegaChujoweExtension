using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class SoulCollectorOptions : AbstractOptionGroup<SoulCollectorRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleSoulCollector", "Soul Collector");

    [ModdedNumberOption("ExtensionOptionSoulCollectorInstantDeathChance", 0f, 100f, 5f, MiraNumberSuffixes.Percent)]
    public float InstantDeathChance { get; set; } = 0f;

    [ModdedNumberOption("ExtensionOptionSoulCollectorReapCooldown", 5f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float ReapCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionSoulCollectorSoulGoal", 1f, 10f, 1f, MiraNumberSuffixes.None)]
    public float SoulGoal { get; set; } = 5f;

    [ModdedNumberOption("ExtensionOptionSoulCollectorMaxMarks", 1f, 5f, 1f, MiraNumberSuffixes.None)]
    public float MaxMarks { get; set; } = 5f;

    [ModdedNumberOption("ExtensionOptionSoulCollectorRoundsToDie", 0f, 10f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float ReapDurationRounds { get; set; } = 2f;

    [ModdedNumberOption("ExtensionOptionSoulCollectorPassiveSoulsPerMeeting", 0f, 5f, 1f, MiraNumberSuffixes.None)]
    public float PassiveSoulsPerMeeting { get; set; } = 1f;

    [ModdedToggleOption("ExtensionOptionSoulCollectorAnnounceDeath")]
    public bool AnnounceDeath { get; set; } = true;

    [ModdedNumberOption("ExtensionOptionSoulCollectorDeathKillCooldown", 5f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float DeathKillCooldown { get; set; } = 20f;

    [ModdedToggleOption("ExtensionOptionSoulCollectorDeathCanVent")]
    public bool DeathCanVent { get; set; } = false;
}
