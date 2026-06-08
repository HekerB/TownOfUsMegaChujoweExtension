using MiraAPI.GameOptions;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Options;

public sealed class DraftOldSettingsOptions : AbstractOptionGroup
{
    public override string GroupName => "Old Draft Settings";
    public override uint GroupPriority => 105;
    public override Color GroupColor => TownOfUsColors.Neutral;
    public override Func<bool> GroupVisible => () => OptionGroupSingleton<DraftModeOptions>.Instance.IsOldDraft;

    public ModdedNumberOption MinOtherNeutralsPerChoice { get; } = new("Min Neutrals Per Choice", 1f, 0f, 3f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxOtherNeutralsPerChoice { get; } = new("Max Neutrals Per Choice", 2f, 0f, 3f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MinOtherNeutrals { get; } = new("Min Other Neutrals", 0f, 0f, 10f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxOtherNeutrals { get; } = new("Max Other Neutrals", 0f, 0f, 10f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MinNeutralKilling { get; } = new("Min Neutral Killing", 0f, 0f, 5f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxNeutralKilling { get; } = new("Max Neutral Killing", 0f, 0f, 5f, 1f, MiraNumberSuffixes.None);
}
