using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;

namespace TouMegaChujoweExtension.Options;

public sealed class DraftModeOptions : AbstractOptionGroup
{
    public override string GroupName => "Draft Mode Settings";
    public override uint GroupPriority => 100;
    public override Func<bool> GroupVisible => () => true;

    // === CORE ===

    [ModdedToggleOption("Enable Draft Mode")]
    public bool EnableDraftMode { get; set; } = false;

    public ModdedToggleOption LockLobbyDuringDraft { get; } = new("Lock Lobby During Draft", true)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedToggleOption ImpostorsPickFromAllClasses { get; } = new("Impostors Pick From All Classes", false)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedToggleOption CrewmatesPickFromAllClasses { get; } = new("Crewmates Pick From All Classes", false)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedToggleOption RespectRoleChances { get; } = new("Use Roles Chances", false)
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

    // === FACTION COUNTS ===

    public ModdedNumberOption MinNeutralBenign { get; } = new("Min Neutral Benign", 0f, 0f, 5f, 1f, MiraNumberSuffixes.None)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption MaxNeutralBenign { get; } = new("Max Neutral Benign", 0f, 0f, 5f, 1f, MiraNumberSuffixes.None)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption MinNeutralEvil { get; } = new("Min Neutral Evil", 0f, 0f, 5f, 1f, MiraNumberSuffixes.None)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption MaxNeutralEvil { get; } = new("Max Neutral Evil", 0f, 0f, 5f, 1f, MiraNumberSuffixes.None)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption MinNeutralKilling { get; } = new("Min Neutral Killing", 0f, 0f, 5f, 1f, MiraNumberSuffixes.None)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption MaxNeutralKilling { get; } = new("Max Neutral Killing", 0f, 0f, 5f, 1f, MiraNumberSuffixes.None)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption MinRandomNeutral { get; } = new("Min Random Neutral", 0f, 0f, 5f, 1f, MiraNumberSuffixes.None)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption MaxRandomNeutral { get; } = new("Max Random Neutral", 0f, 0f, 5f, 1f, MiraNumberSuffixes.None)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };
}