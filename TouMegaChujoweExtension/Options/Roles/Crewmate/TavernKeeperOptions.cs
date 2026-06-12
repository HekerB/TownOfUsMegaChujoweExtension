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

    public ModdedNumberOption DrinkCooldown { get; } = new("ExtensionOptionTavernKeeperCooldown", 30f, 5f, 120f, 1f, MiraNumberSuffixes.Seconds);
    public ModdedNumberOption RoleblockDuration { get; } = new("ExtensionOptionTavernKeeperDuration", 10f, 1f, 30f, 1f, MiraNumberSuffixes.Seconds);
    public ModdedToggleOption InvertControlsOfRoleblocked { get; } = new("ExtensionOptionTavernKeeperInvertControls", false);
    public ModdedToggleOption Immunity { get; } = new("ExtensionOptionTavernKeeperImmunity", true);
    public ModdedNumberOption ImmunityDuration { get; }
    public ModdedToggleOption ShowAlertToTarget { get; } = new("ExtensionOptionTavernKeeperShowAlert", true);
    public ModdedNumberOption AlertDelay { get; }
    public ModdedNumberOption MaxUses { get; } = new("ExtensionOptionTavernKeeperMaxUses", 3f, 0f, 15f, 1f, MiraNumberSuffixes.None, "∞", true);
    public ModdedToggleOption ResetUsesAfterMeeting { get; } = new("ExtensionOptionTavernKeeperResetAfterMeeting", true);

    public TavernKeeperOptions()
    {
        ImmunityDuration = new ModdedNumberOption("ExtensionOptionTavernKeeperImmunityDuration", 10f, 1f, 60f, 1f, MiraNumberSuffixes.Seconds)
        {
            Visible = () => OptionGroupSingleton<TavernKeeperOptions>.Instance.Immunity.Value
        };
        AlertDelay = new ModdedNumberOption("ExtensionOptionTavernKeeperAlertDelay", 0f, 0f, 10f, 0.5f, MiraNumberSuffixes.Seconds)
        {
            Visible = () => OptionGroupSingleton<TavernKeeperOptions>.Instance.ShowAlertToTarget.Value
        };
    }
}

