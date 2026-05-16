using AmongUs.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using System;

namespace TouMegaChujoweExtension.Options.Roles.Neutral;

public sealed class JackalOptions : AbstractOptionGroup<JackalRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleJackal");

    [ModdedNumberOption("Kill Cooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float KillCooldown { get; set; } = 25f;

    [ModdedEnumOption("Kill Distance", null!, new[] { "Short", "Medium", "Long" })]
    public int KillDistance { get; set; } = 1; // 1 = Normal

    [ModdedToggleOption("Can Vent")]
    public bool CanVent { get; set; } = true;

    [ModdedToggleOption("Jackal Shield (while Sidekicks alive)")]
    public bool ShieldWhileSidekicksAlive { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionJackalNotifySidekickDeath")]
    public bool NotifySidekickDeath { get; set; } = true;
}
