using MiraAPI.GameOptions;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TownOfUs;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Options;

public sealed class DraftCrewmateSettingsOptions : AbstractOptionGroup
{
    public override string GroupName => TouLocale.Get("ExtensionDraftCrewmateSettingsGroup", "Draft Crewmate Settings");
    public override uint GroupPriority => 101;
    public override Color GroupColor => Palette.CrewmateRoleHeaderBlue;
    public override Func<bool> GroupVisible => () => OptionGroupSingleton<DraftModeOptions>.Instance.IsMinMaxDraft;

    public ModdedNumberOption MaxCrewInvestigative { get; } = new("ExtensionDraftOptionMaxInvestigative", 5f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxCrewKilling { get; } = new("ExtensionDraftOptionMaxKilling", 3f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxCrewPower { get; } = new("ExtensionDraftOptionMaxPower", 2f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxCrewProtective { get; } = new("ExtensionDraftOptionMaxProtective", 2f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxCrewSupport { get; } = new("ExtensionDraftOptionMaxSupport", 3f, 0f, 20f, 1f, MiraNumberSuffixes.None);
}

public sealed class DraftImpostorSettingsOptions : AbstractOptionGroup
{
    public override string GroupName => TouLocale.Get("ExtensionDraftImpostorSettingsGroup", "Draft Impostor Settings");
    public override uint GroupPriority => 102;
    public override Color GroupColor => Palette.ImpostorRoleHeaderRed;
    public override Func<bool> GroupVisible => () => OptionGroupSingleton<DraftModeOptions>.Instance.IsMinMaxDraft;

    public ModdedNumberOption MaxImpostorsTotal { get; } = new("ExtensionDraftOptionMaxImpostorsTotal", 1f, 0f, 5f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxImpConcealing { get; } = new("ExtensionDraftOptionMaxConcealing", 2f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxImpKilling { get; } = new("ExtensionDraftOptionMaxKilling", 2f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxImpPower { get; } = new("ExtensionDraftOptionMaxPower", 2f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxImpSupport { get; } = new("ExtensionDraftOptionMaxSupport", 2f, 0f, 20f, 1f, MiraNumberSuffixes.None);
}

public sealed class DraftNeutralSettingsOptions : AbstractOptionGroup
{
    public override string GroupName => TouLocale.Get("ExtensionDraftNeutralSettingsGroup", "Draft Neutral Settings");
    public override uint GroupPriority => 103;
    public override Color GroupColor => TownOfUsColors.Neutral;
    public override Func<bool> GroupVisible => () => OptionGroupSingleton<DraftModeOptions>.Instance.IsMinMaxDraft;

    public ModdedNumberOption MaxNeutralTotal { get; } = new("ExtensionDraftOptionMaxNeutralTotal", 3f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxNeutralBenign { get; } = new("ExtensionDraftOptionMaxBenign", 0f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxNeutralEvil { get; } = new("ExtensionDraftOptionMaxEvil", 1f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxNeutralKillingRoles { get; } = new("ExtensionDraftOptionMaxNeutralKillingRoles", 1f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxNeutralOutlier { get; } = new("ExtensionDraftOptionMaxOutlier", 0f, 0f, 20f, 1f, MiraNumberSuffixes.None);
}
