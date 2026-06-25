using System;
using System.Linq;
using System.Collections;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class BurrowerDigButton : TownOfUsRoleButton<BurrowerRole>
{
    public static BurrowerDigButton? Instance { get; private set; }
    private bool _isProcessingClick;

    public BurrowerDigButton()
    {
        Instance = this;
    }

    public override string Name
    {
        get
        {
            var role = PlayerControl.LocalPlayer?.GetRole<BurrowerRole>();
            if (role != null && role.IsPreparingDig)
            {
                return role.CanCancelPreparingDig()
                    ? TouLocale.Get("ExtensionRoleBurrowerCancel", "Cancel")
                    : TouLocale.Get("ExtensionRoleBurrowerDig", "Dig");
            }

            return role != null && role.IsUnderground
                ? TouLocale.Get("ExtensionRoleBurrowerEscape", "Escape")
                : TouLocale.Get("ExtensionRoleBurrowerDig", "Dig");
        }
    }

    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Burrower;
    public override float Cooldown => OptionGroupSingleton<BurrowerOptions>.Instance.DigCooldown;
    public override LoadableAsset<Sprite> Sprite => TouImpAssets.MineSprite;
    public override int MaxUses => (int)OptionGroupSingleton<BurrowerOptions>.Instance.MaxBurrows;
    public override bool ZeroIsInfinite { get; set; } = true;

    public override bool CanUse()
    {
        if (MeetingHud.Instance || ExileController.Instance)
        {
            return false;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied())
        {
            return false;
        }

        var role = player.GetRole<BurrowerRole>();
        if (role == null)
        {
            return false;
        }

        if (HasNoDigUsesLeft(role))
        {
            return false;
        }

        return CanBurrowAtCurrentPosition(player, role);
    }

    public override bool CanClick()
    {
        var player = PlayerControl.LocalPlayer;
        var role = player?.GetRole<BurrowerRole>();

        if (player != null && (player.inVent || player.walkingToVent))
        {
            if (role == null || !role.IsUnderground)
            {
                return false;
            }
        }

        if (role == null)
        {
            return false;
        }

        if (role.IsPreparingDig)
        {
            return CanUse() && role.CanCancelPreparingDig();
        }

        if (role.IsUnderground || role.IsDigging)
        {
            return CanUse() && role.CanCancelUndergroundDig();
        }

        return CanUse() && Timer <= 0f;
    }

    public override void ClickHandler()
    {
        if (_isProcessingClick)
        {
            return;
        }

        _isProcessingClick = true;

        try
        {
            if (CanClick())
            {
                var player = PlayerControl.LocalPlayer;
                var role = player?.GetRole<BurrowerRole>();
                if (player == null || role == null)
                {
                    return;
                }

                var wasPreparing = role.IsPreparingDig;
                var wasUnderground = role.IsUnderground;
                if (!TryPerformBurrowAction(player, role))
                {
                    return;
                }

                if (!wasPreparing && !wasUnderground && LimitedUses && !(ZeroIsInfinite && MaxUses == 0))
                {
                    UsesLeft--;
                    Button?.SetUsesRemaining(UsesLeft);
                    SetTextOutline(TextOutlineColor);

                    if (Button?.usesRemainingSprite != null)
                    {
                        Button.usesRemainingSprite.color = TextOutlineColor;
                    }
                }
            }
        }
        finally
        {
            Coroutines.Start(ResetProcessingFlag());
        }
    }

    private IEnumerator ResetProcessingFlag()
    {
        yield return new WaitForSeconds(0.2f);
        _isProcessingClick = false;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        if (MeetingHud.Instance || ExileController.Instance)
        {
            return;
        }

        var role = playerControl.GetRole<BurrowerRole>();

        if (Button != null)
        {
            if (role != null && role.IsPreparingDig)
            {
                OverrideName(role.CanCancelPreparingDig()
                    ? TouLocale.Get("ExtensionRoleBurrowerCancel", "Cancel")
                    : TouLocale.Get("ExtensionRoleBurrowerDig", "Dig"));
            }
            else
            {
                OverrideName(role != null && role.IsUnderground
                    ? TouLocale.Get("ExtensionRoleBurrowerEscape", "Escape")
                    : TouLocale.Get("ExtensionRoleBurrowerDig", "Dig"));
            }
        }

        if (role != null && role.IsPreparingDig)
        {
            var remaining = Mathf.Max(0f, role.PrepareDigEndTime - Time.time);
            Button?.SetFillUp(remaining, OptionGroupSingleton<BurrowerOptions>.Instance.EnterDelay);

            if (Button?.cooldownTimerText != null)
            {
                var format = remaining <= 10f && MiraAPI.LocalSettings.LocalSettingsTabSingleton<TownOfUs.TownOfUsLocalSettings>.Instance.PreciseCooldownsToggle.Value
                    ? "0.0"
                    : "0";
                Button.cooldownTimerText.text = remaining.ToString(format, System.Globalization.NumberFormatInfo.InvariantInfo);
                Button.cooldownTimerText.gameObject.SetActive(true);
            }

            return;
        }

        if (role != null && role.IsDigging)
        {
            var remaining = Mathf.Max(0f, role.DigEndTime - Time.time);
            Button?.SetFillUp(remaining, OptionGroupSingleton<BurrowerOptions>.Instance.DigDuration);

            if (Button?.cooldownTimerText != null)
            {
                var format = remaining <= 10f && MiraAPI.LocalSettings.LocalSettingsTabSingleton<TownOfUs.TownOfUsLocalSettings>.Instance.PreciseCooldownsToggle.Value
                    ? "0.0"
                    : "0";
                Button.cooldownTimerText.text = remaining.ToString(format, System.Globalization.NumberFormatInfo.InvariantInfo);
                Button.cooldownTimerText.gameObject.SetActive(true);
            }

            return;
        }

        base.FixedUpdate(playerControl);
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        var role = player?.GetRole<BurrowerRole>();
        if (player == null || role == null)
        {
            return;
        }

        TryPerformBurrowAction(player, role);
    }

    private bool TryPerformBurrowAction(PlayerControl player, BurrowerRole role)
    {
        if (role.IsPreparingDig)
        {
            if (role.CanCancelPreparingDig())
            {
                BurrowerRole.RpcCancel(player);
                Timer = Cooldown;
                return true;
            }
            return false;
        }

        if (role.IsUnderground || role.IsDigging)
        {
            if (role.IsDigging && !role.CanCancelUndergroundDig())
            {
                return false;
            }

            if (!BurrowerSystem.TryFindVentPlacementPosition(player, player.GetTruePosition(), out var emergePosition))
            {
                return false;
            }

            BurrowerRole.RpcEmerge(player, emergePosition);
            Timer = Cooldown;
            return true;
        }

        if (!BurrowerSystem.IsNearMapVent(player.transform.position) &&
            BurrowerSystem.TryFindVentPlacementPosition(player, player.GetTruePosition(), out var digPosition))
        {
            BurrowerRole.RpcUnderground(player, digPosition);
            return true;
        }

        return false;
    }

    private static bool CanBurrowAtCurrentPosition(PlayerControl player, BurrowerRole role)
    {
        var position = player.GetTruePosition();

        if (!role.IsUnderground && !role.IsDigging && BurrowerSystem.IsNearMapVent(player.transform.position))
        {
            return false;
        }

        return role.IsPreparingDig || role.IsDigging || BurrowerSystem.TryFindVentPlacementPosition(player, position, out _);
    }

    private bool HasNoDigUsesLeft(BurrowerRole role)
    {
        return !role.IsUnderground &&
               !role.IsDigging &&
               !role.IsPreparingDig &&
               LimitedUses &&
               !(ZeroIsInfinite && MaxUses == 0) &&
               UsesLeft <= 0;
    }

}
