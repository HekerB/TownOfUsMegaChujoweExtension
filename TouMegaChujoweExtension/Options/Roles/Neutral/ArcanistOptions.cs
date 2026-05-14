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

    [ModdedNumberOption("ExtensionOptionArcanistCooldown", 10f, 120f, 5f, MiraNumberSuffixes.Seconds)]
    public float Cooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionArcanistDeckSize", 1f, 22f, 1f)]
    public float DeckSize { get; set; } = 10f;

    [ModdedToggleOption("ExtensionOptionArcanistAllowDuplicateRoles")]
    public bool AllowDuplicateRoles { get; set; } = false;

    [ModdedNumberOption("ExtensionOptionArcanistWeight00", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightFool { get; set; } = 3f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight01", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightMagician { get; set; } = 5f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight02", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightHighPriestess { get; set; } = 3f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight03", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightEmpress { get; set; } = 10f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight04", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightEmperor { get; set; } = 3f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight05", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightHierophant { get; set; } = 5f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight06", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightLovers { get; set; } = 3f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight07", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightChariot { get; set; } = 5f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight08", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightStrength { get; set; } = 5f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight09", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightHermit { get; set; } = 4f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight10", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightWheel { get; set; } = 3f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight11", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightJustice { get; set; } = 3f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight12", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightHangedMan { get; set; } = 3f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight13", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightDeath { get; set; } = 15f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight14", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightTemperance { get; set; } = 3f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight15", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightDevil { get; set; } = 2f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight16", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightTower { get; set; } = 5f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight17", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightStar { get; set; } = 5f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight18", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightMoon { get; set; } = 5f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight19", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightSun { get; set; } = 5f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight20", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightJudgement { get; set; } = 3f;
    [ModdedNumberOption("ExtensionOptionArcanistWeight21", 0f, 100f, 1f, MiraNumberSuffixes.Percent)] public float WeightWorld { get; set; } = 3f;
}