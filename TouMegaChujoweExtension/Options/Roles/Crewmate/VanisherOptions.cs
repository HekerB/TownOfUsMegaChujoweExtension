using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class VanisherOptions : AbstractOptionGroup<VanisherRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleVanisher", "Vanisher");

    [ModdedNumberOption("ExtensionOptionVanisherDuration", 3f, 30f, 1f, MiraNumberSuffixes.Seconds)]
    public float VanishDuration { get; set; } = 10f;

    [ModdedNumberOption("ExtensionOptionVanisherCooldown", 5f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float VanishCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionVanisherMaxUses", 0f, 10f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float MaxVanishes { get; set; } = 0f;

    [ModdedToggleOption("ExtensionOptionVanisherDetectionEnabled")]
    public bool DetectionEnabled { get; set; } = true;

    public ModdedNumberOption DetectionRadius { get; } = new("ExtensionOptionVanisherRadius", 5f, 1f, 15f, 0.5f, MiraNumberSuffixes.None)
    {
        Visible = () => OptionGroupSingleton<VanisherOptions>.Instance.DetectionEnabled
    };

    public ModdedNumberOption NotificationCooldown { get; } = new("ExtensionOptionVanisherNotifCooldown", 5f, 1f, 15f, 1f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<VanisherOptions>.Instance.DetectionEnabled
    };
}
