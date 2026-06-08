using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;

namespace TouMegaChujoweExtension.Options;

public enum DraftPoolMode
{
    OldDraft,
    MinMax,
    RoleList
}

public sealed class DraftModeOptions : AbstractOptionGroup
{
    public override string GroupName => "Draft Mode Settings";
    public override uint GroupPriority => 100;
    public override Func<bool> GroupVisible => () => true;

    // === CORE ===

    [ModdedToggleOption("Enable Draft Mode")]
    public bool EnableDraftMode { get; set; } = false;

    private static readonly string[] DraftPoolModeNames =
    [
        "Old Draft",
        "Min/Max",
        "Role List"
    ];

    public ModdedEnumOption<DraftPoolMode> PoolMode { get; } =
        new("Draft Pool Mode", DraftPoolMode.OldDraft, DraftPoolModeNames)
        {
            Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
        };

    public bool IsOldDraft => EnableDraftMode && PoolMode.Value == DraftPoolMode.OldDraft;
    public bool IsMinMaxDraft => EnableDraftMode && PoolMode.Value == DraftPoolMode.MinMax;
    public bool IsRoleListDraft => EnableDraftMode && PoolMode.Value == DraftPoolMode.RoleList;

    public ModdedToggleOption LockLobbyDuringDraft { get; } = new("Lock Lobby During Draft", true)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedToggleOption RespectRoleChances { get; } = new("Use Role Chances", true)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption RolesToShow { get; } = new("Roles To Show", 3f, 1f, 8f, 1f, MiraNumberSuffixes.None)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption TimeToChoose { get; } = new("Time To Choose", 10f, 3f, 67f, 1f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedToggleOption ReduceKillingStreak { get; } = new("Reduce Killing Streak", true)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption ReductionChance { get; } = new("Imp & neu kill", 15f, 0f, 100f, 5f, MiraNumberSuffixes.Percent)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.ReduceKillingStreak.Value && OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

}
