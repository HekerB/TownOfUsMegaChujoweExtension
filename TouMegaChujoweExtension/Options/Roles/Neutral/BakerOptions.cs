using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class BakerOptions : AbstractOptionGroup<BakerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleBaker", "Baker");

    [ModdedNumberOption("ExtensionOptionBakerBreadNeeded", 1f, 10f, 1f, MiraNumberSuffixes.None)]
    public float BreadNeeded { get; set; } = 3f;

    [ModdedNumberOption("ExtensionOptionBakerGiveCooldown", 5f, 60f, 1f, MiraNumberSuffixes.Seconds, "0")]
    public float GiveCooldown { get; set; } = 20f;

    [ModdedNumberOption("ExtensionOptionBakerStarveCooldown", 5f, 60f, 1f, MiraNumberSuffixes.Seconds, "0")]
    public float StarveCooldown { get; set; } = 25f;

    [ModdedToggleOption("ExtensionOptionBakerCanVent")]
    public bool CanVent { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionBakerAnnounceFamine")]
    public bool AnnounceFamine { get; set; } = true;
}
