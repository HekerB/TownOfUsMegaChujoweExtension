using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class TavernKeeperOptions : AbstractOptionGroup<TavernKeeperRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleTavernKeeper", "Tavern Keeper");

    [ModdedNumberOption("ExtensionOptionTavernKeeperCooldown", 5f, 120f, 1f, MiraNumberSuffixes.Seconds)]
    public float DrinkCooldown { get; set; } = 30f;

    [ModdedNumberOption("ExtensionOptionTavernKeeperDuration", 1f, 30f, 1f, MiraNumberSuffixes.Seconds)]
    public float RoleblockDuration { get; set; } = 10f;

    [ModdedToggleOption("ExtensionOptionTavernKeeperInvertControls")]
    public bool InvertControlsOfRoleblocked { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionTavernKeeperImmunity")]
    public bool Immunity { get; set; } = true;

    public ModdedNumberOption ImmunityDuration { get; } = new ModdedNumberOption("ExtensionOptionTavernKeeperImmunityDuration", 10f, 1f, 60f, 1f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<TavernKeeperOptions>.Instance.Immunity
    };

    [ModdedToggleOption("ExtensionOptionTavernKeeperShowAlert")]
    public bool ShowAlertToTarget { get; set; } = true;

    public ModdedNumberOption AlertDelay { get; } = new ModdedNumberOption("ExtensionOptionTavernKeeperAlertDelay", 0f, 0f, 10f, 0.5f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<TavernKeeperOptions>.Instance.ShowAlertToTarget
    };

    [ModdedNumberOption("ExtensionOptionTavernKeeperMaxUses", 0f, 15f, 1f, MiraNumberSuffixes.None, "∞", true)]
    public float MaxUses { get; set; } = 3f;

    [ModdedToggleOption("ExtensionOptionTavernKeeperResetAfterMeeting")]
    public bool ResetUsesAfterMeeting { get; set; } = true;
}
