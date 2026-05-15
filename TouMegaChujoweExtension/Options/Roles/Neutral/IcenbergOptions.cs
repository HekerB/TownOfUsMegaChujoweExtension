using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class IcenbergOptions : AbstractOptionGroup<IcenbergRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleIcenberg", "Icenberg");

    [ModdedNumberOption("ExtensionOptionIcenbergKillCooldown", 5f, 120f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float KillCooldown { get; set; } = 25f;

    [ModdedToggleOption("ExtensionOptionIcenbergCanVent")]
    public bool CanVent { get; set; } = false;

    [ModdedNumberOption("ExtensionOptionIcenbergFreezeCooldown", 5f, 90f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float FreezeCooldown { get; set; } = 25f;

    [ModdedNumberOption("ExtensionOptionIcenbergFreezeDuration", 1f, 30f, 1f, MiraNumberSuffixes.Seconds)]
    public float FreezeDuration { get; set; } = 8f;

    [ModdedNumberOption("ExtensionOptionIcenbergBlizzardCooldown", 10f, 120f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float BlizzardCooldown { get; set; } = 45f;

    [ModdedNumberOption("ExtensionOptionIcenbergBlizzardDuration", 1f, 20f, 1f, MiraNumberSuffixes.Seconds)]
    public float BlizzardDuration { get; set; } = 5f;

    [ModdedNumberOption("ExtensionOptionIcenbergFreezeUses", 0f, 15f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float FreezeUses { get; set; } = 0f;

    [ModdedNumberOption("ExtensionOptionIcenbergBlizzardUses", 0f, 10f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float BlizzardUses { get; set; } = 0f;
}
