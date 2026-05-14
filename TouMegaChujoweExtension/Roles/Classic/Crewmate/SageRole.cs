using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Utilities;
using System.Text;
using TownOfUs.Assets;
using TownOfUs.Events;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Crewmate;

public sealed class SageRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public override bool IsAffectedByComms => false;
    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => "Sage";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public List<string> ComparisonList = new();

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") +
               MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed("ExtensionRoleSageGaze", "Gaze"),
            TouLocale.GetParsed("ExtensionRoleSageGazeWikiDescription"),
            TouCrewAssets.GazeSprite),
        new(
            TouLocale.GetParsed("ExtensionRoleSageIntuit", "Intuit"),
            TouLocale.GetParsed("ExtensionRoleSageIntuitWikiDescription"),
            TouCrewAssets.IntuitSprite)
    ];

    public Color RoleColor => TouExtensionColors.Sage;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateInvestigative;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouExtensionIcons.SageRoleIcon,
        IntroSound = TouAudio.QuestionSound,
        OptionsScreenshot = TouBanners.SeerRoleBanner,
    };

    [HideFromIl2Cpp] public PlayerControl? GazeTarget { get; set; }
    [HideFromIl2Cpp] public PlayerControl? IntuitTarget { get; set; }

    public override void Initialize(PlayerControl player)
    {
        GazeTarget = null;
        IntuitTarget = null;
        RoleBehaviourStubs.Initialize(this, player);
        ComparisonList = new List<string>();
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    public override void OnMeetingStart()
    {
        RoleBehaviourStubs.OnMeetingStart(this);

        if (Player.AmOwner)
        {
            var gazeButton = CustomButtonSingleton<SageGazeButton>.Instance;
            gazeButton.ResetCooldownAndOrEffect();
            var intuitButton = CustomButtonSingleton<SageIntuitButton>.Instance;
            intuitButton.ResetCooldownAndOrEffect();

            if (IntuitTarget != null)
            {
                ++intuitButton.UsesLeft;
                intuitButton.SetUses(intuitButton.UsesLeft);
                IntuitTarget = null;
            }

            if (GazeTarget != null)
            {
                ++gazeButton.UsesLeft;
                gazeButton.SetUses(gazeButton.UsesLeft);
                GazeTarget = null;
            }
        }
    }

    public void SageCompare(PlayerControl sage)
    {
        if (GazeTarget == null || IntuitTarget == null)
        {
            Coroutines.Start(MiscUtils.CoFlash(Color.red));
            ShowNotification($"<b>You need both a Gaze and Intuit target!</b>");
            return;
        }

        if (GazeTarget == sage || IntuitTarget == sage)
        {
            Coroutines.Start(MiscUtils.CoFlash(Color.red));
            ShowNotification($"<b>You cannot compare yourself!</b>");
            return;
        }

        var gazeButton = CustomButtonSingleton<SageGazeButton>.Instance;
        gazeButton.ResetCooldownAndOrEffect();
        var intuitButton = CustomButtonSingleton<SageIntuitButton>.Instance;
        intuitButton.ResetCooldownAndOrEffect();

        var playerA = GazeTarget.CachedPlayerData.PlayerName;
        var playerB = IntuitTarget.CachedPlayerData.PlayerName;
        var players = new[] { playerA, playerB }.OrderBy(x => x.ToLowerInvariant()).ToArray();

        bool enemies = AreEnemies(GazeTarget, IntuitTarget);

        if (enemies)
        {
            Coroutines.Start(MiscUtils.CoFlash(TownOfUsColors.ImpSoft));
            ShowNotification(
                $"<b>{TownOfUsColors.ImpSoft.ToTextColor()}You sense that {players[0]} and {players[1]} are enemies!</color></b>");
            ComparisonList.Add(
                $"<b>{TownOfUsColors.ImpSoft.ToTextColor()}[R{DeathEventHandlers.CurrentRound}] {players[0]} & {players[1]} - Enemies</color></b>");
        }
        else
        {
            Coroutines.Start(MiscUtils.CoFlash(Palette.CrewmateBlue));
            ShowNotification(
                $"<b>{Palette.CrewmateBlue.ToTextColor()}You sense that {players[0]} and {players[1]} are friends!</color></b>");
            ComparisonList.Add(
                $"<b>{Palette.CrewmateBlue.ToTextColor()}[R{DeathEventHandlers.CurrentRound}] {players[0]} & {players[1]} - Friends</color></b>");
        }

        IntuitTarget = null;
        GazeTarget = null;
    }

    private bool AreEnemies(PlayerControl p1, PlayerControl p2)
    {
        if (p1?.Data?.Role == null || p2?.Data?.Role == null) return false;

        var opts = OptionGroupSingleton<SageOptions>.Instance;

        if (p1.IsCrewmate() && p2.IsCrewmate()) return false;
        if (p1.IsImpostor() && p2.IsImpostor()) return false;
        if (p1.Data.Role.Role == p2.Data.Role.Role) return false;
        if (p1.Is(RoleAlignment.NeutralBenign) && p2.Is(RoleAlignment.NeutralBenign)) return false;
        if (p1.Is(RoleAlignment.NeutralEvil) && p2.Is(RoleAlignment.NeutralEvil)) return false;
        if (p1.Is(RoleAlignment.NeutralOutlier) && p2.Is(RoleAlignment.NeutralOutlier)) return false;

        if (p1.Is(RoleAlignment.NeutralBenign) || p2.Is(RoleAlignment.NeutralBenign))
            return !opts.BenignShowFriendly;
        if (p1.Is(RoleAlignment.NeutralEvil) || p2.Is(RoleAlignment.NeutralEvil))
            return !opts.EvilShowFriendly;
        if (p1.Is(RoleAlignment.NeutralOutlier) || p2.Is(RoleAlignment.NeutralOutlier))
            return !opts.OutlierShowFriendly;

        return true;
    }

    private void ShowNotification(string message)
    {
        var notif = Helpers.CreateAndShowNotification(message, Color.white, new Vector3(0f, 1f, -20f),
            spr: TouExtensionIcons.SageRoleIcon.LoadAsset());
        notif.AdjustNotification();
    }

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var stringB = ITownOfUsRole.SetNewTabText(this);
        if (ComparisonList.Count != 0)
        {
            stringB.AppendLine($"\n<b>Comparisons:</b>");
            foreach (var comparison in ComparisonList)
            {
                stringB.AppendLine($"<b><size=70%>{comparison}</size></b>");
            }
        }

        return stringB;
    }
}















