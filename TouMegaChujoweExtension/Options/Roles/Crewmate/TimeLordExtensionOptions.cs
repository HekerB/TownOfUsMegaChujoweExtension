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
    public override string GroupName => TownOfUs.Modules.Localization.TouLocale.Get("TOUMCEBetterRolePrefix") + TownOfUs.Modules.Localization.TouLocale.Get("Time Lord");
    public override Color GroupColor => TownOfUsColors.TimeLord;
    public override bool ShowInModifiersMenu => false;
    public override MenuCategory ParentMenu => MenuCategory.CustomOne;
    public override uint GroupPriority => 105;

    [ModdedNumberOption("ExtensionOptionTimeLordRewindSpeed", 0.5f, 5.0f, 0.5f, MiraNumberSuffixes.None)]
    public float RewindSpeed { get; set; } = 1.0f;
}
