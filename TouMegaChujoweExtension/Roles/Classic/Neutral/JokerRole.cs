using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
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

public sealed class JokerRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable, IContinuesGame
{
    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => "Joker";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Joker");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public bool MetWinCon { get; set; }
    public bool ContinuesGame => !Player.HasDied() &&
                                 OptionGroupSingleton<JokerOptions>.Instance.WinMode == JokerWinOptions.WinWithWinners &&
                                 MetWinCon;

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => [typeof(JokerPlaceCloneButton)];

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(TouLocale.GetParsed("ExtensionRoleJokerPlaceCloneWiki", "Place Clone"),
            TouLocale.GetParsed("ExtensionRoleJokerPlaceCloneWikiDescription"),
            TouExtensionNeuAssets.JokerCloneButtonSprite)
    ];

    public Color RoleColor => TouExtensionColors.Joker;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralEvil;
    public bool HasImpostorVision => false;

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = false,
        Icon = TouExtensionIcons.JokerRoleIcon,
        IntroSound = TouAudio.NoisemakerIntroSound,
        OptionsScreenshot = TouExtensionBanners.MirageBanner,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>()
    };

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var stringBuilder = ITownOfUsRole.SetNewTabText(this);
        var killsNeeded = (int)OptionGroupSingleton<JokerOptions>.Instance.KillsToWin;
        var currentKills = JokerCloneSystem.KilledCloneCount;

        stringBuilder.AppendLine(string.Format(
            CultureInfo.InvariantCulture,
            TouLocale.Get("ExtensionRoleJokerTabClonesKilled", "Clones Killed: {0} / {1}"),
            currentKills,
            killsNeeded));

        var cloneLocations = GetCloneLocationText(Player.PlayerId);
        if (!string.IsNullOrWhiteSpace(cloneLocations))
        {
            stringBuilder.AppendLine(TouLocale.Get("ExtensionRoleJokerTabCloneLocations", "Clone Locations: {0}")
                .Replace("{0}", cloneLocations));
        }

        if (MetWinCon)
        {
            stringBuilder.AppendLine(TouLocale.Get("ExtensionRoleJokerTabObjectiveComplete", "<b>Objective Complete!</b>"));
        }

        return stringBuilder;
    }

    private static string GetCloneLocationText(byte jokerId)
    {
        var rooms = JokerCloneSystem.Clones
            .Where(clone => clone.JokerId == jokerId && !clone.IsPreview)
            .Select(clone => MiscUtils.GetRoomName(clone.WorldPosition))
            .Where(room => !string.IsNullOrWhiteSpace(room))
            .GroupBy(room => room)
            .Select(group => group.Count() > 1
                ? string.Format(CultureInfo.InvariantCulture, "{0} x{1}", group.Key, group.Count())
                : group.Key)
            .ToList();

        return rooms.Count == 0 ? string.Empty : string.Join(", ", rooms);
    }

    public bool WinConditionMet()
    {
        var options = OptionGroupSingleton<JokerOptions>.Instance;
        if (options.WinMode == JokerWinOptions.WinWithWinners)
        {
            return false;
        }

        if (Player.HasDied() && !MetWinCon)
        {
            return false;
        }

        return MetWinCon || JokerCloneSystem.KilledCloneCount >= (int)options.KillsToWin;
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        JokerCloneSystem.ClearAll();
        MetWinCon = false;
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        JokerCloneSystem.RemoveClonesForJoker(targetPlayer.PlayerId);
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
        return MetWinCon || JokerCloneSystem.KilledCloneCount >= (int)OptionGroupSingleton<JokerOptions>.Instance.KillsToWin;
    }

    [MethodRpc((uint)Networking.ExtensionRpc.JokerPlaceClone)]
    public static void RpcJokerPlaceClone(PlayerControl joker, byte appearancePlayerId, float x, float y, float z)
    {
        if (joker == null ||
            JokerCloneSystem.GetActiveCloneCountForJoker(joker.PlayerId) >=
            (int)OptionGroupSingleton<JokerOptions>.Instance.MaxClones)
        {
            return;
        }

        var appearanceSource = MiscUtils.PlayerById(appearancePlayerId);
        if (appearanceSource == null)
        {
            return;
        }

        JokerCloneSystem.PlaceClone(joker.PlayerId, appearanceSource, new Vector3(x, y, z));
    }

    [MethodRpc((uint)Networking.ExtensionRpc.JokerCloneKilled)]
    public static void RpcJokerCloneKilled(PlayerControl killer, byte jokerId, byte cloneIndex)
    {
        if (!JokerCloneSystem.TryRemoveClone(cloneIndex, out var removedClone))
        {
            return;
        }

        JokerCloneSystem.AddKill();

        var jokerPlayer = MiscUtils.PlayerById(jokerId);
        if (jokerPlayer == null)
        {
            return;
        }

        var options = OptionGroupSingleton<JokerOptions>.Instance;
        var killsNeeded = (int)options.KillsToWin;
        var currentKills = JokerCloneSystem.KilledCloneCount;

        if (currentKills >= killsNeeded && jokerPlayer.Data.Role is JokerRole role)
        {
            role.MetWinCon = true;

            if (options.WinMode == JokerWinOptions.WinWithWinners && jokerPlayer.AmOwner)
            {
                DeathHandlerModifier.RpcUpdateLocalDeathHandler(
                    PlayerControl.LocalPlayer,
                    "DiedToWinning",
                    DeathEventHandlers.CurrentRound,
                    DeathHandlerOverride.SetFalse,
                    killedBy: PlayerControl.LocalPlayer,
                    lockInfo: DeathHandlerOverride.SetTrue);
            }
        }

        if (killer != null && killer.AmOwner)
        {
            ShowKillerCloneNotification();
        }

        if (jokerPlayer.AmOwner)
        {
            ShowJokerCloneKilledNotification(removedClone.WorldPosition, currentKills, killsNeeded);
        }
    }

    [MethodRpc((uint)Networking.ExtensionRpc.JokerDestroyClone)]
    public static void RpcJokerDestroyClone(PlayerControl joker, byte cloneIndex)
    {
        JokerCloneSystem.TryRemoveClone(cloneIndex, out _);
    }

    private static void ShowKillerCloneNotification()
    {
        try
        {
            TouAudio.PlaySound(TouAudio.DiscoveredSound);
            Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Joker));

            Helpers.CreateAndShowNotification(
                FormatJokerNotification(TouLocale.GetParsed("ExtensionRoleJokerFooledNotif", "You've been fooled!")),
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.JokerRoleIcon.LoadAsset())?.AdjustNotification();
        }
        catch
        {
        }
    }

    private static void ShowJokerCloneKilledNotification(Vector3 clonePosition, int currentKills, int killsNeeded)
    {
        try
        {
            Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Joker));

            var room = MiscUtils.GetRoomName(clonePosition);
            var text = TouLocale.GetParsed("ExtensionRoleJokerCloneKilledNotif", "Your clone was killed in {0}! ({1}/{2})")
                .Replace("{0}", room)
                .Replace("{1}", currentKills.ToString(CultureInfo.InvariantCulture))
                .Replace("{2}", killsNeeded.ToString(CultureInfo.InvariantCulture));

            Helpers.CreateAndShowNotification(
                FormatJokerNotification(text),
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.JokerRoleIcon.LoadAsset())?.AdjustNotification();
        }
        catch
        {
        }
    }

    private static string FormatJokerNotification(string text)
    {
        return $"<b>{TouExtensionColors.Joker.ToTextColor()}{text}</color></b>";
    }
}
