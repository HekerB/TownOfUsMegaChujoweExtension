using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class SpiritMasterOptions : AbstractOptionGroup<SpiritMasterRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleSpiritMaster", "Spirit Master");

    [ModdedNumberOption("ExtensionOptionSpiritMasterMediateCooldown", 0f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float MediateCooldown { get; set; } = 10f;

    [ModdedToggleOption("ExtensionOptionSpiritMasterRevealAppearanceOfMediateTarget")]
    public bool RevealMediateAppearance { get; set; } = true;

    private static readonly string[] ArrowVisibilityValues =
    [
        "ExtensionOptionSpiritMasterArrowEnumShowSpiritMaster",
        "ExtensionOptionSpiritMasterArrowEnumShowMediated",
        "ExtensionOptionSpiritMasterArrowEnumBoth",
        "ExtensionOptionSpiritMasterArrowEnumNone"
    ];

    public ModdedEnumOption<SpiritMasterVisibility> ArrowVisibility { get; } =
        new("ExtensionOptionSpiritMasterArrowVisibility", SpiritMasterVisibility.Both, ArrowVisibilityValues);

    private static readonly string[] RevealedTargetValues =
    [
        "ExtensionOptionSpiritMasterGhostEnumOldestDead",
        "ExtensionOptionSpiritMasterGhostEnumNewestDead",
        "ExtensionOptionSpiritMasterGhostEnumRandomDead",
        "ExtensionOptionSpiritMasterGhostEnumAllDead"
    ];

    public ModdedEnumOption<SpiritMasterRevealedTargets> WhoIsRevealed { get; } =
        new("ExtensionOptionSpiritMasterWhoIsRevealed", SpiritMasterRevealedTargets.OldestDead, RevealedTargetValues);
}

public enum SpiritMasterRevealedTargets
{
    OldestDead,
    NewestDead,
    RandomDead,
    AllDead
}

public enum SpiritMasterVisibility
{
    ShowSpiritMaster,
    ShowMediated,
    Both,
    None
}
