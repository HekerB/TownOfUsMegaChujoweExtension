using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMiraRolesExtension.Roles.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMiraRolesExtension.Options.Roles.Impostor;

public sealed class CharlatanOptions : AbstractOptionGroup<CharlatanRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleCharlatan", "Charlatan");

    [ModdedNumberOption("ExtensionOptionCharlatanDeceiveBaseDuration", 0f, 10f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float DeceiveBaseDuration { get; set; } = 5f;

    [ModdedNumberOption("ExtensionOptionCharlatanDeceiveDurationIncreasePerKill", 0f, 5f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float DeceiveDurationIncreasePerKill { get; set; } = 2.5f;

    [ModdedNumberOption("ExtensionOptionCharlatanConcealUses", 0f, 5f, 1f, MiraNumberSuffixes.None)]
    public float ConcealUses { get; set; } = 2f;

    [ModdedNumberOption("ExtensionOptionCharlatanConcealChargesPerKill", 1f, 5f, 1f, MiraNumberSuffixes.None)]
    public float ConcealChargesPerKill { get; set; } = 1f;

    [ModdedEnumOption("ExtensionOptionCharlatanConcealReportRange", typeof(ReportRangeType))]
    public ReportRangeType ConcealReportRange { get; set; } = ReportRangeType.VeryShort;

    [ModdedNumberOption("ExtensionOptionCharlatanConcealChannelDuration", 1f, 5f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float ConcealChannelDuration { get; set; } = 2.5f;
}

public enum ReportRangeType
{
    VeryShort = 0,
    Short = 1
}

