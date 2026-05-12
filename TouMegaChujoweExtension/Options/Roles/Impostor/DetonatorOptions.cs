using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Localization;
using System.Collections.Generic;

namespace TouMegaChujoweExtension.Options.Roles.Impostor;

public sealed class DetonatorOptions : AbstractOptionGroup<DetonatorRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleDetonator", "Detonator");

    [ModdedNumberOption("ExtensionOptionDetonatorManualDetonateDelay", 0f, 30f, 1f, MiraNumberSuffixes.Seconds)]
    public float ManualDetonateDelay { get; set; } = 10f;

    [ModdedNumberOption("ExtensionOptionDetonatorAttachDuration", 0.5f, 5f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float AttachDuration { get; set; } = 1.0f;

    [ModdedNumberOption("ExtensionOptionDetonatorDetonateRadius", 0.05f, 1f, 0.05f, MiraNumberSuffixes.Multiplier, "0.00")]
    public float DetonateRadius { get; set; } = 0.2f;

    [ModdedNumberOption("ExtensionOptionDetonatorMaxKills", min: 1, max: 15, increment: 1)]
    public float MaxKills { get; set; } = 3;

    [ModdedToggleOption("ExtensionOptionDetonatorCanVent")]
    public bool CanVent { get; set; } = true;
}
