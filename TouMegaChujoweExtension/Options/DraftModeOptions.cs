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

    public ModdedNumberOption MinOtherNeutralsPerChoice { get; } = new("Min Neutrals Per Choice", 0f, 0f, 3f, 1f, MiraNumberSuffixes.None)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption MaxOtherNeutralsPerChoice { get; } = new("Max Neutrals Per Choice", 0f, 0f, 3f, 1f, MiraNumberSuffixes.None)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedToggleOption ReduceKillingStreak { get; } = new("Reduce Killing Streak", false)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption ReductionChance { get; } = new("Impostor", 15f, 0f, 100f, 5f, MiraNumberSuffixes.Percent)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.ReduceKillingStreak.Value && OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption NKReductionChance { get; } = new("Neutral Killing", 15f, 0f, 100f, 5f, MiraNumberSuffixes.Percent)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.ReduceKillingStreak.Value && OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption MinOtherNeutrals { get; } = new("Min Other Neutrals", 0f, 0f, 10f, 1f, MiraNumberSuffixes.None)
    {
        Visible = () => OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode
    };

    public ModdedNumberOption MaxOtherNeutrals { get; } = new("Max Other Neutrals", 0f, 0f, 10f, 1f, MiraNumberSuffixes.None)
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
}