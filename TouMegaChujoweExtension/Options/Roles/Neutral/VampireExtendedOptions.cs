using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TownOfUs;
using TownOfUs.Roles.Neutral;
using TownOfUs.Modules.Localization;
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

    [ModdedNumberOption("ExtensionOptionVampireSabotageCooldown", 0f, 120f, 5f, MiraNumberSuffixes.Seconds)]
    public ModdedNumberOption SabotageCooldownOption { get; } = new("ExtensionOptionVampireSabotageCooldown", 30f, 0f, 120f, 5f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<VampireExtendedOptions>.Instance.CanOnlySabotageLights
    };
    public float SabotageCooldown => SabotageCooldownOption.Value;

    [ModdedToggleOption("ExtensionOptionVampireOnlyOgCanSabotage")]
    public ModdedToggleOption OnlyOgCanSabotageOption { get; } = new("ExtensionOptionVampireOnlyOgCanSabotage", false)
    {
        Visible = () => OptionGroupSingleton<VampireExtendedOptions>.Instance.CanOnlySabotageLights
    };
    public bool OnlyOgCanSabotage => OnlyOgCanSabotageOption.Value;
}
