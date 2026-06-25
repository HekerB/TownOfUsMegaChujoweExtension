using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options;

public enum DraftPoolMode
{
    OldDraft,
    MinMax,
    RoleList
}

public sealed class DraftModeOptions : AbstractOptionGroup
{
    public override string GroupName => TouLocale.Get("ExtensionDraftModeGroupName", "Draft Mode");
    public override uint GroupPriority => 100;
    public override Func<bool> GroupVisible => () => true;

    // === CORE ===

    [ModdedToggleOption("ExtensionOptionDraftModeEnabled")]
    public bool EnableDraftMode { get; set; } = false;

    private static readonly string[] DraftPoolModeNames =
    [
        "ExtensionDraftPoolModeOld",
        "ExtensionDraftPoolModeMinMax",
        "ExtensionDraftPoolModeRoleList"
    ];

    public ModdedEnumOption<DraftPoolMode> PoolMode { get; } =
        new("ExtensionDraftOptionPoolMode", DraftPoolMode.RoleList, DraftPoolModeNames)
        {
            Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
        };

    public bool IsOldDraft => EnableDraftMode && PoolMode.Value == DraftPoolMode.OldDraft;
    public bool IsMinMaxDraft => EnableDraftMode && PoolMode.Value == DraftPoolMode.MinMax;
    public bool IsRoleListDraft => EnableDraftMode && PoolMode.Value == DraftPoolMode.RoleList;

    public ModdedToggleOption RespectRoleChances { get; } = new("ExtensionDraftOptionUseRoleChances", true)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedToggleOption ExcludePreviousGameRoles { get; } = new("ExtensionDraftOptionExcludePreviousRoles", false)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption RolesToShow { get; } = new("ExtensionOptionDraftModeRolesToShow", 3f, 1f, 6f, 1f, MiraNumberSuffixes.None)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption TimeToChoose { get; } = new("ExtensionOptionDraftModeTimeToChoose", 10f, 3f, 67f, 1f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedToggleOption ReduceKillingStreak { get; } = new("ExtensionDraftOptionReduceKillingStreak", true)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption ReductionChance { get; } = new("ExtensionDraftOptionReductionChance", 15f, 0f, 100f, 5f, MiraNumberSuffixes.Percent)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.ReduceKillingStreak.Value && OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

}
