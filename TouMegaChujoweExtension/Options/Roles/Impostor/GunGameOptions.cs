using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class GunGameOptions : AbstractOptionGroup<GunGameRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleGunGame", "Gun Game");
    public ModdedEnumOption GunGameGuess { get; } = new(
        "ExtensionOptionGunGameGuessAs",
        (int)CacheRoleGuess.ActiveOrCachedRole,
        typeof(CacheRoleGuess),
        [
            "ExtensionOptionGunGameGuessAsEnumGunGame",
            "ExtensionOptionGunGameGuessAsEnumCurrentRole",
            "ExtensionOptionGunGameGuessAsEnumGunGameOrCurrentRole"
        ]);
    [ModdedToggleOption("ExtensionOptionGunGameKeepRoleAfterMeeting")]
    public bool KeepRoleAfterMeeting { get; set; } = true;

    [ModdedNumberOption("ExtensionOptionGunGameKillsNeededToChangeRole", 1f, 5f, 1f, MiraNumberSuffixes.None)]
    public float KillsNeededToChangeRole { get; set; } = 1f;

    [ModdedNumberOption("ExtensionOptionGunGameMaxRememberedRoles", 0f, 15f, 1f, MiraNumberSuffixes.None, "0", true)]
    public float MaxRememberedRoles { get; set; } = 0f;

    [ModdedToggleOption("ExtensionOptionGunGameRemoveExistingImpostorRoles")]
    public bool RemoveExistingImpostorRoles { get; set; } = true;
}
