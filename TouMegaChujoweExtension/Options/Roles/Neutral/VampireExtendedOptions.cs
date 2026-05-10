using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;
using TownOfUs.Roles.Neutral;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class VampireExtendedOptions : AbstractOptionGroup<VampireRole>
{
    public override string GroupName => TownOfUs.Modules.Localization.TouLocale.Get("TOUMCEBetterRolePrefix") + TouLocale.Get("ExtensionRoleVampire", "Vampire");
    public override Color GroupColor => TownOfUsColors.Vampire;
    public override bool ShowInModifiersMenu => false;
    public override MenuCategory ParentMenu => MenuCategory.CustomOne;
    public override uint GroupPriority => 111;

    [ModdedToggleOption("ExtensionOptionVampireCanOnlySabotageLights")]
    public bool CanOnlySabotageLights { get; set; } = false;




    [ModdedToggleOption("ExtensionOptionVampireOnlyOgCanSabotage")]
    public ModdedToggleOption OnlyOgCanSabotageOption { get; } = new("ExtensionOptionVampireOnlyOgCanSabotage", false)
    {
        Visible = () => OptionGroupSingleton<VampireExtendedOptions>.Instance.CanOnlySabotageLights
    };
    public bool OnlyOgCanSabotage => OnlyOgCanSabotageOption.Value;
}











