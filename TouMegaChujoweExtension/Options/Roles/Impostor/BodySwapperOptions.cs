using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;
using TouMegaChujoweExtension.Roles.Classic.Impostor;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class BodySwapperOptions : AbstractOptionGroup<BodySwapperRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleBodySwapper", "Body Swapper");

    [ModdedNumberOption("Decoy Cooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float Cooldown { get; set; } = 30f;
}
