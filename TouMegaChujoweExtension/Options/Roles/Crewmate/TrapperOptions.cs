using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class TrapperOptions : AbstractOptionGroup<TrapperRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleTrapper", "Trapper");

    [ModdedNumberOption("ExtensionOptionTrapperTrapCooldown", 1f, 60f, 1f, MiraNumberSuffixes.Seconds)]
    public float TrapCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionTrapperMaxTraps", 1f, 15f, 1f, MiraNumberSuffixes.None, "0")]
    public float MaxTraps { get; set; } = 3f;

    [ModdedToggleOption("ExtensionOptionTrapperGetMoreFromTasks")]
    public bool GetMoreFromTasks { get; set; } = true;

    public ModdedNumberOption TasksUntilMoreTrapsOption { get; } = new("ExtensionOptionTrapperTasksUntilMoreTraps", 2f, 1f, 10f, 1f, MiraNumberSuffixes.None, "0")
    {
        Visible = () => OptionGroupSingleton<TrapperOptions>.Instance.GetMoreFromTasks
    };

    [ModdedEnumOption("ExtensionOptionTrapperTrapTargets", typeof(VentTrapTargets),
        ["ExtensionOptionTrapperTrapTargetsEnumImpostors", "ExtensionOptionTrapperTrapTargetsEnumImpostorsAndNeutrals", "ExtensionOptionTrapperTrapTargetsEnumAll"])]
    public VentTrapTargets TrapTargets { get; set; } = VentTrapTargets.ImpostorsAndNeutrals;

    [ModdedEnumOption("ExtensionOptionTrapperArrowTarget", typeof(TrapperArrowTarget),
        ["ExtensionOptionTrapperArrowTargetEnumVent", "ExtensionOptionTrapperArrowTargetEnumPerson"])]
    public TrapperArrowTarget ArrowTarget { get; set; } = TrapperArrowTarget.Vent;

    [ModdedNumberOption("ExtensionOptionTrapperTrappeduration", 0.5f, 15f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float Trappeduration { get; set; } = 4f;

    [ModdedNumberOption("ExtensionOptionTrapperArrowDuration", 0.5f, 15f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float ArrowDuration { get; set; } = 4.5f;

    [ModdedNumberOption("ExtensionOptionTrapperTrapRoundsLast", 0f, 15f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float TrapRoundsLast { get; set; } = 3f;

    // Backward compatibility for TrapperRole
    public float TasksUntilMoreTraps => TasksUntilMoreTrapsOption.Value;
}

public enum VentTrapTargets
{
    Impostors,
    ImpostorsAndNeutrals,
    All
}

public enum TrapperArrowTarget
{
    Vent,
    Person
}
