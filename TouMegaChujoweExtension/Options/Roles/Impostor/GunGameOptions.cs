using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;
using TouMegaChujoweExtension.Roles.Classic.Impostor;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class GunGameOptions : AbstractOptionGroup<GunGameRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleGunGame", "Gun Game");

    [ModdedToggleOption("ExtensionOptionGunGameUseLethalChain")]
    public bool UseLethalChain { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionGunGameCanVent")]
    public bool CanVent { get; set; } = true;
}
