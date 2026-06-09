using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class VoodooMasterOptions : AbstractOptionGroup<VoodooMasterRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleVoodooMaster", "Voodoo Master");

    [ModdedNumberOption("ExtensionOptionVoodooMasterCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float Cooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionVoodooMasterBlindDuration", 5f, 30f, 1f, MiraNumberSuffixes.Seconds)]
    public float BlindDuration { get; set; } = 15f;

    [ModdedToggleOption("ExtensionOptionVoodooMasterCanVent")]
    public bool CanVent { get; set; } = true;
}
