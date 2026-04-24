using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Neutral;

public sealed class DoppelgangerRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => "Doppelganger";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Doppelganger;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralKilling;

    public bool HasImpostorVision => true;

    public int RemainingIdentityThefts { get; set; }

    public bool HasInfiniteSteals => (int)OptionGroupSingleton<DoppelgangerOptions>.Instance.MaxSteals == 0;

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = OptionGroupSingleton<DoppelgangerOptions>.Instance.CanVent,
        IntroSound = TouAudio.GlitchSound,
        Icon = TouExtensionIcons.DoppelgangerRoleIcon,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>()
    };

    public bool WinConditionMet()
    {
        if (Player.HasDied()) return false;

        var aliveCount = Helpers.GetAlivePlayers().Count;
        var killersAlive = MiscUtils.KillersAliveCount;

        // Win only when we're the SOLE killer and <= 2 players remain
        return aliveCount <= 2 && killersAlive == 1;
    }

    public override void SpawnTaskHeader(PlayerControl playerControl)
    {
        if (playerControl != PlayerControl.LocalPlayer) return;
        ImportantTextTask orCreateTask = PlayerTask.GetOrCreateTask<ImportantTextTask>(playerControl, 0);
        orCreateTask.Text = $"{TownOfUsColors.Neutral.ToTextColor()}{TouLocale.GetParsed("NeutralKillingTaskHeader")}</color>";
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        var maxSteals = (int)OptionGroupSingleton<DoppelgangerOptions>.Instance.MaxSteals;
        RemainingIdentityThefts = maxSteals == 0 ? -1 : maxSteals;
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player)) return false;
        var console = usable.TryCast<Console>()!;
        return console == null || console.AllowImpostor;
    }

    public override bool DidWin(GameOverReason gameOverReason) => WinConditionMet();
}
