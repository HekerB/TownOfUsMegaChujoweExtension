using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class LonerOptions : AbstractOptionGroup<LonerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleLoner", "Loner");

    [ModdedToggleOption("ExtensionOptionLonerChangeRoleAfterKills")]
    public bool ChangeRoleAfterKills { get; set; } = false;

    public ModdedNumberOption KillsNeededToChangeRole { get; } = new("ExtensionOptionLonerKillsNeededToChangeRole", 2f, 1f, 5f, 1f, MiraNumberSuffixes.None)
    {
        Visible = () => OptionGroupSingleton<LonerOptions>.Instance.ChangeRoleAfterKills
    };

    [ModdedToggleOption("ExtensionOptionLonerRecruitedImpostorBecomesAssassin")]
    public bool RecruitedImpostorBecomesAssassin { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionLonerRecruitBecomesTraitor")]
    public bool RecruitBecomesTraitor { get; set; } = false;

    [ModdedToggleOption("ExtensionOptionLonerRemoveExistingImpostorRoles")]
    public bool RemoveExistingImpostorRoles { get; set; } = true;
}
