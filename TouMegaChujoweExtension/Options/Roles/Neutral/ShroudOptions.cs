using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class ShroudOptions : AbstractOptionGroup<ShroudRole>
{
    public override string GroupName => "Shroud";

    [ModdedNumberOption("Kill Cooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float KillCooldown { get; set; } = 25f;

    [ModdedNumberOption("Shroud Cooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float ShroudCooldown { get; set; } = 30f;

    [ModdedToggleOption("Can Vent")]
    public bool CanVent { get; set; } = false;


}














