using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Modules.Localization;
using TownOfUs.Options.Roles.Neutral;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class VultureOptions : AbstractOptionGroup<VultureRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleVulture", "Vulture");

    [ModdedNumberOption("ExtensionOptionVultureBodiesToWin", 1f, 15f, 1f, MiraNumberSuffixes.None)]
    public float BodiesToWin { get; set; } = 3f;

    [ModdedNumberOption("ExtensionOptionVultureEatCooldown", 5f, 120f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float EatCooldown { get; set; } = 17.5f;

    [ModdedToggleOption("ExtensionOptionVultureCanVent")]
    public bool CanVent { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionVultureScavengeEnabled")]
    public bool ScavengeEnabled { get; set; } = false;

    public ModdedNumberOption ScavengeCooldown { get; } =
        new("ExtensionOptionVultureScavengeCooldown", 30f, 5f, 120f, 2.5f, MiraNumberSuffixes.Seconds)
        {
            Visible = () => OptionGroupSingleton<VultureOptions>.Instance.ScavengeEnabled,
        };


    public ModdedNumberOption ScavengeDuration { get; } =
        new("ExtensionOptionVultureScavengeDuration", 5f, 5f, 60f, 1f, MiraNumberSuffixes.Seconds)
        {
            Visible = () => OptionGroupSingleton<VultureOptions>.Instance.ScavengeEnabled,
        };

    public ModdedEnumOption OnLoseBecomes { get; } =
        new("ExtensionOptionVultureOnLoseBecomes", (int)BecomeOptions.Crew, typeof(BecomeOptions),
            ["CrewmateKeyword", "TouRoleAmnesiac", "TouRoleSurvivor", "TouRoleMercenary", "TouRoleJester"]);
}
