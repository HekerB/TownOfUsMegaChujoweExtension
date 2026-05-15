using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class ZapperOptions : AbstractOptionGroup<ZapperRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleZapper", "Zapper");

    [ModdedNumberOption("ExtensionOptionZapperRadius", 0.05f, 1f, 0.05f, MiraNumberSuffixes.Multiplier, "0.00")]
    public float Radius { get; set; } = 0.3f;

    [ModdedNumberOption("ExtensionOptionZapperMaxJumps", 1f, 10f, 1f)]
    public float MaxJumps { get; set; } = 3f;

    [ModdedNumberOption("ExtensionOptionZapperZapCooldown", 5f, 120f, 5f, MiraNumberSuffixes.Seconds)]
    public float ZapCooldown { get; set; } = 30f;

    [ModdedToggleOption("ExtensionOptionZapperCanVent")]
    public bool CanVent { get; set; } = true;
}
