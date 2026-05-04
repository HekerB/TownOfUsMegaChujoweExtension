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
    public override string GroupName => TouLocale.Get("TouRoleForensic", "Forensic");
    public override Color GroupColor => TownOfUsColors.Forensic;
    public override bool ShowInModifiersMenu => true;
    public override uint GroupPriority => 91;

    [ModdedToggleOption("ExtensionOptionForensicFreezeOnMeeting")]
    public bool FreezeOnMeeting { get; set; } = true;
}
