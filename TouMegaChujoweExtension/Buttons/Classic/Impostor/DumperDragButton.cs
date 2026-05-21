using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TownOfUs.Modules.Localization;
using UnityEngine;
using System;
using System.Linq;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class DumperDragButton : TownOfUsRoleButton<DumperRole, DeadBody>
{
    public static DumperDragButton? Instance { get; private set; }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        Instance = this;
        if (KeybindIcon != null)
        {
            KeybindIcon.transform.localPosition = new Vector3(0.4f, 0.45f, -9f);
        }
    }

    public override string Name => (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null && DumperSystem.GetDraggedBodyId(PlayerControl.LocalPlayer.PlayerId).HasValue) 
        ? TouLocale.Get("ExtensionRoleDumperDump", "Dump") 
        : TouLocale.Get("ExtensionRoleDumperTake", "Store");

    public override BaseKeybind Keybind => Keybinds.SecondaryAction; // Keybind F
    public override Color TextOutlineColor => TouExtensionColors.Dumper;
    public override float Cooldown => OptionGroupSingleton<DumperOptions>.Instance.TakeCooldown;
    public override bool ZeroIsInfinite { get; set; } = true;
    private bool _isProcessingClick;

    public override LoadableAsset<Sprite> Sprite => (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null && DumperSystem.GetDraggedBodyId(PlayerControl.LocalPlayer.PlayerId).HasValue) 
        ? TownOfUs.Assets.TouImpAssets.DropSprite 
        : TownOfUs.Assets.TouImpAssets.DragSprite;

    public static void SetOwnCooldown()
    {
        var instance = MiraAPI.Hud.CustomButtonSingleton<DumperDragButton>.Instance;
        if (instance != null)
        {
            instance.Timer = instance.Cooldown;
        }
    }

    public override void ClickHandler()
    {
        if (_isProcessingClick) return;
        _isProcessingClick = true;

        try
        {
            if (!CanClick()) return;

            if (LimitedUses)
            {
                UsesLeft--;
                Button?.SetUsesRemaining(UsesLeft);
            }

            OnClick();
            Button?.SetDisabled();
        }
        finally
        {
            Reactor.Utilities.Coroutines.Start(ResetProcessingFlag());
        }
    }

    private System.Collections.IEnumerator ResetProcessingFlag()
    {
        yield return new WaitForSeconds(0.2f);
        _isProcessingClick = false;
    }

    public override DeadBody? GetTarget()
    {
        return PlayerControl.LocalPlayer?.GetNearestDeadBody(PlayerControl.LocalPlayer.MaxReportDistance / 4f);
    }

    public override bool IsTargetValid(DeadBody? target)
    {
        return target && target.Reported == false;
    }

    public override bool CanUse()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data.IsDead || player.inVent) return false;

        var isDragging = DumperSystem.GetDraggedBodyId(player.PlayerId).HasValue;
        if (isDragging)
        {
            return true;
        }

        return base.CanUse() && Target && Timer <= 0f;
    }

    public override bool CanClick()
    {
        return CanUse();
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        if (DumperSystem.GetDraggedBodyId(player.PlayerId).HasValue)
        {
            DumperRole.RpcDropBody(player);
        }
        else
        {
            if (Target != null && !Target.Reported)
            {
                DumperRole.RpcPickupBody(player, Target.ParentId);
            }
        }
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);
        
        if (Button != null)
        {
            if (Button.usesRemainingText != null) Button.usesRemainingText.gameObject.SetActive(false);
            if (Button.usesRemainingSprite != null) Button.usesRemainingSprite.gameObject.SetActive(false);

            var isDragging = DumperSystem.GetDraggedBodyId(playerControl.PlayerId).HasValue;
            OverrideName(isDragging 
                ? TouLocale.Get("ExtensionRoleDumperDump", "Dump") 
                : TouLocale.Get("ExtensionRoleDumperTake", "Store"));

            var targetSprite = isDragging 
                ? TownOfUs.Assets.TouImpAssets.DropSprite 
                : TownOfUs.Assets.TouImpAssets.DragSprite;

            if (Button.graphic != null && targetSprite != null)
            {
                var spr = targetSprite.LoadAsset();
                if (spr != null && Button.graphic.sprite != spr)
                {
                    Button.graphic.sprite = spr;
                }
            }
        }
        
        var autoDumpTime = DumperSystem.GetAutoDumpTime(playerControl.PlayerId);
        if (autoDumpTime.HasValue)
        {
            float remaining = Mathf.Max(0f, autoDumpTime.Value - Time.time);
            Button?.SetFillUp(remaining, OptionGroupSingleton<DumperOptions>.Instance.MaxDragDuration);
            if (Button != null && Button.cooldownTimerText != null)
            {
                Button.cooldownTimerText.text = Mathf.Ceil(remaining).ToString();
                Button.cooldownTimerText.gameObject.SetActive(true);
            }

            // Force auto-dump when time expires (defensive fallback)
            if (remaining <= 0f && playerControl.AmOwner && DumperSystem.GetDraggedBodyId(playerControl.PlayerId).HasValue)
            {
                DumperRole.RpcDropBody(playerControl);
            }
        }
    }
}
