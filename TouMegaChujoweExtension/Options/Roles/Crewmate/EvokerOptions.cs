using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Crewmate;
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

    public ModdedNumberOption BlindCooldown { get; } = new("ExtensionOptionEvokerBlindCooldown", 25f, 5f, 60f, 2.5f, MiraNumberSuffixes.Seconds);

    public ModdedNumberOption BlindDuration { get; } = new("ExtensionOptionEvokerBlindDuration", 10f, 5f, 30f, 1f, MiraNumberSuffixes.Seconds);

    private static readonly string[] BlindTypeValues =
    {
        "ExtensionOptionEvokerBlindTypeEnumNormal",
        "ExtensionOptionEvokerBlindTypeEnumShowOnlySelf"
    };

    public ModdedEnumOption<EvokerBlindType> BlindType { get; } = new("ExtensionOptionEvokerBlindType", EvokerBlindType.Normal, BlindTypeValues);

    public ModdedToggleOption CrewmateKillersBlinded { get; } = new("ExtensionOptionEvokerCrewKillersBlinded", false);

    public ModdedToggleOption CantVerify { get; } = new("ExtensionOptionEvokerCantVerify", false);

    public ModdedNumberOption VerifyCooldown { get; }
    public ModdedNumberOption MaxVerifications { get; }

    public EvokerOptions()
    {
        VerifyCooldown = new ModdedNumberOption("ExtensionOptionEvokerVerifyCooldown", 5f, 1f, 30f, 1f, MiraNumberSuffixes.Seconds)
        {
            Visible = () => !CantVerify.Value
        };

        MaxVerifications = new ModdedNumberOption("ExtensionOptionEvokerMaxVerifications", 3f, 0f, 20f, 1f, MiraNumberSuffixes.None, "0", true)
        {
            Visible = () => !CantVerify.Value
        };
    }
}