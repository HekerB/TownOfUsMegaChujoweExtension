using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class MirageOptions : AbstractOptionGroup<MirageRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleMirage", "Mirage");

    [ModdedNumberOption("ExtensionOptionMirageDecoyCooldown", 1f, 60f, 1f, MiraNumberSuffixes.Seconds)]
    public float DecoyCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionMirageInitialUses", 0f, 6f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float InitialUses { get; set; } = 0f;

    [ModdedToggleOption("ExtensionOptionMirageRevealInteractorRole")]
    public bool RevealInteractorRole { get; set; } = false;

    [ModdedEnumOption("ExtensionOptionMirageArrowTarget", typeof(MirageArrowTarget),
        ["ExtensionOptionMirageArrowTargetEnumMirage", "ExtensionOptionMirageArrowTargetEnumInteractor"])]
    public MirageArrowTarget ArrowTarget { get; set; } = MirageArrowTarget.Interactor;

    public ModdedNumberOption DecoyDuration { get; } =
        new("ExtensionOptionMirageDecoyDuration", 0f, 0f, 60f, 1f, "Off", "#", MiraNumberSuffixes.Seconds, "0");

    [ModdedNumberOption("ExtensionOptionMirageArrowTime", 0f, 15f, 0.5f, MiraNumberSuffixes.Seconds, "0", true)]
    public float ArrowTime { get; set; } = 6f;
}

public enum MirageArrowTarget
{
    Mirage,
    Interactor
}
