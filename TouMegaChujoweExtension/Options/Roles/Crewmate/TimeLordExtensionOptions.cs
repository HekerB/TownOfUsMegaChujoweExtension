using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TownOfUs;
using TownOfUs.Modules.Localization;
using TownOfUs.Roles.Crewmate;
using UnityEngine;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class TimeLordExtensionOptions : AbstractOptionGroup
{
    public override string GroupName => TouLocale.Get("TouRoleTimeLord", "Time Lord");
    public override Color GroupColor => TownOfUsColors.TimeLord;
    public override bool ShowInModifiersMenu => true;
    public override uint GroupPriority => 94;

    [ModdedNumberOption("ExtensionOptionTimeLordRewindSpeed", 0.5f, 5.0f, 0.5f, MiraNumberSuffixes.None)]
    public float RewindSpeed { get; set; } = 1.0f;
}
