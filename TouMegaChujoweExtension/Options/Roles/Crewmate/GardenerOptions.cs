using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class GardenerOptions : AbstractOptionGroup<GardenerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleGardener", "Gardener");

    [ModdedNumberOption("ExtensionOptionGardenerCooldown", 1f, 30f, 1f, MiraNumberSuffixes.Seconds)]
    public float TrapCooldown { get; set; } = 20f;

    [ModdedNumberOption("ExtensionOptionGardenerMaxGardens", 1f, 15f, 1f, MiraNumberSuffixes.None, "0")]
    public float MaxTraps { get; set; } = 5f;

    [ModdedNumberOption("ExtensionOptionGardenerTrapSize", 0.05f, 1f, 0.05f, MiraNumberSuffixes.Multiplier, "0.00")]
    public float TrapSize { get; set; } = 0.25f;

    [ModdedToggleOption("ExtensionOptionGardenerGardensRemovedAfterRound")]
    public bool TrapsRemoveOnNewRound { get; set; } = true;

    public ModdedToggleOption TaskUses { get; } = new("ExtensionOptionGardenerGetUsesFromTasks", false)
    {
        Visible = () => !OptionGroupSingleton<GardenerOptions>.Instance.TrapsRemoveOnNewRound
    };

    [ModdedToggleOption("ExtensionOptionGardenerFeedback")]
    public bool Feedback { get; set; } = true;
}