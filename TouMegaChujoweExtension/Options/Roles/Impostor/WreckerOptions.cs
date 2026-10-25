using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class WreckerOptions : AbstractRoleOptionGroup<WreckerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleWrecker", "Wrecker");

    [ModdedNumberOption("ExtensionOptionWreckerSmashCooldown", 10f, 90f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float SmashCooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionWreckerMaxSmashes", 0f, 15f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float MaxSmashes { get; set; } = 3f;

    [ModdedNumberOption("ExtensionOptionWreckerMaxWallThickness", 1f, 5f, 0.5f, MiraNumberSuffixes.None, "0.0")]
    public float MaxWallThickness { get; set; } = 3f;

    [ModdedToggleOption("ExtensionOptionWreckerSmashIsLoud")]
    public bool SmashIsLoud { get; set; } = true;
}
