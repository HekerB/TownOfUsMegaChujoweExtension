using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMiraRolesExtension.Roles.Neutral;
using TownOfUs.Modules.Localization;
using TownOfUs.Options.Roles.Neutral;

namespace TouMiraRolesExtension.Options.Roles.Neutral;

public sealed class ScavengerOptions : AbstractOptionGroup<ScavengerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleScavenger", "Scavenger");

    [ModdedNumberOption("ExtensionOptionScavengerEatCooldown", 5f, 120f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float EatCooldown { get; set; } = 17.5f;

    [ModdedNumberOption("ExtensionOptionScavengerEatDuration", 0.5f, 10f, 0.1f, MiraNumberSuffixes.Seconds)]
    public float EatDuration { get; set; } = 1.5f;

    [ModdedNumberOption("ExtensionOptionScavengerBodiesToWin", 1f, 15f, 1f, MiraNumberSuffixes.None)]
    public float BodiesToWin { get; set; } = 3f;

    [ModdedToggleOption("ExtensionOptionScavengerCanVent")]
    public bool CanVent { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionScavengerScavengeEnabled")]
    public bool ScavengeEnabled { get; set; } = false;

    public ModdedNumberOption ScavengeCooldown { get; } =
        new("ExtensionOptionScavengerScavengeCooldown", 30f, 5f, 120f, 2.5f, MiraNumberSuffixes.Seconds)
        {
            Visible = () => OptionGroupSingleton<ScavengerOptions>.Instance.ScavengeEnabled,
        };

    public ModdedNumberOption ScavengeDuration { get; } =
        new("ExtensionOptionScavengerScavengeDuration", 5f, 5f, 60f, 1f, MiraNumberSuffixes.Seconds)
        {
            Visible = () => OptionGroupSingleton<ScavengerOptions>.Instance.ScavengeEnabled,
        };

    public ModdedEnumOption OnLoseBecomes { get; } =
        new("ExtensionOptionScavengerOnLoseBecomes", (int)BecomeOptions.Crew, typeof(BecomeOptions),
            ["CrewmateKeyword", "TouRoleAmnesiac", "TouRoleSurvivor", "TouRoleMercenary", "TouRoleJester"]);
}