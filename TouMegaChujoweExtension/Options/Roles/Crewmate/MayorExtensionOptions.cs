using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TownOfUs;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Options.Roles.Crewmate;

public sealed class MayorExtensionOptions : AbstractOptionGroup
{
    public override string GroupName => TownOfUs.Modules.Localization.TouLocale.Get("TOUMCEBetterRolePrefix") + TownOfUs.Modules.Localization.TouLocale.Get("Mayor");
    public override Color GroupColor => TownOfUsColors.Mayor;
    public override bool ShowInModifiersMenu => false;
    public override MenuCategory ParentMenu => MenuCategory.CustomOne;
    public override uint GroupPriority => 102;

    [ModdedNumberOption("ExtensionOptionMayorVoteCount", 3, 10, 1, MiraNumberSuffixes.None)]
    public float VoteCount { get; set; } = 3f;
}
