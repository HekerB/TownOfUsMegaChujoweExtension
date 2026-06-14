using System.Text;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Modifiers.Crewmate;
using TouMegaChujoweExtension.Modifiers.Game;
using UnityEngine;
using System.Globalization;
using TownOfUs.Modifiers.Game.Universal;
using TownOfUs;
using TownOfUs.Options.Roles.Impostor;
using TownOfUs.Roles.Impostor;
using TownOfUs.Roles;
using TownOfUs.Assets;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using System;
using System.Collections.Generic;
using System.Linq;
using TouMegaChujoweExtension;
using MiraAPI.Patches.Stubs;
using MiraAPI.Hud;
using Cpp2IL.Core.Utils;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Roles.Classic.Crewmate;

public sealed class TavernKeeperRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public override bool IsAffectedByComms => false;
    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => "TavernKeeper";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Tavern Keeper");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb", "Drink with others to protect them and disable their abilities.");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription", "Drink with others to protect them from death and disable their abilities.");
    public Color RoleColor => TouExtensionColors.TavernKeeper;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateProtective;

    private static readonly Color NotificationColor = new Color32(255, 214, 74, 255);

    public byte LastRoleblockedPlayerId { get; set; } = byte.MaxValue;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouRoleIcons.Barkeeper,
        OptionsScreenshot = TouBanners.CrewmateRoleBanner,
        IntroSound = TouAudio.ToppatIntroSound,
    };

    public RoleBehaviour AppearAs => this;

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);

        if (!targetPlayer.AmOwner) return;

        CustomButtonSingleton<TavernKeeperDrinkButton>.Instance?.ResetCooldownAndOrEffect();
    }

    public override void OnDeath(DeathReason reason)
    {
        Deinitialize(Player);
    }

    public override void OnMeetingStart()
    {
        RoleBehaviourStubs.OnMeetingStart(this);

        if (Player.AmOwner)
        {
            var drinkButton = CustomButtonSingleton<TavernKeeperDrinkButton>.Instance;
            if (drinkButton != null)
            {
                drinkButton.ResetCooldownAndOrEffect();
                if (OptionGroupSingleton<TavernKeeperOptions>.Instance.ResetUsesAfterMeeting)
                {
                    drinkButton.SetUses((int)OptionGroupSingleton<TavernKeeperOptions>.Instance.MaxUses);
                }
            }
        }
    }

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var sb = ITownOfUsRole.SetNewTabText(this);

        if (LastRoleblockedPlayerId != byte.MaxValue)
        {
            var target = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == LastRoleblockedPlayerId);
            if (target != null && target.HasModifier<RoleblockedModifier>())
            {
                var drinkingWith = TouLocale.Get("ExtensionRoleTavernKeeperTabCurrentlyRoleblocked", "Drinking With: \\%player\\%")
                    .Replace("\\%player\\%", target.CachedPlayerData.PlayerName);
                sb.AppendLine();
                sb.AppendLine($"{NotificationColor.ToTextColor()}<b>{drinkingWith}</b></color>");
            }
        }

        return sb;
    }

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + TownOfUs.Utilities.MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.Get($"ExtensionRole{LocaleKey}Drink", "Drink"),
            TouLocale.Get($"ExtensionRole{LocaleKey}DrinkWikiDescription", "Drink with a player to protect them from death and disable their abilities for \\%duration\\%s.")
                .Replace("\\%duration\\%", OptionGroupSingleton<TavernKeeperOptions>.Instance.RoleblockDuration.ToString(CultureInfo.InvariantCulture)),
            TouCrewAssets.CleanseSprite)
    ];

    [MethodRpc((uint)TouMegaChujoweExtension.Networking.ExtensionRpc.TavernKeeperRoleblock)]
    public static void RpcRoleblock(PlayerControl player, PlayerControl target)
    {
        var options = OptionGroupSingleton<TavernKeeperOptions>.Instance;
        if (options == null) return;

        var roleblockDuration = options.RoleblockDuration;
        var immunityDuration = options.ImmunityDuration.Value;
        var applyImmunity = options.Immunity;
        var invertControls = options.InvertControlsOfRoleblocked;

        var showAlert = options.ShowAlertToTarget;
        var alertDelay = options.AlertDelay.Value;

        var iconSelf = TouRoleIcons.Barkeeper.LoadAsset();
        var iconTarget = TouRoleIcons.Barkeeper.LoadAsset();
        var targetName = target.CachedPlayerData.PlayerName;

        var immune = true;
        if (!target.HasModifier<DrinkImmunityModifier>() && !target.HasModifier<DrunkModifier>() &&
            !target.HasModifier<RoleblockedModifier>() && !target.IsRole<TavernKeeperRole>())
        {
            immune = false;
            target.AddModifier<RoleblockedModifier>(invertControls, applyImmunity, roleblockDuration, immunityDuration);
            if (player.GetRole<TavernKeeperRole>() is TavernKeeperRole tkRole)
            {
                tkRole.LastRoleblockedPlayerId = target.PlayerId;
            }
        }

        if (player.AmOwner)
        {
            var msgKey = immune
                ? "ExtensionRoleTavernKeeperNotificationImmune"
                : "ExtensionRoleTavernKeeperNotificationRoleblocked";
            var fallback = immune
                ? "\\%player\\% resisted your drink!"
                : "\\%player\\% is protected by your drink and cannot use abilities!";
            var msg = TouLocale.Get(msgKey, fallback).Replace("\\%player\\%", targetName);
            ShowNotification(msg, iconSelf);
        }

        if (target.AmOwner && showAlert)
        {
            if (immune)
            {
                var msg = TouLocale.Get("ExtensionRoleTavernKeeperAlertImmune", "The Tavern Keeper drank with you, but you resisted it!");
                if (alertDelay > 0f)
                {
                    Reactor.Utilities.Coroutines.Start(ShowDelayedAlert(msg, iconTarget, alertDelay));
                }
                else
                {
                    ShowNotification(msg, iconTarget);
                }
            }
            else
            {
                var msg = TouLocale.Get("ExtensionRoleTavernKeeperAlertRoleblocked", "The Tavern Keeper drank with you and protects you from deaths with a hangover, but you cannot use abilities.");
                if (alertDelay > 0f)
                {
                    Reactor.Utilities.Coroutines.Start(ShowDelayedAlert(msg, iconTarget, alertDelay));
                }
                else
                {
                    ShowNotification(msg, iconTarget);
                }
            }
        }

        static void ShowNotification(string message, Sprite icon)
        {
            var notif = Helpers.CreateAndShowNotification(FormatNotification(message), Color.white, new Vector3(0f, 1f, -20f), spr: icon);
            notif.AdjustNotification();
        }
    }

    [HideFromIl2Cpp]
    private static System.Collections.IEnumerator ShowDelayedAlert(string message, Sprite icon, float delay)
    {
        yield return new WaitForSeconds(delay);
        var notif = Helpers.CreateAndShowNotification(FormatNotification(message), Color.white, new Vector3(0f, 1f, -20f), spr: icon);
        notif.AdjustNotification();
    }

    private static string FormatNotification(string message)
    {
        return $"<b>{NotificationColor.ToTextColor()}{message}</color></b>";
    }
}
