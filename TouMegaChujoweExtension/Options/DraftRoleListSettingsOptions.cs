using System;
using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Options;

public sealed class DraftRoleListSettingsOptions : AbstractOptionGroup
{
    public override string GroupName => TouLocale.Get("ExtensionDraftRoleListSettingsGroup", "Draft Role List Settings");
    public override uint GroupPriority => 104;
    public override Color GroupColor => new Color32(255, 182, 200, 255);
    public override Func<bool> GroupVisible => () => OptionGroupSingleton<DraftModeOptions>.Instance.IsRoleListDraft;

    private static string CrewNeuName =>
        $"<color=#8CFFFF>{TouLocale.Get("ExtensionDraftHudCrewmate", "Crewmate")}</color> + " +
        $"<color=#B8B8B8>{TouLocale.Get("Neutral", "Neutral")}</color>";

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
        MiscUtils.GetParsedRoleBucket("Any"),
        CrewNeuName
    ];

    public ModdedEnumOption<DraftRoleListOption> Slot1 { get; } = CreateSlotOption(1, DraftRoleListOption.ImpRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot2 { get; } = CreateSlotOption(2, DraftRoleListOption.ImpRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot3 { get; } = CreateSlotOption(3, DraftRoleListOption.CrewNeu);
    public ModdedEnumOption<DraftRoleListOption> Slot4 { get; } = CreateSlotOption(4, DraftRoleListOption.NeutBenign);
    public ModdedEnumOption<DraftRoleListOption> Slot5 { get; } = CreateSlotOption(5, DraftRoleListOption.NeutEvil);

    public ModdedEnumOption<DraftRoleListOption> Slot6 { get; } = CreateSlotOption(6, DraftRoleListOption.NeutKilling);
    public ModdedEnumOption<DraftRoleListOption> Slot7 { get; } = CreateSlotOption(7, DraftRoleListOption.CrewKilling);
    public ModdedEnumOption<DraftRoleListOption> Slot8 { get; } = CreateSlotOption(8, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot9 { get; } = CreateSlotOption(9, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot10 { get; } = CreateSlotOption(10, DraftRoleListOption.CrewRandom);

    public ModdedEnumOption<DraftRoleListOption> Slot11 { get; } = CreateSlotOption(11, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot12 { get; } = CreateSlotOption(12, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot13 { get; } = CreateSlotOption(13, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot14 { get; } = CreateSlotOption(14, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot15 { get; } = CreateSlotOption(15, DraftRoleListOption.CrewRandom);

    public ModdedEnumOption<DraftRoleListOption> Slot16 { get; } = CreateSlotOption(16, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot17 { get; } = CreateSlotOption(17, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot18 { get; } = CreateSlotOption(18, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot19 { get; } = CreateSlotOption(19, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot20 { get; } = CreateSlotOption(20, DraftRoleListOption.CrewRandom);

    public ModdedEnumOption<DraftRoleListOption> Slot21 { get; } = CreateSlotOption(21, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot22 { get; } = CreateSlotOption(22, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot23 { get; } = CreateSlotOption(23, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot24 { get; } = CreateSlotOption(24, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot25 { get; } = CreateSlotOption(25, DraftRoleListOption.CrewRandom);

    public ModdedEnumOption<DraftRoleListOption> Slot26 { get; } = CreateSlotOption(26, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot27 { get; } = CreateSlotOption(27, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot28 { get; } = CreateSlotOption(28, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot29 { get; } = CreateSlotOption(29, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot30 { get; } = CreateSlotOption(30, DraftRoleListOption.CrewRandom);

    public ModdedEnumOption<DraftRoleListOption> Slot31 { get; } = CreateSlotOption(31, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot32 { get; } = CreateSlotOption(32, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot33 { get; } = CreateSlotOption(33, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot34 { get; } = CreateSlotOption(34, DraftRoleListOption.CrewRandom);
    public ModdedEnumOption<DraftRoleListOption> Slot35 { get; } = CreateSlotOption(35, DraftRoleListOption.CrewRandom);

    private static ModdedEnumOption<DraftRoleListOption> CreateSlotOption(int slot, DraftRoleListOption defaultValue)
    {
        return new($"{TouLocale.Get("ExtensionDraftOptionSlot", "Slot")} {slot}", defaultValue, RoleListOptionNames)
        {
            Visible = () => SlotVisible(slot)
        };
    }

    private static bool SlotVisible(int slot)
    {
        return OptionGroupSingleton<DraftModeOptions>.Instance.IsRoleListDraft
            && slot <= GetLobbyMaxPlayers();
    }

    private static int GetLobbyMaxPlayers()
    {
        try
        {
            return GameOptionsManager.Instance.CurrentGameOptions.GetInt(Int32OptionNames.MaxPlayers);
        }
        catch
        {
            return 15;
        }
    }
}
