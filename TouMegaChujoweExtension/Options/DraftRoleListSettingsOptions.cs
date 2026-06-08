using MiraAPI.GameOptions;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TownOfUs.Options;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Options;

public sealed class DraftRoleListSettingsOptions : AbstractOptionGroup
{
    public override string GroupName => "Draft Role List Settings";
    public override uint GroupPriority => 104;
    public override Color GroupColor => new Color32(255, 182, 200, 255);
    public override Func<bool> GroupVisible => () => OptionGroupSingleton<DraftModeOptions>.Instance.IsRoleListDraft;

    private static readonly string[] RoleListOptionNames =
    [
        MiscUtils.GetParsedRoleBucket("CrewInvestigative"),
        MiscUtils.GetParsedRoleBucket("CrewKilling"),
        MiscUtils.GetParsedRoleBucket("CrewProtective"),
        MiscUtils.GetParsedRoleBucket("CrewPower"),
        MiscUtils.GetParsedRoleBucket("CrewSupport"),

        MiscUtils.GetParsedRoleBucket("CommonCrew"),
        MiscUtils.GetParsedRoleBucket("SpecialCrew"),
        MiscUtils.GetParsedRoleBucket("RandomCrew"),

        MiscUtils.GetParsedRoleBucket("NeutralBenign"),
        MiscUtils.GetParsedRoleBucket("NeutralEvil"),
        MiscUtils.GetParsedRoleBucket("NeutralKilling"),
        MiscUtils.GetParsedRoleBucket("NeutralOutlier"),

        MiscUtils.GetParsedRoleBucket("CommonNeutral"),
        MiscUtils.GetParsedRoleBucket("SpecialNeutral"),
        MiscUtils.GetParsedRoleBucket("WildcardNeutral"),
        MiscUtils.GetParsedRoleBucket("RandomNeutral"),

        MiscUtils.GetParsedRoleBucket("ImpConcealing"),
        MiscUtils.GetParsedRoleBucket("ImpKilling"),
        MiscUtils.GetParsedRoleBucket("ImpPower"),
        MiscUtils.GetParsedRoleBucket("ImpSupport"),

        MiscUtils.GetParsedRoleBucket("CommonImp"),
        MiscUtils.GetParsedRoleBucket("SpecialImp"),
        MiscUtils.GetParsedRoleBucket("RandomImp"),

        MiscUtils.GetParsedRoleBucket("NonImp"),
        MiscUtils.GetParsedRoleBucket("Any")
    ];

    public ModdedEnumOption<RoleListOption> Slot1 { get; } = CreateSlotOption(1, RoleListOption.CrewCommon);
    public ModdedEnumOption<RoleListOption> Slot2 { get; } = CreateSlotOption(2, RoleListOption.CrewCommon);
    public ModdedEnumOption<RoleListOption> Slot3 { get; } = CreateSlotOption(3, RoleListOption.CrewCommon);
    public ModdedEnumOption<RoleListOption> Slot4 { get; } = CreateSlotOption(4, RoleListOption.ImpCommon);
    public ModdedEnumOption<RoleListOption> Slot5 { get; } = CreateSlotOption(5, RoleListOption.CrewCommon);
    public ModdedEnumOption<RoleListOption> Slot6 { get; } = CreateSlotOption(6, RoleListOption.CrewCommon);
    public ModdedEnumOption<RoleListOption> Slot7 { get; } = CreateSlotOption(7, RoleListOption.CrewCommon);
    public ModdedEnumOption<RoleListOption> Slot8 { get; } = CreateSlotOption(8, RoleListOption.CrewCommon);
    public ModdedEnumOption<RoleListOption> Slot9 { get; } = CreateSlotOption(9, RoleListOption.ImpCommon);
    public ModdedEnumOption<RoleListOption> Slot10 { get; } = CreateSlotOption(10, RoleListOption.CrewCommon);
    public ModdedEnumOption<RoleListOption> Slot11 { get; } = CreateSlotOption(11, RoleListOption.CrewCommon);
    public ModdedEnumOption<RoleListOption> Slot12 { get; } = CreateSlotOption(12, RoleListOption.CrewCommon);
    public ModdedEnumOption<RoleListOption> Slot13 { get; } = CreateSlotOption(13, RoleListOption.CrewCommon);
    public ModdedEnumOption<RoleListOption> Slot14 { get; } = CreateSlotOption(14, RoleListOption.ImpCommon);
    public ModdedEnumOption<RoleListOption> Slot15 { get; } = CreateSlotOption(15, RoleListOption.CrewCommon);
    public ModdedEnumOption<RoleListOption> Slot16 { get; } = CreateSlotOption(16, RoleListOption.CrewCommon);
    public ModdedEnumOption<RoleListOption> Slot17 { get; } = CreateSlotOption(17, RoleListOption.CrewCommon);
    public ModdedEnumOption<RoleListOption> Slot18 { get; } = CreateSlotOption(18, RoleListOption.CrewCommon);
    public ModdedEnumOption<RoleListOption> Slot19 { get; } = CreateSlotOption(19, RoleListOption.ImpCommon);
    public ModdedEnumOption<RoleListOption> Slot20 { get; } = CreateSlotOption(20, RoleListOption.CrewCommon);

    private static ModdedEnumOption<RoleListOption> CreateSlotOption(int slot, RoleListOption defaultValue)
    {
        return new($"Slot {slot}", defaultValue, RoleListOptionNames);
    }
}
