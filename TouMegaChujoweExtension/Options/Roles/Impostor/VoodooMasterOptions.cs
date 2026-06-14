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

    [ModdedNumberOption("ExtensionOptionVoodooMasterCurseCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float CurseCooldown { get; set; } = 30f;

    public ModdedNumberOption MaxBlindCurses { get; } = new("ExtensionOptionVoodooMasterMaxBlindUses", 3f, -1f, 15f, 1f, "#", "∞", MiraNumberSuffixes.None, "0");

    [ModdedNumberOption("ExtensionOptionVoodooMasterBlindDelay", 0.5f, 5f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float BlindDelay { get; set; } = 3f;

    [ModdedNumberOption("ExtensionOptionVoodooMasterBlindDuration", 5f, 30f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float BlindDuration { get; set; } = 15f;

    public ModdedNumberOption MaxConfuseCurses { get; } = new("ExtensionOptionVoodooMasterMaxConfuseUses", 5f, -1f, 15f, 1f, "#", "∞", MiraNumberSuffixes.None, "0");

    [ModdedNumberOption("ExtensionOptionVoodooMasterConfuseDelay", 0.5f, 5f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float ConfuseDelay { get; set; } = 3f;

    [ModdedNumberOption("ExtensionOptionVoodooMasterConfuseDuration", 5f, 30f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float ConfuseDuration { get; set; } = 15f;

    public ModdedNumberOption MaxMuteCurses { get; } = new("ExtensionOptionVoodooMasterMaxMuteUses", 7f, -1f, 15f, 1f, "#", "∞", MiraNumberSuffixes.None, "0");

    [ModdedNumberOption("ExtensionOptionVoodooMasterMuteDuration", 1f, 3f, 1f, MiraNumberSuffixes.None)]
    public float MuteDuration { get; set; } = 1f;

    [ModdedNumberOption("ExtensionOptionVoodooMasterTargetLockDuration", 0f, 5f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float TargetLockDurationRounds { get; set; } = 2f;
}
