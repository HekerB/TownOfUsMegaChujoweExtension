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

namespace TouMegaChujoweExtension.Buttons.Impostor;

public sealed class DetonatorAttachButton : TownOfUsRoleButton<DetonatorRole, PlayerControl>
{
    private PlayerControl? _attachTarget;
    private float _attachTimer; // How long we've been attaching
    private bool _isAttaching;

    public override string Name => TouLocale.Get("ExtensionRoleDetonatorAttach", "Attach Bomb");

    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Detonator;

    public override float Cooldown => GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown + MapCooldown;

    public override float EffectDuration => 0f;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.DetonatorAttachSprite;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        if (Button != null)
        {
            var icon = Button.transform.FindChild("Icon");
            if (icon != null) icon.localScale = Vector3.one * 0.75f;
        }
    }

    public override PlayerControl? GetTarget()
    {
        if (DetonatorSystem.HasAnyActiveBomb(PlayerControl.LocalPlayer.PlayerId)) return null;
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance);
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (DetonatorSystem.HasAnyActiveBomb(PlayerControl.LocalPlayer.PlayerId)) return true;
        if (!base.IsTargetValid(target) || target == null) return false;
        return target != PlayerControl.LocalPlayer;
    }

    public override bool CanClick()
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (DetonatorSystem.HasAnyActiveBomb(localPlayer.PlayerId))
        {
            return Timer <= 0;
        }

        var target = GetTarget();
        if (target != null && target.IsImpostorAligned()) return false;

        return base.CanClick();
    }

    public override bool CanUse()
    {
        if (DetonatorSystem.HasAnyActiveBomb(PlayerControl.LocalPlayer.PlayerId))
        {
            return Timer <= 0;
        }
        return base.CanUse();
    }

    protected override void OnClick()
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (DetonatorSystem.HasAnyActiveBomb(localPlayer.PlayerId))
        {
            if (DetonatorSystem.CanManualDetonate(localPlayer.PlayerId))
            {
                DetonatorRole.RpcDetonate(localPlayer);
                localPlayer.SetKillTimer(localPlayer.GetKillCooldown());
            }
            return;
        }

        if (Target == null || Target.IsImpostorAligned()) return;
        
        // Start "Witch-style" attaching
        _attachTarget = Target;
        _attachTimer = 0f;
        _isAttaching = true;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        if (playerControl == null || playerControl.Data.IsDead)
        {
            return;
        }

        var options = OptionGroupSingleton<DetonatorOptions>.Instance;
        bool hasBomb = DetonatorSystem.HasAnyActiveBomb(playerControl.PlayerId);
        
        if (hasBomb)
        {
            _isAttaching = false;
            _attachTarget = null;
            _attachTimer = 0f;
            OverrideName(TouLocale.Get("ExtensionRoleDetonatorDetonate", "Detonate"));
            OverrideSprite(TouExtensionImpAssets.DetonatorDetonateSprite.LoadAsset());

            float manualDelay = DetonatorSystem.GetManualDetonateRemainingTime(playerControl.PlayerId);
            Timer = Mathf.Max(manualDelay, playerControl.killTimer);
        }
        else
        {
            OverrideName(TouLocale.Get("ExtensionRoleDetonatorAttach", "Attach Bomb"));
            OverrideSprite(TouExtensionImpAssets.DetonatorAttachSprite.LoadAsset());
            Timer = playerControl.killTimer;

            if (_isAttaching && _attachTarget != null)
            {
                var dist = Vector2.Distance(playerControl.GetTruePosition(), _attachTarget.GetTruePosition());
                if (dist > Distance || _attachTarget.HasDied() || playerControl.killTimer > 0)
                {
                    _isAttaching = false;
                    _attachTarget = null;
                    _attachTimer = 0f;
                }
                else
                {
                    _attachTimer += Time.fixedDeltaTime;
                    var progress = Mathf.Clamp01(_attachTimer / options.AttachDuration);
                    
                    if (Button != null) Button.SetCooldownFill(1f - progress);

                    if (progress >= 1f)
                    {
                        DetonatorRole.RpcAttachBomb(playerControl, _attachTarget);
                        playerControl.SetKillTimer(playerControl.GetKillCooldown());
                        _isAttaching = false;
                        _attachTarget = null;
                        _attachTimer = 0f;
                    }
                }
            }
        }

        base.FixedUpdate(playerControl);
        
        if (Button == null) return;

        var target = GetTarget();
        bool hasTarget = target != null;
        bool isTeammateTarget = target != null && target.IsImpostorAligned();

        // Update visuals based on target presence
        if (Button.graphic != null)
        {
            // Disable (desaturate/dim) if target is a teammate
            bool shouldHighlight = hasBomb || (hasTarget && !isTeammateTarget) || _isAttaching;
            Button.graphic.color = shouldHighlight ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            Button.graphic.material.SetFloat("_Desat", shouldHighlight ? 0f : 1f);
        }

        if (Button.buttonLabelText != null)
        {
            // If we have a target but on CD (or ready), highlight the text
            if (!hasBomb && (hasTarget || _isAttaching))
            {
                Button.buttonLabelText.color = Timer > 0 ? new Color(1f, 1f, 0.5f, 1f) : Color.white;
                Button.buttonLabelText.alpha = 1f;
            }
            else
            {
                Button.buttonLabelText.color = Color.white;
                Button.buttonLabelText.alpha = Timer > 0 ? 0.3f : 1f;
            }
        }
    }
}
