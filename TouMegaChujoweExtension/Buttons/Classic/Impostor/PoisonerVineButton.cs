using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using System.Collections;
using System;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class PoisonerVineButton : TownOfUsRoleButton<PoisonerRole>
{
    private const float CancelLockDuration = 2f;

    public override string Name => TouLocale.GetParsed("ExtensionRolePoisonerVine", "Vine");
    public override BaseKeybind Keybind => Keybinds.TertiaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Poisoner;
    public override float Cooldown
    {
        get
        {
            if (PlayerControl.LocalPlayer != null)
                return PlayerControl.LocalPlayer.GetKillCooldown();
            return GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown;
        }
    }
    public override float EffectDuration => 0f;
    public override int MaxUses => 0;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.VineButtonSprite;
    public override bool ZeroIsInfinite { get; set; } = true;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        Reactor.Utilities.Coroutines.Start(CoMoveWithDelay());
    }

    private IEnumerator CoMoveWithDelay()
    {
        yield return MiscUtils.CoMoveButtonIndex(this, false);
    }

    private PlayerControl? _lastOutlined;
    private bool _isSeeking;
    private float _seekingTimer;
    private float _seekingDuration;
    private float _cancelUnlockTime;

    private bool _isVining;
    private float _vineTimer;
    private float _vineDuration;

    private bool CanCancelSeekingNow =>
        OptionGroupSingleton<PoisonerOptions>.Instance.CanCancelVineSeeking &&
        Time.time >= _cancelUnlockTime;


    public override bool CanUse()
    {
        if (!OptionGroupSingleton<PoisonerOptions>.Instance.VineEnabled) return false;
        if (MeetingHud.Instance || HudManager.Instance.Chat.IsOpenOrOpening) return false;
        if (PoisonSystem.IsVineActive) return false;
        if (PoisonSystem.HasActivePoison) return false;

        if (_isSeeking) return CanCancelSeekingNow;
        if (PoisonSystem.IsSeeking) return false;

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied()) return false;
        if (player.inVent) return false;
        if (TouMegaChujoweExtension.Modules.PelicanSystem.IsSwallowed(player.PlayerId)) return false;

        return true;
    }

    public override bool CanClick()
    {
        if (!OptionGroupSingleton<PoisonerOptions>.Instance.VineEnabled) return false;
        if (MeetingHud.Instance || HudManager.Instance.Chat.IsOpenOrOpening) return false;

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied()) return false;

        if (_isSeeking) return CanCancelSeekingNow;
        if (_isVining) return false;

        return CanUse() && Timer <= 0f;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        if (!OptionGroupSingleton<PoisonerOptions>.Instance.VineEnabled)
        {
            Button?.gameObject.SetActive(false);
            return;
        }

        if (MeetingHud.Instance)
        {
            if (_isSeeking) EndSeeking(false);
            if (_isVining) EndVining();
            base.FixedUpdate(playerControl);
            return;
        }

        if (playerControl == null || !playerControl.IsRole<PoisonerRole>())
        {
            ClearOutline();
            if (_isSeeking) EndSeeking(false);
            if (_isVining) EndVining();
            if (playerControl != null) base.FixedUpdate(playerControl);
            return;
        }

        // === Vining countdown phase (waiting for target to die) ===
        if (_isVining)
        {
            _vineTimer -= Time.fixedDeltaTime;

            if (_vineTimer <= 0f)
            {
                EndVining();

                Timer = Cooldown;
                playerControl.SetKillTimer(GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown);
                PoisonerPoisonButton.SetOwnCooldown();

                OverrideSprite(TouExtensionImpAssets.VineButtonSprite.LoadAsset());
                OverrideName(TouLocale.GetParsed("ExtensionRolePoisonerVine", "Vine"));
            }
            else
            {
                Timer = -1f;

                if (Button != null)
                {
                    Button.SetEnabled();
                    Button.SetFillUp(_vineTimer, _vineDuration);
                    var format = _vineTimer <= 10f && MiraAPI.LocalSettings.LocalSettingsTabSingleton<TownOfUs.TownOfUsLocalSettings>.Instance.PreciseCooldownsToggle.Value
                        ? "0.0"
                        : "0";
                    Button.cooldownTimerText.text = _vineTimer.ToString(format, System.Globalization.NumberFormatInfo.InvariantInfo);
                    Button.cooldownTimerText.gameObject.SetActive(true);
                }
            }

            Button?.gameObject.SetActive(
                HudManager.Instance.UseButton.isActiveAndEnabled ||
                HudManager.Instance.PetButton.isActiveAndEnabled);
            return;
        }

        // === Seeking countdown phase ===
        if (_isSeeking)
        {
            _seekingTimer -= Time.fixedDeltaTime;

            if (_seekingTimer <= 0f)
            {
                EndSeeking(true);
                ShowSeekingPenaltyNotification(
                    TouLocale.Get("PoisonerVineSeekingExpiredPenalty", "Seeking expired! Cooldown applied."));
            }
            else
            {
                Timer = -1f;

                if (Button != null)
                {
                    if (CanCancelSeekingNow)
                    {
                        Button.SetEnabled();
                    }
                    else
                    {
                        Button.SetDisabled();
                    }
                    Button.SetFillUp(_seekingTimer, _seekingDuration);
                    var format = _seekingTimer <= 10f && MiraAPI.LocalSettings.LocalSettingsTabSingleton<TownOfUs.TownOfUsLocalSettings>.Instance.PreciseCooldownsToggle.Value
                        ? "0.0"
                        : "0";
                    Button.cooldownTimerText.text = _seekingTimer.ToString(format, System.Globalization.NumberFormatInfo.InvariantInfo);
                    Button.cooldownTimerText.gameObject.SetActive(true);
                }

                // Check outline under mouse cursor
                PlayerControl? mouseTarget = null;
                if (Camera.main != null)
                {
                    var mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    var minClickDist = 0.8f;
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc == null || pc.Data.IsDead || pc.PlayerId == playerControl.PlayerId) continue;
                        if (pc.IsImpostorAligned()) continue;
                        if (TouMegaChujoweExtension.Modules.PelicanSystem.IsSwallowed(pc.PlayerId)) continue;
                        if (Vector2.Distance(mouseWorldPos, pc.transform.position) < minClickDist)
                        {
                            mouseTarget = pc;
                            break;
                        }
                    }
                }
                UpdateOutline(mouseTarget);
            }

            Button?.gameObject.SetActive(
                HudManager.Instance.UseButton.isActiveAndEnabled ||
                HudManager.Instance.PetButton.isActiveAndEnabled);
            return;
        }

        // === Normal Mode ===
        if (PoisonSystem.HasActivePoison || PoisonSystem.IsVineActive || playerControl.inVent)
        {
            ClearOutline();
            base.FixedUpdate(playerControl);
            return;
        }

        base.FixedUpdate(playerControl);
    }

    private void UpdateOutline(PlayerControl? target)
    {
        if (_lastOutlined != null && _lastOutlined != target)
        {
            _lastOutlined.cosmetics.SetOutline(false, new Il2CppSystem.Nullable<Color>());
        }

        if (target != null)
        {
            target.cosmetics.SetOutline(true,
                new Il2CppSystem.Nullable<Color>(new Color(0.1f, 0.6f, 0.1f)));
        }

        _lastOutlined = target;
    }

    private void ClearOutline()
    {
        if (_lastOutlined != null)
        {
            _lastOutlined.cosmetics.SetOutline(false, new Il2CppSystem.Nullable<Color>());
            _lastOutlined = null;
        }
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        if (_isSeeking)
        {
            if (CanCancelSeekingNow)
            {
                EndSeeking(true);
                ShowSeekingPenaltyNotification(
                    TouLocale.Get("PoisonerVineSeekingCancelledPenalty", "Seeking cancelled! Cooldown applied."));
            }
            return;
        }

        // Enter seeking mode
        _seekingTimer = OptionGroupSingleton<PoisonerOptions>.Instance.VineSeekingDuration;
        _seekingDuration = _seekingTimer;
        _isSeeking = true;
        PoisonSystem.IsSeeking = true;
        PoisonSystem.StartSeekingFrame = UnityEngine.Time.frameCount;
        _cancelUnlockTime = Time.time + CancelLockDuration;

        OverrideSprite(TouExtensionImpAssets.VineButtonSprite.LoadAsset());
        OverrideName(TouLocale.GetParsed("ExtensionRolePoisonerSeeking", "Seeking"));
    }

    private static void ShowSeekingPenaltyNotification(string message)
    {
        MiraAPI.Utilities.Helpers.CreateAndShowNotification(
            $"<b><color=#{ColorUtility.ToHtmlStringRGBA(TouExtensionColors.Poisoner)}>{message}</color></b>",
            Color.white,
            new Vector3(0f, 1.5f, -20f),
            spr: TouExtensionIcons.PoisonerRole.LoadAsset()
        );
    }

    public void EndSeeking(bool putOnCooldown)
    {
        _isSeeking = false;
        PoisonSystem.IsSeeking = false;
        ClearOutline();

        if (!_isVining)
        {
            OverrideSprite(TouExtensionImpAssets.VineButtonSprite.LoadAsset());
            OverrideName(TouLocale.GetParsed("ExtensionRolePoisonerVine", "Vine"));
        }

        if (putOnCooldown)
        {
            Timer = Cooldown;
            var local = PlayerControl.LocalPlayer;
            if (local != null)
            {
                local.SetKillTimer(Cooldown);
            }
            PoisonerPoisonButton.SetOwnCooldown();
        }
        else
        {
            Timer = -1f;
        }
    }

    public void StartVining(float duration)
    {
        _isSeeking = false;
        PoisonSystem.IsSeeking = false;
        _isVining = true;
        _vineDuration = duration;
        _vineTimer = duration;
        ClearOutline();
        OverrideSprite(TouExtensionImpAssets.VineButtonSprite.LoadAsset());
        OverrideName(TouLocale.GetParsed("ExtensionRolePoisonerVining", "Vining"));
    }

    public void EndVining()
    {
        _isVining = false;
        _vineTimer = 0f;
        Button?.SetCooldownFill(0f);
        if (Button != null)
            Button.cooldownTimerText.gameObject.SetActive(false);
    }

    public override void ResetCooldownAndOrEffect()
    {
        EndSeeking(false);
        EndVining();
        base.ResetCooldownAndOrEffect();
    }

    public static void SetOwnCooldown()
    {
        var instance = CustomButtonSingleton<PoisonerVineButton>.Instance;
        if (instance != null)
        {
            instance.Timer = instance.Cooldown;
        }
    }

    public override void OnEffectEnd()
    {
        // No effect end action required for PoisonerVineButton
    }
}
