using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs;
using TownOfUs.Assets;
using TownOfUs.Events;
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

public sealed class SoulCollectorRole(IntPtr cppPtr)
    : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public static bool PendingDeathAnnouncement { get; set; }
    public static bool DeathAnnounced { get; set; }

    public DoomableType DoomHintType => DoomableType.Death;
    public string LocaleKey => "SoulCollector";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Soul Collector");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public int SoulsCollected { get; set; }

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => [typeof(SoulCollectorReapButton)];

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.Get("ExtensionRoleSoulCollectorReap", "Reap"),
            TouLocale.Get("ExtensionRoleSoulCollectorReapWikiDescription", "Mark a player. If they die before the mark expires, gain one soul."),
            TouNeutAssets.ReapSprite)
    ];

    public Color RoleColor => TouExtensionColors.SoulCollector;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralKilling;
    public bool HasImpostorVision => false;

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = false,
        IntroSound = TouAudio.PhantomIntroSound,
        Icon = TouExtensionIcons.SoulCollectorRoleIcon,
        OptionsScreenshot = TouBanners.NeutralRoleBanner,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>()
    };

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var stringBuilder = ITownOfUsRole.SetNewTabText(this);
        var options = OptionGroupSingleton<SoulCollectorOptions>.Instance;
        var roleColor = TouExtensionColors.SoulCollector.ToTextColor();
        stringBuilder.AppendLine(string.Format(
            CultureInfo.InvariantCulture,
            "{0}<b>Souls:</b></color> {1} / {2}",
            roleColor,
            SoulsCollected,
            (int)options.SoulGoal));

        var activeTargets = GetActiveMarkedPlayers(Player.PlayerId);
        stringBuilder.AppendLine(string.Format(
            CultureInfo.InvariantCulture,
            "{0}<b>Reaped Targets:</b></color> {1} / {2}",
            roleColor,
            activeTargets.Count,
            (int)options.MaxMarks));

        if (activeTargets.Count > 0)
        {
            var targetNames = string.Join(", ", activeTargets.Select(x => x.Data.PlayerName));
            stringBuilder.AppendLine($"{roleColor}<b>Marked:</b></color> {targetNames}");
        }

        return stringBuilder;
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        SoulsCollected = 0;
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    public bool WinConditionMet() => false;

    public override bool DidWin(GameOverReason gameOverReason)
    {
        return gameOverReason == MiraAPI.GameEnd.CustomGameOver.GameOverReason<GameOver.ExtensionNeutralGameOver>() &&
               TouMegaChujoweExtension.Patches.WinConditions.NeutralExtensionWinCondition.IsApocalypseAllianceWon;
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player))
        {
            return false;
        }

        var console = usable.TryCast<Console>();
        return console == null || console.AllowImpostor;
    }

    public static int GetActiveMarkCount(byte soulCollectorId)
    {
        return GetActiveMarkedPlayers(soulCollectorId).Count;
    }

    private static List<PlayerControl> GetActiveMarkedPlayers(byte soulCollectorId)
    {
        return PlayerControl.AllPlayerControls.ToArray()
            .Where(player => player != null &&
                             !player.HasDied() &&
                             player.TryGetModifier<SoulReapedModifier>(out var mod) &&
                             mod.SoulCollectorId == soulCollectorId &&
                             !mod.IsExpired())
            .ToList();
    }

    [MethodRpc((uint)ExtensionRpc.SoulCollectorReap)]
    public static void RpcReapTarget(PlayerControl soulCollector, PlayerControl target)
    {
        if (soulCollector == null ||
            target == null ||
            target.HasDied() ||
            target == soulCollector ||
            soulCollector.Data?.Role is not SoulCollectorRole)
        {
            return;
        }

        if (target.HasModifier<SoulReapedModifier>())
        {
            target.RemoveModifier<SoulReapedModifier>();
        }

        var options = OptionGroupSingleton<SoulCollectorOptions>.Instance;
        if (GetActiveMarkCount(soulCollector.PlayerId) >= (int)options.MaxMarks)
        {
            return;
        }

        target.AddModifier<SoulReapedModifier>(
            soulCollector.PlayerId,
            DeathEventHandlers.CurrentRound,
            (int)options.ReapDurationRounds);
    }

    [MethodRpc((uint)ExtensionRpc.SoulCollectorSetSouls, LocalHandling = Reactor.Networking.Rpc.RpcLocalHandling.Before)]
    public static void RpcSetSouls(PlayerControl soulCollector, int souls)
    {
        if (soulCollector == null || soulCollector.Data?.Role is not SoulCollectorRole role)
        {
            return;
        }

        role.SoulsCollected = Math.Max(0, souls);
    }

    [MethodRpc((uint)ExtensionRpc.SoulCollectorTransformToDeath, LocalHandling = Reactor.Networking.Rpc.RpcLocalHandling.Before)]
    public static void RpcTransformToDeath(PlayerControl soulCollector)
    {
        if (soulCollector == null || soulCollector.HasDied())
        {
            return;
        }

        if (soulCollector.Data?.Role is not DeathRole)
        {
            soulCollector.ChangeRole(RoleId.Get<DeathRole>());
        }

        if (OptionGroupSingleton<SoulCollectorOptions>.Instance.AnnounceDeath)
        {
            PendingDeathAnnouncement = true;
            ShowPendingDeathAnnouncement();
        }
    }

    public static void ShowPendingDeathAnnouncement()
    {
        if (!PendingDeathAnnouncement ||
            DeathAnnounced ||
            PlayerControl.LocalPlayer == null ||
            MeetingHud.Instance == null ||
            !OptionGroupSingleton<SoulCollectorOptions>.Instance.AnnounceDeath)
        {
            return;
        }

        PendingDeathAnnouncement = false;
        DeathAnnounced = true;
        var msg = TouLocale.GetParsed("ExtensionRoleSoulCollectorDeathAnnouncement", "The final soul has been claimed.\\%nl\\%\\%color=#202020FF\\%Death\\%/color\\%, Horseman of the Apocalypse, has emerged!");
        var title = $"<color=#{UnityEngine.ColorUtility.ToHtmlStringRGBA(TownOfUsColors.SoulCollector)}>{TouLocale.Get("ExtensionRoleSoulCollectorDeathAnnouncementTitle", "Death Warning")}</color>";

        var notif = Helpers.CreateAndShowNotification(
            $"<b>{msg.Replace("\n", " ")}</b>",
            Color.white,
            new Vector3(0f, 1f, -20f),
            spr: TouExtensionIcons.SoulCollectorRoleIcon.LoadAsset());
        notif?.AdjustNotification();

        MiscUtils.AddFakeChat(PlayerControl.LocalPlayer.Data, title, msg, false, true);
    }
}
