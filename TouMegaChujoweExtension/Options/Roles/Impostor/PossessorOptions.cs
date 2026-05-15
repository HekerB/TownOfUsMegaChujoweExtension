using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;
using TouMegaChujoweExtension.Roles.Impostor;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class PossessorOptions : AbstractOptionGroup<PossessorRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRolePossessor", "Possessor");

    [ModdedToggleOption("ExtensionOptionPossessorEnabled")]
    public bool Enabled { get; set; } = true;

    [ModdedNumberOption("ExtensionOptionPossessorExtraShortTasks", 0f, 5f, 1f, MiraNumberSuffixes.None, "0")]
    public float ExtraShortTasks { get; set; } = 1f;

    [ModdedNumberOption("ExtensionOptionPossessorExtraLongTasks", 0f, 5f, 1f, MiraNumberSuffixes.None, "0")]
    public float ExtraLongTasks { get; set; } = 0f;

    [ModdedNumberOption("ExtensionOptionPossessorTasksLeftBeforeClickable", 0f, 15f, 1f, MiraNumberSuffixes.None, "0")]
    public float TasksLeftBeforeClickable { get; set; } = 3f;

    [ModdedToggleOption("ExtensionOptionPossessorAnnounceTasksComplete")]
    public bool AnnounceTasksComplete { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionPossessorAnnounceSuccessorChosen")]
    public bool AnnounceSuccessorChosen { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionPossessorRevealSuccessorIdentity")]
    public bool RevealSuccessorIdentity { get; set; } = false;
}
