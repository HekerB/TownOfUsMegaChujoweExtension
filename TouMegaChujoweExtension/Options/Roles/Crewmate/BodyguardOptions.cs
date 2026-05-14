using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public enum BodyguardShieldVisibility
{
    Bodyguard = 0,
    Target = 1,
    TargetAndBodyguard = 2,
    Everyone = 3
}

public sealed class BodyguardOptions : AbstractOptionGroup<BodyguardRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleBodyguard", "Bodyguard");

    [ModdedToggleOption("ExtensionOptionBodyguardDiesAfterKill")]
    public bool DiesAfterKill { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionBodyguardOnlyTargetAttacker")]
    public bool OnlyTargetAttacker { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionBodyguardShowBacklashArrow")]
    public bool ShowBacklashArrow { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionBodyguardGreenNameOnAttacker")]
    public bool GreenNameOnAttacker { get; set; } = true;

    [ModdedEnumOption(
        "ExtensionOptionBodyguardShowShieldToTarget",
        typeof(BodyguardShieldVisibility),
        [
            "ExtensionOptionBodyguardShieldEnumBodyguard",
            "ExtensionOptionBodyguardShieldEnumTarget",
            "ExtensionOptionBodyguardShieldEnumTargetAndBodyguard",
            "ExtensionOptionBodyguardShieldEnumEveryone"
        ]
    )]
    public BodyguardShieldVisibility ShowShieldTo { get; set; } = BodyguardShieldVisibility.TargetAndBodyguard;

    [ModdedNumberOption("ExtensionOptionBodyguardBacklashWindow", 3f, 30f, 1f, MiraNumberSuffixes.Seconds)]
    public float BacklashWindow { get; set; } = 10f;

    [ModdedNumberOption("ExtensionOptionBodyguardKillWindow", 3f, 20f, 1f, MiraNumberSuffixes.Seconds)]
    public float KillWindow { get; set; } = 8f;
}














