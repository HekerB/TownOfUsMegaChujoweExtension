using System.Text;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;
using TouMegaChujoweExtension.Assets;

namespace TouMegaChujoweExtension.Roles.Neutral;

public sealed class SerialKillerRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public int KillCount { get; set; }
    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => "SerialKiller";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.SerialKiller;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralKilling;

    public bool HasImpostorVision => true;

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = true,
        IntroSound = TouAudio.WarlockIntroSound,
        Icon = TouRoleIcons.SerialKiller,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>()
    };
    
    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var stringB = ITownOfUsRole.SetNewTabText(this);
        var killsText = TouLocale.GetParsed("ExtensionRoleSerialKillerTabKillCounter", "Kills: <count>");
        stringB.Append(TownOfUsPlugin.Culture, $"\n<b>{killsText.Replace("<count>", $"{KillCount}")}</b>");

        return stringB;
    }

    public bool WinConditionMet()
    {
        if (Player.HasDied())
        {
            return false;
        }

        var aliveCount = Helpers.GetAlivePlayers().Count;
        var killersAlive = MiscUtils.KillersAliveCount;

        return aliveCount <= killersAlive && killersAlive == 1;
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);

        if (!OptionGroupSingleton<SerialKillerOptions>.Instance.CanReportBodies && !Player.HasModifier<SerialKillerNoReportModifier>())
        {
            Player.AddModifier<SerialKillerNoReportModifier>();
        }

        var options = OptionGroupSingleton<SerialKillerOptions>.Instance;
        if (options.ManiacMode)
        {
            Player.AddModifier<SerialKillerManiacModifier>(options.ManiacTimer.Value, options.ManiacCooldown.Value);
        }
        UpdateReductionModifier();
    }

    public void UpdateReductionModifier()
    {
        var options = OptionGroupSingleton<SerialKillerOptions>.Instance;
        if (!options.KillCooldownReductionEnabled || KillCount <= 0)
        {
            if (Player.HasModifier<SerialKillerReductionModifier>())
            {
                Player.RemoveModifier<SerialKillerReductionModifier>();
            }
            return;
        }

        var reduction = options.KillCooldownReductionPerKill.Value * KillCount;
        if (Player.TryGetModifier<SerialKillerReductionModifier>(out var mod))
        {
            mod.ReductionAmount = reduction;
        }
        else
        {
            Player.AddModifier<SerialKillerReductionModifier>(reduction);
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player))
        {
            return false;
        }

        var console = usable.TryCast<Console>()!;
        return console == null || console.AllowImpostor;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        return WinConditionMet();
    }
}