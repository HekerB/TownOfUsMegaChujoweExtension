using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Options;

public sealed class CrewmateModifierOptions : AbstractOptionGroup
{
    public override string GroupName => "Crewmate Modifiers";
    public override Color GroupColor => Palette.CrewmateRoleHeaderBlue;
    public override bool ShowInModifiersMenu => true;
    public override uint GroupPriority => 2;

    [ModdedNumberOption("Ventable Amount", 0, 5)]
    public float VentableAmount { get; set; } = 0;

    public ModdedNumberOption VentableChance { get; } =
        new("Ventable Chance", 50f, 0, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<CrewmateModifierOptions>.Instance.VentableAmount > 0
        };

    [ModdedNumberOption("Publicity Amount", 0, 1)]
    public float PublicityAmount { get; set; } = 0;

    public ModdedNumberOption PublicityChance { get; } =
        new("Publicity Chance", 50f, 0, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<CrewmateModifierOptions>.Instance.PublicityAmount > 0
        };
}