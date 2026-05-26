using System;
using System.Collections.Generic;
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
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
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
            TouExtensionCrewAssets.DecoyButtonSprite)
    ];

    public Color RoleColor => TouExtensionColors.Joker;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralEvil;
    public bool HasImpostorVision => false;

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = false,
        Icon = TouExtensionIcons.JokerRoleIcon,
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
            TouLocale.Get("ExtensionRoleJokerTabClonesKilled", "Clones Killed: {0} / {1}"),
            currentKills,
            killsNeeded));

        if (MetWinCon)
        {
            stringBuilder.AppendLine(TouLocale.Get("ExtensionRoleJokerTabObjectiveComplete", "<b>Objective Complete!</b>"));
        }

        return stringBuilder;
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
        JokerCloneSystem.AddKill();

        if (!JokerCloneSystem.TryRemoveClone(cloneIndex, out _))
        {
            return;
        }

        if (killer.AmOwner)
        {
            ShowNotification("ExtensionRoleJokerFooledNotif", "You've been fooled!");
        }

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
                    lockInfo: DeathHandlerOverride.SetTrue);
            }
        }

        if (jokerPlayer.AmOwner)
        {
            ShowNotification("ExtensionRoleJokerCloneKilledNotif", $"Clone killed! ({currentKills}/{killsNeeded})", currentKills, killsNeeded);
        }
    }

    [MethodRpc((uint)Networking.ExtensionRpc.JokerDestroyClone)]
    public static void RpcJokerDestroyClone(PlayerControl joker, byte cloneIndex)
    {
        JokerCloneSystem.TryRemoveClone(cloneIndex, out _);
    }

    private static void ShowNotification(string localeKey, string fallback, int? currentKills = null, int? killsNeeded = null)
    {
        try
        {
            SoundManager.Instance.PlaySound(TouExtensionAudio.JokerLaugh.LoadAsset(), false, 1f);
            var text = TouLocale.GetParsed(localeKey, fallback);
            if (currentKills.HasValue && killsNeeded.HasValue)
            {
                text = $"{text} ({currentKills}/{killsNeeded})";
            }

            Helpers.CreateAndShowNotification(
                text,
                TouExtensionColors.Joker,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.JokerRoleIcon.LoadAsset())?.AdjustNotification();
        }
        catch
        {
            // notification fallback
        }
    }
}
