using AmongUs.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using System;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class JackalOptions : AbstractRoleOptionGroup<JackalRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleJackal");

    [ModdedNumberOption("ExtensionOptionJackalKillCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float KillCooldown { get; set; } = 20f;




    [ModdedToggleOption("ExtensionOptionJackalCanVent")]
    public bool CanVent { get; set; } = true;

    [ModdedNumberOption("ExtensionOptionJackalShieldRounds", 0f, 5f, 1f, MiraNumberSuffixes.None)]
    public float ShieldRounds { get; set; } = 3f;

    [ModdedToggleOption("ExtensionOptionJackalNotifySidekickDeath")]
    public bool NotifySidekickDeath { get; set; } = true;

    /// <summary>The shield only holds for the first <see cref="ShieldRounds"/> rounds; 0 turns it off entirely.</summary>
    public bool IsShieldActive(int round)
    {
        var maxRounds = (int)ShieldRounds;
        return maxRounds > 0 && round <= maxRounds;
    }
}