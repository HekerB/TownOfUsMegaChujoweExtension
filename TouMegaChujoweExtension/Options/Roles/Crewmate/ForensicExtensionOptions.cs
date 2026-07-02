using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;
using TownOfUs.Roles.Crewmate;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class ForensicExtensionOptions : AbstractOptionGroup
{
    public override string GroupName => TownOfUs.Modules.Localization.TouLocale.Get("TOUMCEBetterRolePrefix") + TownOfUs.Modules.Localization.TouLocale.Get("Forensic");
    public override Color GroupColor => TownOfUsColors.Forensic;
    public override MenuCategory ParentMenu => MenuCategory.CustomOne;
    public override uint GroupPriority => 102;

    [ModdedToggleOption("ExtensionOptionForensicFreezeOnMeeting")]
    public bool FreezeOnMeeting { get; set; } = true;
}











