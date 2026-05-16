using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using System;
using System.Linq;
using MiraAPI.Modifiers;
using TownOfUs.Assets;
using Reactor.Utilities;
using UnityEngine.UI;

namespace TouMegaChujoweExtension.Buttons.Impostor;

public sealed class DetonatorAttachButton : TownOfUsKillRoleButton<DetonatorRole, PlayerControl>
{
    private PlayerControl? _attachTarget;
    private float _attachTimer;
    private bool _isAttaching;
    private PlayerControl? _lastOutlined;

    public override string Name => TouLocale.Get("ExtensionRoleDetonatorAttach", "Attach Bomb");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Detonator;
    public override float Cooldown
    {
        get
        {
            var local = PlayerControl.LocalPlayer;
            if (local == null) return DetonatorSystem.GetDetonateCooldown();
            
            bool hasBomb = DetonatorSystem.HasAnyActiveBomb(local.PlayerId);
            return hasBomb ? DetonatorSystem.GetDetonateCooldown() : local.GetKillCooldown();
        }
    }
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.DetonatorAttachSprite;
    public override bool ZeroIsInfinite { get; set; } = true;

    public override PlayerControl? GetTarget()
    {
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(false, Distance);
    }

    public override bool CanClick()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data.IsDead) return false;

        bool hasBomb = DetonatorSystem.HasAnyActiveBomb(local.PlayerId);
        float remaining = hasBomb ? DetonatorSystem.GetManualDetonateRemainingTime(local.PlayerId) : DetonatorSystem.GetAttachRemainingTime(local.PlayerId);

        if (hasBomb)
            return remaining <= 0;

        var t = GetTarget();
        return t != null && !t.IsImpostorAligned() && remaining <= 0;
    }

    protected override void OnClick()
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.Data.IsDead) return;

        if (!CanClick()) return;

        bool hasBomb = DetonatorSystem.HasAnyActiveBomb(localPlayer.PlayerId);

        if (hasBomb)
        {
            DetonatorRole.RpcDetonate(localPlayer);
            DetonatorSystem.ResetAttachCooldown(localPlayer.PlayerId);
            localPlayer.SetKillTimer(localPlayer.GetKillCooldown());
            Button?.SetDisabled();
            return;
        }

        var target = GetTarget();
        if (target == null || target.IsImpostorAligned()) return;

        _attachTarget = target;
        _attachTimer = 0f;
        _isAttaching = true;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        if (playerControl == null || playerControl.Data.IsDead) return;

        var options = OptionGroupSingleton<DetonatorOptions>.Instance;
        bool hasBomb = DetonatorSystem.HasAnyActiveBomb(playerControl.PlayerId);

        if (hasBomb)
        {
            _isAttaching = false;
            _attachTarget = null;
            _attachTimer = 0f;
            OverrideName(TouLocale.Get("ExtensionRoleDetonatorDetonate", "Detonate"));
            OverrideSprite(TouExtensionImpAssets.DetonatorDetonateSprite.LoadAsset());
            
            Timer = DetonatorSystem.GetManualDetonateRemainingTime(playerControl.PlayerId);
        }
        else
        {
            OverrideName(TouLocale.Get("ExtensionRoleDetonatorAttach", "Attach Bomb"));
            OverrideSprite(TouExtensionImpAssets.DetonatorAttachSprite.LoadAsset());
            
            Timer = playerControl.killTimer;
        }

        // Handling Attach phase (progress bar on button)
        if (_isAttaching)
        {
            if (_attachTarget == null || _attachTarget.Data.IsDead || !CanClick())
            {
                _isAttaching = false;
                _attachTimer = 0f;
            }
            else
            {
                _attachTimer += Time.fixedDeltaTime;
                var progress = Mathf.Clamp01(_attachTimer / options.AttachDuration);
                if (Button != null) Button.SetCooldownFill(1f - progress);

                if (_attachTimer >= options.AttachDuration)
                {
                    DetonatorRole.RpcAttachBomb(playerControl, _attachTarget);
                    DetonatorSystem.ResetDetonateCooldown(playerControl.PlayerId);
                    playerControl.SetKillTimer(playerControl.GetKillCooldown());
                    _isAttaching = false;
                    _attachTimer = 0f;
                    _attachTarget = null;
                }
                
                // Button should be bright and outline visible during attaching
                UpdateOutline();
                SetButtonState(true, hasBomb, true);
                return; 
            }
        }

        base.FixedUpdate(playerControl);
        UpdateOutline();
        SetButtonState(hasBomb || GetTarget() != null, hasBomb, false);
    }

    private void SetButtonState(bool shouldBeBright, bool hasBomb, bool isAttaching)
    {
        if (Button == null) return;

        // White timer text
        if (Button.cooldownTimerText != null && Button.cooldownTimerText.gameObject.activeSelf)
        {
            Button.cooldownTimerText.color = Color.white;
        }

        if (Button.buttonLabelText != null)
        {
            Button.buttonLabelText.text = hasBomb ? TouLocale.Get("ExtensionRoleDetonatorDetonate", "Detonate") : TouLocale.Get("ExtensionRoleDetonatorAttach", "Attach Bomb");
            Button.buttonLabelText.color = shouldBeBright ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        }

        if (Button.graphic != null)
        {
            float alpha = shouldBeBright ? 1f : 0.5f;
            Button.graphic.color = new Color(1f, 1f, 1f, alpha);
            Button.graphic.material.SetFloat("_Desat", shouldBeBright ? 0f : 1f);
        }

        // Red fill (radial cooldown) like Poisoner
        try
        {
            var fill = Button.gameObject.transform.Find("CooldownFill")?.GetComponent<Image>();
            if (fill != null)
            {
                fill.color = (hasBomb || isAttaching) ? Palette.ImpostorRed : Color.white;
            }
        }
        catch { /* ignore */ }
    }

    private void UpdateOutline()
    {
        var target = GetTarget();
        if (_lastOutlined != null && _lastOutlined != target)
        {
            _lastOutlined.cosmetics.SetOutline(false, new Il2CppSystem.Nullable<Color>());
        }

        if (target != null)
        {
            target.cosmetics.SetOutline(true, new Il2CppSystem.Nullable<Color>(Palette.ImpostorRed));
        }

        _lastOutlined = target;
    }
}
