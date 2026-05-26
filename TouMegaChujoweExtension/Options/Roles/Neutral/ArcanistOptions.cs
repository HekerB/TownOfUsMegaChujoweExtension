using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using AmongUs.GameOptions;
using System.Collections.Generic;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class ArcanistOptions : AbstractOptionGroup<ArcanistRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleArcanist", "Arcanist");

    [ModdedNumberOption("ExtensionOptionArcanistCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float Cooldown { get; set; } = 13f;

    [ModdedNumberOption("ExtensionOptionArcanistDeckSize", 1f, 22f, 1f)]
    public float DeckSize { get; set; } = 10f;

    [ModdedToggleOption("ExtensionOptionArcanistAllowDuplicateRoles")]
    public bool AllowDuplicateRoles { get; set; } = false;

    // Group 1: Cyan/Blue (Neutral aligned / Role changes)
    [ModdedNumberOption("ExtensionOptionArcanistWeight00", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightFool { get; set; } = 4f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight02", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightHighPriestess { get; set; } = 5f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight05", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightHierophant { get; set; } = 4f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight10", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightWheel { get; set; } = 4f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight12", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightHangedMan { get; set; } = 4f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight20", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightJudgement { get; set; } = 4f;

    // Group 2: Bright Green (Buffs - Speed/Vision)
    [ModdedNumberOption("ExtensionOptionArcanistWeight07", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightChariot { get; set; } = 6f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight19", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightSun { get; set; } = 6f;

    // Group 3: Light Green (Buffs)
    [ModdedNumberOption("ExtensionOptionArcanistWeight03", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightEmpress { get; set; } = 4f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight08", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightStrength { get; set; } = 4f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight09", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightHermit { get; set; } = 3f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight17", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightStar { get; set; } = 3f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight21", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightWorld { get; set; } = 3f;

    // Group 4: Red (Debuffs / Impostor aligned)
    [ModdedNumberOption("ExtensionOptionArcanistWeight01", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightMagician { get; set; } = 4f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight18", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightMoon { get; set; } = 4f;

    // Group 5: Dark Red (Debuffs / Risk)
    [ModdedNumberOption("ExtensionOptionArcanistWeight04", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightEmperor { get; set; } = 4f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight11", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightJustice { get; set; } = 5f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight13", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightDeath { get; set; } = 12f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight14", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightTemperance { get; set; } = 4f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight16", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightTower { get; set; } = 3f;
}