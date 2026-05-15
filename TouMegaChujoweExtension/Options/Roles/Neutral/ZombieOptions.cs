using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class ZombieOptions : AbstractOptionGroup<ZombieRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleZombie", "Zombie");

    [ModdedNumberOption("ExtensionOptionZombieMeetingsUntilRot", 1f, 5f, 1f, MiraNumberSuffixes.None, "0")]
    public float MeetingsUntilDeath { get; set; } = 2f;

    [ModdedNumberOption("ExtensionOptionZombieChanceToConvert", 0f, 100f, 5f, MiraNumberSuffixes.Percent)]
    public float ChanceToConvert { get; set; } = 80f;

    [ModdedToggleOption("ExtensionOptionZombieCanVent")]
    public bool CanVent { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionZombiePrivateChat")]
    public bool PrivateChat { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionZombieRevealedToEachOther")]
    public bool RevealedToEachOther { get; set; } = true;
}
