using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public enum EvokerBlindType
{
    Normal,
    ShowOnlySelf
}

public sealed class EvokerOptions : AbstractOptionGroup<EvokerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleEvoker", "Evoker");

    public ModdedNumberOption BlindDuration { get; } = new("ExtensionOptionEvokerBlindDuration", 10f, 5f, 30f, 1f, MiraNumberSuffixes.Seconds);
    public ModdedNumberOption BlindCooldown { get; } = new("ExtensionOptionEvokerBlindCooldown", 25f, 5f, 60f, 2.5f, MiraNumberSuffixes.Seconds);

    public ModdedEnumOption<EvokerBlindType> BlindType { get; } = new("ExtensionOptionEvokerBlindType", EvokerBlindType.Normal,
    [
        "ExtensionOptionEvokerBlindTypeEnumNormal",
        "ExtensionOptionEvokerBlindTypeEnumShowOnlySelf"
    ])
    {
    };

    public ModdedToggleOption CrewmateKillersBlinded { get; } = new("ExtensionOptionEvokerCrewKillersBlinded", false);

    public ModdedToggleOption CantVerify { get; } = new("ExtensionOptionEvokerCantVerify", false);

    public ModdedNumberOption VerifyCooldown { get; }
    public ModdedNumberOption MaxVerifications { get; }

    public EvokerOptions()
    {
        VerifyCooldown = new ModdedNumberOption("ExtensionOptionEvokerVerifyCooldown", 5f, 1f, 30f, 1f, MiraNumberSuffixes.Seconds)
        {
            Visible = () => !OptionGroupSingleton<EvokerOptions>.Instance.CantVerify.Value
        };

        MaxVerifications = new ModdedNumberOption("ExtensionOptionEvokerMaxVerifications", 3f, 0f, 20f, 1f, MiraNumberSuffixes.None, "0", true)
        {
            Visible = () => !OptionGroupSingleton<EvokerOptions>.Instance.CantVerify.Value
        };
    }
}














