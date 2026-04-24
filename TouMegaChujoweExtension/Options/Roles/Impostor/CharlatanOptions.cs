using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class CharlatanOptions : AbstractOptionGroup<CharlatanRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleCharlatan", "Charlatan");

    [ModdedNumberOption("ExtensionOptionCharlatanDeceiveBaseDuration", 0f, 60f, 1f, MiraNumberSuffixes.Seconds)]
    public float DeceiveBaseDuration { get; set; } = 15f;

    [ModdedNumberOption("ExtensionOptionCharlatanDeceiveDurationIncreasePerKill", 0f, 15f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float DeceiveDurationIncreasePerKill { get; set; } = 2.5f;

    [ModdedNumberOption("ExtensionOptionCharlatanConcealUses", 0f, 10f, 1f, MiraNumberSuffixes.None)]
    public float ConcealUses { get; set; } = 2f;

    [ModdedNumberOption("ExtensionOptionCharlatanConcealChargesPerKill", 1f, 10f, 1f, MiraNumberSuffixes.None)]
    public float ConcealChargesPerKill { get; set; } = 1f;

    [ModdedEnumOption("ExtensionOptionCharlatanConcealReportRange", typeof(ReportRangeType), ["ExtensionOptionCharlatanConcealReportRangeEnumExtremelyShort", "ExtensionOptionCharlatanConcealReportRangeEnumVeryShort", "ExtensionOptionCharlatanConcealReportRangeEnumShort"])]
    public ReportRangeType ConcealReportRange { get; set; } = ReportRangeType.VeryShort;

    [ModdedNumberOption("ExtensionOptionCharlatanConcealChannelDuration", 1f, 15f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float ConcealChannelDuration { get; set; } = 2.5f;

    [ModdedNumberOption("ExtensionOptionCharlatanConcealCooldown", 5f, 300f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float ConcealCooldown { get; set; } = 30f;
}

public enum ReportRangeType
{
    ExtremelyShort = 0,
    VeryShort = 1,
    Short = 2
}

