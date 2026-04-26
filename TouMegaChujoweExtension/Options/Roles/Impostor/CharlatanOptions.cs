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

    [ModdedNumberOption("ExtensionOptionCharlatanConcealCooldown", 5f, 300f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float ConcealCooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionCharlatanConcealUses", 0f, 10f, 1f, MiraNumberSuffixes.None)]
    public float ConcealUses { get; set; } = 2f;

    [ModdedNumberOption("ExtensionOptionCharlatanConcealChargesPerKill", 1f, 10f, 1f, MiraNumberSuffixes.None)]
    public float ConcealChargesPerKill { get; set; } = 1f;

    [ModdedNumberOption("ExtensionOptionCharlatanConcealChannelDuration", 1f, 15f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float ConcealChannelDuration { get; set; } = 2.5f;

    [ModdedEnumOption("ExtensionOptionCharlatanConcealReportRange", typeof(ReportRangeType), ["ExtensionOptionCharlatanConcealReportRangeEnumExtremelyShort", "ExtensionOptionCharlatanConcealReportRangeEnumVeryShort", "ExtensionOptionCharlatanConcealReportRangeEnumShort"])]
    public ReportRangeType ConcealReportRange { get; set; } = ReportRangeType.VeryShort;

    [ModdedToggleOption("ExtensionOptionCharlatanDeceiveEnabled")]
    public bool DeceiveEnabled { get; set; } = true;

    public ModdedNumberOption DeceiveBaseDurationOption { get; } = new("ExtensionOptionCharlatanDeceiveBaseDuration", 15f, 0f, 60f, 1f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<CharlatanOptions>.Instance.DeceiveEnabled
    };

    public ModdedNumberOption DeceiveDurationIncreasePerKillOption { get; } = new("ExtensionOptionCharlatanDeceiveDurationIncreasePerKill", 2.5f, 0f, 15f, 0.5f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<CharlatanOptions>.Instance.DeceiveEnabled
    };

    // Backward compatibility for CharlatanRole
    public float DeceiveBaseDuration => DeceiveBaseDurationOption.Value;
    public float DeceiveDurationIncreasePerKill => DeceiveDurationIncreasePerKillOption.Value;
}

public enum ReportRangeType
{
    ExtremelyShort = 0,
    VeryShort = 1,
    Short = 2
}

