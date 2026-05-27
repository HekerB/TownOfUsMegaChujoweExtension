using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class BakerOptions : AbstractOptionGroup<BakerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleBaker", "Baker");

    [ModdedNumberOption("ExtensionOptionBakerInstantFamineChance", 0f, 100f, 5f, MiraNumberSuffixes.Percent)]
    public float InstantFamineChance { get; set; } = 0f;

    [ModdedNumberOption("ExtensionOptionBakerGiveCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float GiveCooldown { get; set; } = 27.5f;

    [ModdedNumberOption("ExtensionOptionBakerBreadNeeded", 1f, 10f, 1f, MiraNumberSuffixes.None)]
    public float BreadNeeded { get; set; } = 3f;

    [ModdedToggleOption("ExtensionOptionBakerAnnounceFamine")]
    public bool AnnounceFamine { get; set; } = true;

    [ModdedNumberOption("ExtensionOptionBakerStarveCooldown", 5f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float StarveCooldown { get; set; } = 20f;

    [ModdedToggleOption("ExtensionOptionBakerCanVent")]
    public bool CanVent { get; set; } = false;
}
