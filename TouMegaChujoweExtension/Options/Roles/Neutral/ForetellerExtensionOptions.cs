using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class ForetellerExtensionOptions : AbstractOptionGroup
{
    public override string GroupName => TouLocale.Get("TOUMCEBetterRolePrefix") + TouLocale.Get("TouRoleDoomsayer", "Foreteller");
    public override Color GroupColor => TownOfUsColors.Doomsayer;
    public override bool ShowInModifiersMenu => false;
    public override MenuCategory ParentMenu => MenuCategory.CustomOne;
    public override uint GroupPriority => 103;

    [ModdedNumberOption("ExtensionOptionForetellerMaxHintRoles", 3f, 15f, 1f, MiraNumberSuffixes.None)]
    public float MaxHintRoles { get; set; } = 10f;
}
