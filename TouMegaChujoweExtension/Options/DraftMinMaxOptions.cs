using MiraAPI.GameOptions;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Options;

public sealed class DraftCrewmateSettingsOptions : AbstractOptionGroup
{
    public override string GroupName => "Draft Crewmate Settings";
    public override uint GroupPriority => 101;
    public override Color GroupColor => Palette.CrewmateRoleHeaderBlue;
    public override Func<bool> GroupVisible => () => OptionGroupSingleton<DraftModeOptions>.Instance.IsMinMaxDraft;

    public ModdedNumberOption MaxCrewInvestigative { get; } = new("Max Investigative Roles", 5f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxCrewKilling { get; } = new("Max Killing Roles", 3f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxCrewPower { get; } = new("Max Power Roles", 2f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxCrewProtective { get; } = new("Max Protective Roles", 2f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxCrewSupport { get; } = new("Max Support Roles", 3f, 0f, 20f, 1f, MiraNumberSuffixes.None);
}

public sealed class DraftImpostorSettingsOptions : AbstractOptionGroup
{
    public override string GroupName => "Draft Impostor Settings";
    public override uint GroupPriority => 102;
    public override Color GroupColor => Palette.ImpostorRoleHeaderRed;
    public override Func<bool> GroupVisible => () => OptionGroupSingleton<DraftModeOptions>.Instance.IsMinMaxDraft;

    public ModdedNumberOption MaxImpostorsTotal { get; } = new("Max Impostors Total", 1f, 0f, 5f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxImpConcealing { get; } = new("Max Concealing Roles", 2f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxImpKilling { get; } = new("Max Killing Roles", 2f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxImpPower { get; } = new("Max Power Roles", 2f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxImpSupport { get; } = new("Max Support Roles", 2f, 0f, 20f, 1f, MiraNumberSuffixes.None);
}

public sealed class DraftNeutralSettingsOptions : AbstractOptionGroup
{
    public override string GroupName => "Draft Neutral Settings";
    public override uint GroupPriority => 103;
    public override Color GroupColor => TownOfUsColors.Neutral;
    public override Func<bool> GroupVisible => () => OptionGroupSingleton<DraftModeOptions>.Instance.IsMinMaxDraft;

    public ModdedNumberOption MaxNeutralTotal { get; } = new("Max Neutral Roles", 3f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxNeutralBenign { get; } = new("Max Benign Roles", 0f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxNeutralEvil { get; } = new("Max Evil Roles", 1f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxNeutralKillingRoles { get; } = new("Max Neutral Killing Roles", 1f, 0f, 20f, 1f, MiraNumberSuffixes.None);
    public ModdedNumberOption MaxNeutralOutlier { get; } = new("Max Outlier Roles", 0f, 0f, 20f, 1f, MiraNumberSuffixes.None);
}
