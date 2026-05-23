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

public sealed class SniperShootButton : TownOfUsRoleButton<SniperRole>
{
    private const float CancelLockDuration = 2f;
    public override string Name => TouLocale.GetParsed("ExtensionRoleSniperShoot", "Snipe");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => Palette.ImpostorRed;
    public override float Cooldown => GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown;
    public override float EffectDuration => 0f;
    public override int MaxUses => 0;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.SniperShootButtonSprite;
    public override bool ZeroIsInfinite { get; set; } = true;

    private IEnumerator? _activeZoomCoroutine;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        if (Button != null)
        {
            var aspect = Button.GetComponent<AspectPosition>();
            if (aspect != null)
            {
                aspect.Alignment = AspectPosition.EdgeAlignments.LeftBottom;
                aspect.DistanceFromEdge = new Vector3(0.6f, 1.8f, -1f);
                aspect.AdjustPosition();
            }
        }
    }

    private PlayerControl? _lastOutlined;
    private bool _isAimingLocal;
    private float _aimTimer;
    private float _aimDuration;
    private float _cancelUnlockTime;

    private bool CanCancelAimingNow =>
        OptionGroupSingleton<SniperOptions>.Instance.CanCancelAiming &&
        Time.time >= _cancelUnlockTime;

    public override bool CanUse()
    {
        if (MeetingHud.Instance || HudManager.Instance.Chat.IsOpenOrOpening) return false;

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied()) return false;
        if (player.inVent) return false;

        if (SniperSystem.IsAiming)
        {
            return _isAimingLocal && CanCancelAimingNow;
        }

        return true;
    }

    public override bool CanClick()
    {
        if (MeetingHud.Instance || HudManager.Instance.Chat.IsOpenOrOpening) return false;

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied()) return false;

        if (_isAimingLocal)
        {
            return CanCancelAimingNow;
        }

        return CanUse() && Timer <= 0f;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        if (MeetingHud.Instance)
        {
            if (_isAimingLocal) EndAiming(false);
            base.FixedUpdate(playerControl);
            return;
        }

        if (playerControl == null || !playerControl.IsRole<SniperRole>())
        {
            ClearOutline();
            if (_isAimingLocal) EndAiming(false);
            if (playerControl != null) base.FixedUpdate(playerControl);
            return;
        }

        // === Aiming countdown phase ===
        if (_isAimingLocal)
        {
            _aimTimer -= Time.fixedDeltaTime;

            if (_aimTimer <= 0f)
            {
                EndAiming(true);
                ShowAimingPenaltyNotification(
                    TouLocale.Get("SniperAimingExpiredPenalty", "Aiming expired! Penalty cooldown applied."));
            }
            else
            {
                Timer = -1f;

                if (Button != null)
                {
                    if (CanCancelAimingNow)
                    {
                        Button.SetEnabled();
                    }
                    else
                    {
                        Button.SetDisabled();
                    }
                    Button.SetFillUp(_aimTimer, _aimDuration);
                    Button.cooldownTimerText.text = Mathf.CeilToInt(_aimTimer).ToString();
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
            // Red outline for Sniper scope
            target.cosmetics.SetOutline(true,
                new Il2CppSystem.Nullable<Color>(new Color(0.8f, 0.1f, 0.1f)));
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

        if (_isAimingLocal)
        {
            if (CanCancelAimingNow)
            {
                EndAiming(true);
                ShowAimingPenaltyNotification(
                    TouLocale.Get("SniperAimingCancelledPenalty", "Aiming cancelled! Penalty cooldown applied."));
            }
            return;
        }

        // Enter aiming mode
        _aimTimer = OptionGroupSingleton<SniperOptions>.Instance.AimDuration;
        _aimDuration = _aimTimer;
        _isAimingLocal = true;
        SniperSystem.IsAiming = true;
        SniperSystem.StartAimingFrame = UnityEngine.Time.frameCount;
        _cancelUnlockTime = Time.time + CancelLockDuration;

        OverrideSprite(TouExtensionImpAssets.SniperShootButtonSprite.LoadAsset());
        OverrideName(TouLocale.GetParsed("ExtensionRoleSniperAiming", "Aiming"));

        if (OptionGroupSingleton<SniperOptions>.Instance.AimZoomEnabled)
        {
            DoZoomOut();
        }
    }

    private static void ShowAimingPenaltyNotification(string message)
    {
        MiraAPI.Utilities.Helpers.CreateAndShowNotification(
            $"<b><color=#{ColorUtility.ToHtmlStringRGBA(Palette.ImpostorRed)}>{message}</color></b>",
            Color.white,
            new Vector3(0f, 1.5f, -20f),
            spr: TouExtensionIcons.SniperRoleIcon.LoadAsset()
        );
    }

    public void EndAiming(bool putOnCooldown)
    {
        _isAimingLocal = false;
        SniperSystem.IsAiming = false;
        ClearOutline();
        OverrideSprite(TouExtensionImpAssets.SniperShootButtonSprite.LoadAsset());
        OverrideName(TouLocale.GetParsed("ExtensionRoleSniperShoot", "Snipe"));

        DoZoomIn();

        if (putOnCooldown)
        {
            Timer = Cooldown;
            var local = PlayerControl.LocalPlayer;
            if (local != null)
            {
                local.SetKillTimer(Cooldown);
            }
        }
        else
        {
            Timer = -1f;
        }
    }

    private void DoZoomOut()
    {
        if (_activeZoomCoroutine != null) Reactor.Utilities.Coroutines.Stop(_activeZoomCoroutine);
        _activeZoomCoroutine = ZoomOutCoroutine();
        Reactor.Utilities.Coroutines.Start(_activeZoomCoroutine);
    }

    private void DoZoomIn()
    {
        if (_activeZoomCoroutine != null) Reactor.Utilities.Coroutines.Stop(_activeZoomCoroutine);
        _activeZoomCoroutine = ZoomInCoroutine();
        Reactor.Utilities.Coroutines.Start(_activeZoomCoroutine);
    }

    private void ForceZoomIn()
    {
        if (_activeZoomCoroutine != null)
        {
            Reactor.Utilities.Coroutines.Stop(_activeZoomCoroutine);
            _activeZoomCoroutine = null;
        }

        foreach (var cam in Camera.allCameras)
            cam.orthographicSize = 3f;

        ResolutionManager.ResolutionChanged.Invoke(
            (float)Screen.width / Screen.height, Screen.width, Screen.height, Screen.fullScreen);
    }

    private IEnumerator ZoomOutCoroutine()
    {
        if (Camera.main == null) yield break;
        float elapsed = 0f;
        float duration = 0.5f;
        float startSize = Camera.main.orthographicSize;
        float endSize = OptionGroupSingleton<SniperOptions>.Instance.ZoomDistance;

        while (elapsed < duration)
        {
            if (!_isAimingLocal || MeetingHud.Instance) yield break;
            elapsed += Time.deltaTime;
            float currentSize = Mathf.Lerp(startSize, endSize, elapsed / duration);

            foreach (var cam in Camera.allCameras)
                cam.orthographicSize = currentSize;

            ResolutionManager.ResolutionChanged.Invoke(
                (float)Screen.width / Screen.height, Screen.width, Screen.height, Screen.fullScreen);
            yield return null;
        }

        foreach (var cam in Camera.allCameras)
            cam.orthographicSize = endSize;
    }

    private IEnumerator ZoomInCoroutine()
    {
        if (Camera.main == null) yield break;
        float elapsed = 0f;
        float duration = 0.5f;
        float startSize = Camera.main.orthographicSize;
        float endSize = 3.0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentSize = Mathf.Lerp(startSize, endSize, elapsed / duration);

            foreach (var cam in Camera.allCameras)
                cam.orthographicSize = currentSize;

            ResolutionManager.ResolutionChanged.Invoke(
                (float)Screen.width / Screen.height, Screen.width, Screen.height, Screen.fullScreen);
            yield return null;
        }

        foreach (var cam in Camera.allCameras)
            cam.orthographicSize = endSize;

        _activeZoomCoroutine = null;
    }

    public override void ResetCooldownAndOrEffect()
    {
        ForceZoomIn();
        EndAiming(false);
        base.ResetCooldownAndOrEffect();
    }

    public override void OnEffectEnd() { }
}
