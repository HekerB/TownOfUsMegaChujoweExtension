using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public enum SonarDisplayMode
{
    ArrowAndMap = 0,
    MapOnly = 1
}

public sealed class SonarExtendedOptions : AbstractOptionGroup
{
    public override string GroupName => TownOfUs.Modules.Localization.TouLocale.Get("TOUMCEBetterRolePrefix") + TownOfUs.Modules.Localization.TouLocale.Get("Sonar");
    public override Color GroupColor => TownOfUsColors.Tracker;
    public override MenuCategory ParentMenu => MenuCategory.CustomOne;
    public override uint GroupPriority => 106;

    [ModdedToggleOption("ExtensionOptionBetterSonar")]
    public bool BetterSonar { get; set; } = false;

    public ModdedEnumOption ModeOption { get; } = new(
        "ExtensionOptionBetterSonarMode",
        (int)SonarDisplayMode.ArrowAndMap,
        typeof(SonarDisplayMode),
        [
            "ExtensionOptionBetterSonarModeArrowAndMap",
            "ExtensionOptionBetterSonarModeMapOnly"
        ]
    )
    {
        Visible = () => OptionGroupSingleton<SonarExtendedOptions>.Instance.BetterSonar
    };

    public SonarDisplayMode Mode => (SonarDisplayMode)ModeOption.Value;
}
