using MiraAPI.GameOptions;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TownOfUs;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Options;

public sealed class DraftOldSettingsOptions : AbstractOptionGroup
{
    public override string GroupName => TouLocale.Get("ExtensionDraftOldSettingsGroup", "Old Draft Settings");
    public override uint GroupPriority => 105;
    public override Color GroupColor => TownOfUsColors.Neutral;
    public override Func<bool> GroupVisible => () => OptionGroupSingleton<DraftModeOptions>.Instance.IsOldDraft;

    public ModdedNumberOption MinOtherNeutralsPerChoice { get; } = new("ExtensionDraftOptionMinNeutralsPerChoice", 1f, 0f, 3f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxOtherNeutralsPerChoice { get; } = new("ExtensionDraftOptionMaxNeutralsPerChoice", 2f, 0f, 3f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MinOtherNeutrals { get; } = new("ExtensionDraftOptionMinOtherNeutrals", 0f, 0f, 10f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxOtherNeutrals { get; } = new("ExtensionDraftOptionMaxOtherNeutrals", 0f, 0f, 10f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MinNeutralKilling { get; } = new("ExtensionDraftOptionMinNeutralKilling", 0f, 0f, 5f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxNeutralKilling { get; } = new("ExtensionDraftOptionMaxNeutralKilling", 0f, 0f, 5f, 1f, MiraNumberSuffixes.None);
}
