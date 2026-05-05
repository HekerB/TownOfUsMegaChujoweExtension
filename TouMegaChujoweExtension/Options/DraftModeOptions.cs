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

    public ModdedToggleOption ImpostorsPickFromAllClasses { get; } = new("All Classes (Imp)", false)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedToggleOption CrewmatesPickFromAllClasses { get; } = new("All Classes (Crew)", false)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedToggleOption RespectRoleChances { get; } = new("Use Role Chances", false)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedToggleOption MergeNeutralsWithCrew { get; } = new("Merge Neutrals with Crew", false)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption NeutralMergeChance { get; } = new("Neutral Merge Chance", 15f, 5f, 50f, 5f, MiraNumberSuffixes.Percent)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.MergeNeutralsWithCrew.Value && OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption MaxNeutralsInMerge { get; } = new("Max Neutrals in Merge", 1f, 1f, 3f, 1f, MiraNumberSuffixes.None)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.MergeNeutralsWithCrew.Value && OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
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

    public ModdedNumberOption ReductionChance { get; } = new("Impostor", 20f, 0f, 100f, 5f, MiraNumberSuffixes.Percent)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.ReduceKillingStreak.Value && OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption NKReductionChance { get; } = new("Neutral Killing", 20f, 0f, 100f, 5f, MiraNumberSuffixes.Percent)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.ReduceKillingStreak.Value && OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
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
