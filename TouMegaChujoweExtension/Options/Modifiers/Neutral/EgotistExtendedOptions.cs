using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Options.Modifiers.Neutral;

public sealed class EgotistExtendedOptions : AbstractOptionGroup
{
    public override string GroupName => TownOfUs.Modules.Localization.TouLocale.Get("TOUMCEBetterModifierPrefix") + TownOfUs.Modules.Localization.TouLocale.Get("Egotist");
    public override Color GroupColor => TownOfUsColors.Egotist;
    public override MenuCategory ParentMenu => MenuCategory.CustomOne;
    public override uint GroupPriority => 100;

    [ModdedToggleOption("ExtensionOptionEgotistCanVent")]
    public bool CanVent { get; set; } = false;

    [ModdedNumberOption("ExtensionOptionEgotistMaxVentTime", 1f, 30f, 1f, MiraNumberSuffixes.Seconds)]
    public ModdedNumberOption MaxVentTimeOption { get; } = new("ExtensionOptionEgotistMaxVentTime", 10f, 1f, 30f, 1f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<EgotistExtendedOptions>.Instance.CanVent
    };
    public float MaxVentTime => MaxVentTimeOption.Value;

    [ModdedNumberOption("ExtensionOptionEgotistVentCooldown", 0f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public ModdedNumberOption VentCooldownOption { get; } = new("ExtensionOptionEgotistVentCooldown", 15f, 0f, 60f, 2.5f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<EgotistExtendedOptions>.Instance.CanVent
    };
    public float VentCooldown => VentCooldownOption.Value;

    [ModdedToggleOption("ExtensionOptionEgotistImpostorVision")]
    public bool ImpostorVision { get; set; } = false;
}











