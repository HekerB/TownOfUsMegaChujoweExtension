using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public enum BerserkerVentAccess
{
    Neither,
    Berserker,
    War,
    Both
}

public sealed class BerserkerOptions : AbstractOptionGroup<BerserkerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleBerserker", "Berserker");

    [ModdedNumberOption("ExtensionOptionBerserkerInstantWarChance", 0f, 100f, 5f, MiraNumberSuffixes.Percent)]
    public float InstantWarChance { get; set; } = 0f;

    [ModdedNumberOption("ExtensionOptionBerserkerInitialKillCooldown", 5f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float InitialKillCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionBerserkerKillCooldownReduction", 0f, 15f, 1f, MiraNumberSuffixes.Seconds)]
    public float KillCooldownReduction { get; set; } = 5f;

    [ModdedEnumOption("ExtensionOptionBerserkerVentAccess", typeof(BerserkerVentAccess),
        ["ExtensionOptionBerserkerVentAccessEnumNeither",
         "ExtensionOptionBerserkerVentAccessEnumBerserker",
         "ExtensionOptionBerserkerVentAccessEnumWar",
         "ExtensionOptionBerserkerVentAccessEnumBoth"])]
    public BerserkerVentAccess VentAccess { get; set; } = BerserkerVentAccess.Both;

    [ModdedNumberOption("ExtensionOptionBerserkerKillsNeededToTransform", 1f, 10f, 1f)]
    public float KillsNeededToTransform { get; set; } = 4f;

    [ModdedToggleOption("ExtensionOptionBerserkerAnnounceWar")]
    public bool AnnounceWarTransformation { get; set; } = true;

    [ModdedNumberOption("ExtensionOptionWarKillingSpreeDuration", 0f, 10f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float WarKillingSpreeDuration { get; set; } = 1f;
}
