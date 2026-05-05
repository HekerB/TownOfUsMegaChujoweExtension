using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TownOfUs;
using TownOfUs.Modules.Localization;
using TownOfUs.Roles.Crewmate;
using UnityEngine;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class ForensicExtensionOptions : AbstractOptionGroup
{
    public override string GroupName => TownOfUs.Modules.Localization.TouLocale.Get("TOUMCEBetterRolePrefix") + TownOfUs.Modules.Localization.TouLocale.Get("Forensic");
    public override Color GroupColor => TownOfUsColors.Forensic;
    public override bool ShowInModifiersMenu => false;
    public override MenuCategory ParentMenu => MenuCategory.CustomOne;
    public override uint GroupPriority => 101;

    [ModdedToggleOption("ExtensionOptionForensicFreezeOnMeeting")]
    public bool FreezeOnMeeting { get; set; } = true;
}
