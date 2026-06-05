using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.LocalSettings;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using System.Text;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class BerserkerRole(IntPtr cppPtr)
    : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable, IUnguessable
{
    public static bool PendingWarAnnouncement { get; set; }

    [HideFromIl2Cpp]
    public bool IsWar { get; set; }

    [HideFromIl2Cpp]
    public int KillCount { get; set; }

    [HideFromIl2Cpp]
    public float WarSpreeUntil { get; set; }

    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => IsWar ? "War" : "Berserker";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", IsWar ? "War" : "Berserker");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => [typeof(BerserkerKillButton)];

    public Color RoleColor => IsWar ? TouExtensionColors.War : TouExtensionColors.Berserker;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralKilling;
    public bool HasImpostorVision => IsWar;
    public RoleBehaviour AppearAs => RoleManager.Instance.GetRole((RoleTypes)RoleId.Get<BerserkerRole>());
    public bool IsGuessable => !IsWar;

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = IsWar && OptionGroupSingleton<BerserkerOptions>.Instance.WarCanVent,
        IntroSound = TouAudio.WarlockIntroSound,
        Icon = IsWar ? TouExtensionIcons.WarRoleIcon : TouExtensionIcons.BerserkerRoleIcon,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>(),
        OptionsScreenshot = TouBanners.NeutralRoleBanner,
    };

    public override void SpawnTaskHeader(PlayerControl playerControl)
    {
        if (playerControl != PlayerControl.LocalPlayer)
        {
            return;
        }

        var task = PlayerTask.GetOrCreateTask<ImportantTextTask>(playerControl, 0);
        task.Text = $"{TownOfUsColors.Neutral.ToTextColor()}{TouLocale.GetParsed("NeutralKillingTaskHeader")}</color>";
    }

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var builder = ITownOfUsRole.SetNewTabText(this);
        if (IsWar)
        {
            return builder;
        }

        var needed = (int)OptionGroupSingleton<BerserkerOptions>.Instance.KillsNeededToTransform;
        builder.AppendLine(TownOfUsPlugin.Culture,
            $"{TouExtensionColors.Berserker.ToTextColor()}{TouLocale.Get("ExtensionRoleBerserkerKillsTab", "Kills to become War")}: <b>{KillCount}/{needed}</b></color>");
        return builder;
    }

    public bool WinConditionMet()
    {
        if (Player == null || Player.HasDied())
        {
            return false;
        }

        var alivePlayers = Helpers.GetAlivePlayers();
        var berserkersAlive = alivePlayers.Count(x => x != null && x.IsRole<BerserkerRole>());

        if (MiscUtils.ImpAliveCount > 0)
        {
            return false;
        }

        if (MiscUtils.NKillersAliveCount > berserkersAlive)
        {
            return false;
        }

        var otherKillers = MiscUtils.KillersAliveCount - berserkersAlive;
        if (otherKillers > 0)
        {
            return false;
        }

        return alivePlayers.Count <= berserkersAlive * 2;
    }

    public void OffsetButtons()
    {
        var canVent = CanVentByState() || LocalSettingsTabSingleton<TownOfUsLocalSettings>.Instance.OffsetButtonsToggle.Value;
        var kill = MiraAPI.Hud.CustomButtonSingleton<BerserkerKillButton>.Instance;
        if (kill != null)
        {
            Reactor.Utilities.Coroutines.Start(MiscUtils.CoMoveButtonIndex(kill, !canVent));
        }
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        if (IsWar)
        {
            EnsureWarInvulnerability(player);
        }

        if (player.AmOwner)
        {
            OffsetButtons();
            RefreshVentButton();
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);

        if (targetPlayer.HasModifier<InvulnerabilityModifier>())
        {
            targetPlayer.RemoveModifier<InvulnerabilityModifier>();
        }

        if (targetPlayer.AmOwner)
        {
            HudManager.Instance.ImpostorVentButton.graphic.sprite = TouAssets.VentSprite.LoadAsset();
            HudManager.Instance.ImpostorVentButton.buttonLabelText.SetOutlineColor(TownOfUsColors.Impostor);
        }
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player))
        {
            return false;
        }

        var console = usable.TryCast<Console>();
        if (console != null && !console.AllowImpostor)
        {
            return false;
        }

        var vent = usable.TryCast<Vent>();
        if (vent != null && !CanVentByState())
        {
            return false;
        }

        return true;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        if (gameOverReason == MiraAPI.GameEnd.CustomGameOver.GameOverReason<GameOver.ExtensionNeutralGameOver>() &&
            TouMegaChujoweExtension.Patches.WinConditions.NeutralExtensionWinCondition.IsApocalypseAllianceWon)
        {
            return true;
        }

        return WinConditionMet();
    }

    public bool CanVentByState()
    {
        var options = OptionGroupSingleton<BerserkerOptions>.Instance;
        return IsWar && options.WarCanVent;
    }

    public bool ShouldShowVentButton()
    {
        return CanVentByState();
    }

    public float GetKillCooldown()
    {
        return GetKillCooldownForKills(KillCount);
    }

    public static float GetKillCooldownForKills(int killCount)
    {
        var options = OptionGroupSingleton<BerserkerOptions>.Instance;
        var needed = Math.Max(1, (int)options.KillsNeededToTransform);
        var maxReductionKills = Math.Max(0, needed - 1);
        var cappedKills = Math.Min(Math.Max(0, killCount), maxReductionKills);
        var cooldown = options.InitialKillCooldown - cappedKills * options.KillCooldownReduction;
        return Math.Clamp(cooldown, 5f, 120f);
    }

    public void OnSuccessfulKill()
    {
        if (IsWar)
        {
            return;
        }

        var nextKillCount = KillCount + 1;
        var needed = Math.Max(1, (int)OptionGroupSingleton<BerserkerOptions>.Instance.KillsNeededToTransform);
        if (nextKillCount >= needed)
        {
            RpcSetBerserkerKills(Player, needed);
            RpcTransformToWar(Player);
            return;
        }

        RpcSetBerserkerKills(Player, nextKillCount);
    }

    [MethodRpc((uint)ExtensionRpc.BerserkerSetKills, LocalHandling = Reactor.Networking.Rpc.RpcLocalHandling.Before)]
    public static void RpcSetBerserkerKills(PlayerControl player, int killCount)
    {
        if (player == null || player.Data?.Role is not BerserkerRole role)
        {
            return;
        }

        role.KillCount = Math.Max(0, killCount);
    }

    [MethodRpc((uint)ExtensionRpc.BerserkerTransformToWar, LocalHandling = Reactor.Networking.Rpc.RpcLocalHandling.Before)]
    public static void RpcTransformToWar(PlayerControl player)
    {
        if (player == null || player.HasDied() || player.Data?.Role is not BerserkerRole role)
        {
            return;
        }

        role.IsWar = true;
        role.WarSpreeUntil = 0f;
        role.KillCount = Math.Max(role.KillCount, (int)OptionGroupSingleton<BerserkerOptions>.Instance.KillsNeededToTransform);
        EnsureWarInvulnerability(player);

        if (OptionGroupSingleton<BerserkerOptions>.Instance.AnnounceWarTransformation)
        {
            PendingWarAnnouncement = true;
            ShowPendingWarAnnouncement();
        }

        if (player.AmOwner)
        {
            Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(Color.white, 0.15f, 0.35f));
            role.OffsetButtons();
            role.RefreshVentButton();
            MiraAPI.Hud.CustomButtonSingleton<BerserkerKillButton>.Instance?.RefreshWarState();
        }
    }

    public static void ShowPendingWarAnnouncement()
    {
        if (!PendingWarAnnouncement ||
            PlayerControl.LocalPlayer == null ||
            MeetingHud.Instance == null ||
            !OptionGroupSingleton<BerserkerOptions>.Instance.AnnounceWarTransformation)
        {
            return;
        }

        PendingWarAnnouncement = false;
        var msg = TouLocale.GetParsed("ExtensionRoleBerserkerWarAnnouncement", "War has consumed the battlefield.\\%nl\\%\\%color=#EEEEEEFF\\%War\\%/color\\%, Horseman of the Apocalypse, has emerged!");
        var title = $"<color=#{UnityEngine.ColorUtility.ToHtmlStringRGBA(TouExtensionColors.War)}>{TouLocale.Get("ExtensionRoleBerserkerWarAnnouncementTitle", "War Warning")}</color>";

        var notif = Helpers.CreateAndShowNotification(
            $"<b>{msg.Replace("\n", " ")}</b>",
            Color.white,
            new Vector3(0f, 1f, -20f),
            spr: TouExtensionIcons.WarRoleIcon.LoadAsset());
        notif?.AdjustNotification();

        MiscUtils.AddFakeChat(PlayerControl.LocalPlayer.Data, title, msg, false, true);
    }

    private static void EnsureWarInvulnerability(PlayerControl war)
    {
        if (war.HasModifier<InvulnerabilityModifier>())
        {
            war.RemoveModifier<InvulnerabilityModifier>();
        }

        war.AddModifier<InvulnerabilityModifier>(false, false, false);
    }

    private void RefreshVentButton()
    {
        if (Player == null || !Player.AmOwner || HudManager.Instance?.ImpostorVentButton == null)
        {
            return;
        }

        var ventButton = HudManager.Instance.ImpostorVentButton;
        ventButton.gameObject.SetActive(CanVentByState());
        ventButton.graphic.sprite = IsWar
            ? TouNeutAssets.WerewolfVentSprite.LoadAsset()
            : TouAssets.VentSprite.LoadAsset();
        ventButton.buttonLabelText.SetOutlineColor(RoleColor);
    }
}
