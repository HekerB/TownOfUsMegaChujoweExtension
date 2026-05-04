using MiraAPI.GameOptions;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TownOfUs;
using TownOfUs.Modifiers.Game.Alliance;
using UnityEngine;

namespace TouMegaChujoweExtension.Options.Modifiers;

public sealed class EgotistExtendedOptions : AbstractOptionGroup<EgotistModifier>
{
    public override string GroupName => "Egotist";
    public override Color GroupColor => TownOfUsColors.Egotist;
    public override bool ShowInModifiersMenu => true;
    public override uint GroupPriority => 90;

    public ModdedToggleOption CanVent { get; set; } =
        new("ExtensionOptionEgotistCanVent", false);

    public ModdedNumberOption MaxVentTime { get; set; } =
        new("ExtensionOptionEgotistMaxVentTime", 10f, 1f, 30f, 1f, MiraNumberSuffixes.Seconds)
        {
            Visible = () => OptionGroupSingleton<EgotistExtendedOptions>.Instance.CanVent
        };

    public ModdedNumberOption VentCooldown { get; set; } =
        new("ExtensionOptionEgotistVentCooldown", 15f, 0f, 60f, 2.5f, MiraNumberSuffixes.Seconds)
        {
            Visible = () => OptionGroupSingleton<EgotistExtendedOptions>.Instance.CanVent
        };

    public ModdedToggleOption ImpostorVision { get; set; } =
        new("ExtensionOptionEgotistImpostorVision", false);
}
