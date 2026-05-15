using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TownOfUs.Options;
using TownOfUs.Modules.Localization;
using UnityEngine;
using System;

namespace TouMegaChujoweExtension.Options.Modifiers;

public class SniperOptions : AbstractOptionGroup<SniperModifier>
{
    public override Func<bool> GroupVisible => () => OptionGroupSingleton<RoleOptions>.Instance.IsClassicRoleAssignment;
    public override string GroupName => TouLocale.Get("ExtensionModifierSniper", "Sniper");
    public override Color GroupColor => SniperModifier.SniperColor;
    public override uint GroupPriority => 50;

    [ModdedNumberOption("ExtensionModifierSniperKillDistanceMultiplier", 1.1f, 2.0f, 0.1f, MiraNumberSuffixes.Multiplier, "0.0")]
    public float KillDistanceMultiplier { get; set; } = 1.5f;
}
