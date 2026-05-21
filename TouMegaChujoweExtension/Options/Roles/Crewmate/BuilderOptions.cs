using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class BuilderOptions : AbstractOptionGroup<BuilderRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleBuilder", "Builder");

    [ModdedNumberOption("ExtensionOptionBuilderCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float Cooldown { get; set; } = 20f;

    [ModdedNumberOption("ExtensionOptionBuilderDuration", 5f, 60f, 5f, MiraNumberSuffixes.Seconds)]
    public float BuildDuration { get; set; } = 15f;

    [ModdedNumberOption("ExtensionOptionBuilderMaxStructures", 1f, 5f, 1f, MiraNumberSuffixes.None)]
    public float MaxStructures { get; set; } = 3f;
}
