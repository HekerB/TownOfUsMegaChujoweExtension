using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class VanisherOptions : AbstractOptionGroup<VanisherRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleVanisher", "Vanisher");

    [ModdedNumberOption("ExtensionOptionVanisherDuration", 3f, 30f, 1f, MiraNumberSuffixes.Seconds)]
    public float VanishDuration { get; set; } = 12f;

    [ModdedNumberOption("ExtensionOptionVanisherCooldown", 5f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float VanishCooldown { get; set; } = 27.5f;

    [ModdedNumberOption("ExtensionOptionVanisherMaxUses", 0f, 10f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float MaxVanishes { get; set; } = 0f;
}














