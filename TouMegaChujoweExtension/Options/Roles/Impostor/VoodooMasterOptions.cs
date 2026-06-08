using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class VoodooMasterOptions : AbstractOptionGroup<VoodooMasterRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleVoodooMaster", "Voodoo Master");

    [ModdedNumberOption("ExtensionOptionVoodooMasterMaxCurses", 0f, 15f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float MaxCurses { get; set; } = 0f;

    [ModdedNumberOption("ExtensionOptionVoodooMasterBlindCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float BlindCooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionVoodooMasterBlindDuration", 5f, 30f, 1f, MiraNumberSuffixes.Seconds)]
    public float BlindDuration { get; set; } = 15f;

    [ModdedNumberOption("ExtensionOptionVoodooMasterConfuseCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float ConfuseCooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionVoodooMasterConfuseDuration", 3f, 30f, 1f, MiraNumberSuffixes.Seconds)]
    public float ConfuseDuration { get; set; } = 10f;

    [ModdedNumberOption("ExtensionOptionVoodooMasterMuteCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float MuteCooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionVoodooMasterMuteDuration", 1f, 3f, 1f, MiraNumberSuffixes.None)]
    public float MuteDuration { get; set; } = 1f;

    [ModdedToggleOption("ExtensionOptionVoodooMasterBlindTargetAlert")]
    public bool BlindTargetAlert { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionVoodooMasterCanVent")]
    public bool CanVent { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionVoodooMasterImpostorsSeeMuted")]
    public bool ImpostorsSeeMuted { get; set; } = true;
}
