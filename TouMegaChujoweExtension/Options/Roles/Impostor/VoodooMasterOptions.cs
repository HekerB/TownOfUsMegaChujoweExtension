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

    [ModdedNumberOption("ExtensionOptionVoodooMasterMaxBlindUses", 0f, 15f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float MaxBlindCurses { get; set; } = 2f;

    [ModdedNumberOption("ExtensionOptionVoodooMasterMaxConfuseUses", 0f, 15f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float MaxConfuseCurses { get; set; } = 2f;

    [ModdedNumberOption("ExtensionOptionVoodooMasterMaxMuteUses", 0f, 15f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float MaxMuteCurses { get; set; } = 2f;

    [ModdedNumberOption("ExtensionOptionVoodooMasterBlindDuration", 5f, 30f, 1f, MiraNumberSuffixes.Seconds)]
    public float BlindDuration { get; set; } = 15f;

    [ModdedNumberOption("ExtensionOptionVoodooMasterConfuseDuration", 3f, 30f, 1f, MiraNumberSuffixes.Seconds)]
    public float ConfuseDuration { get; set; } = 10f;

    [ModdedNumberOption("ExtensionOptionVoodooMasterMuteDuration", 1f, 3f, 1f, MiraNumberSuffixes.None)]
    public float MuteDuration { get; set; } = 1f;

    [ModdedToggleOption("ExtensionOptionVoodooMasterMoveWhileMenu")]
    public bool MoveWhileMenu { get; set; } = true;
}
