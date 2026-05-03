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
    public override string GroupName => TouLocale.Get("TouRoleMayor", "Mayor");
    public override Color GroupColor => TownOfUsColors.Mayor;
    public override bool ShowInModifiersMenu => true;
    public override uint GroupPriority => 92;

    [ModdedNumberOption("ExtensionOptionMayorVoteCount", 3, 10, 1, MiraNumberSuffixes.None)]
    public float VoteCount { get; set; } = 3f;
}
