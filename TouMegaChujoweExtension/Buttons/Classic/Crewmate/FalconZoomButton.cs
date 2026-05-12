using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using System.Collections;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Crewmate;

public sealed class FalconZoomButton : TownOfUsRoleButton<FalconRole>
{
    private bool _isZoomed;
    private float _zoomTimer;
    private IEnumerator? _activeCoroutine;
    private int _lastClickFrame = -1;

    public override string Name => TouLocale.GetParsed("ExtensionRoleFalconZoom", "Zoom Out");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Falcon;
    public override float Cooldown => OptionGroupSingleton<FalconOptions>.Instance.ZoomCooldown + MapCooldown;
    public override float EffectDuration => 0;
    public override int MaxUses => (int)OptionGroupSingleton<FalconOptions>.Instance.MaxUses;
    public override LoadableAsset<Sprite> Sprite => TouExtensionCrewAssets.ZoomOutButtonSprite;
    public override bool ZeroIsInfinite { get; set; } = true;

    private float ZoomDuration => OptionGroupSingleton<FalconOptions>.Instance.ZoomDuration;

    public override bool CanUse() => _isZoomed || CanActivate();
    public override bool CanClick() => _isZoomed || CanActivate();

    private bool CanActivate()
    {
        if (PlayerControl.LocalPlayer == null) return false;
        if (PlayerControl.LocalPlayer.HasDied()) return false;
        if (MeetingHud.Instance) return false;
        if (HudManager.Instance.Chat.IsOpenOrOpening) return false;
        if (IsLightsSabotaged()) return false;
        if (Timer > 0) return false;
        if (LimitedUses && UsesLeft <= 0) return false;
        return true;
    }

    protected override void OnClick()
    {
        if (Time.frameCount == _lastClickFrame) return;
        _lastClickFrame = Time.frameCount;

        if (_isZoomed)
        {
            DoZoomIn();
            return;
        }

        if (!CanActivate()) return;

        DoZoomOut();
    }

    private void DoZoomOut()
    {
        if (_isZoomed) return;

        if (LimitedUses)
        {
            UsesLeft--;
            Button?.SetUsesRemaining(UsesLeft);
        }

        _isZoomed = true;
        _zoomTimer = ZoomDuration;

        if (_activeCoroutine != null) Coroutines.Stop(_activeCoroutine);
        _activeCoroutine = ZoomOutCoroutine();
        Coroutines.Start(_activeCoroutine);
    }

    private void DoZoomIn()
    {
        if (!_isZoomed) return;

        _isZoomed = false;
        _zoomTimer = 0;
        Timer = Cooldown;

        if (_activeCoroutine != null) Coroutines.Stop(_activeCoroutine);
        _activeCoroutine = ZoomInCoroutine();
        Coroutines.Start(_activeCoroutine);
    }

    private bool IsLightsSabotaged()
    {
        if (ShipStatus.Instance == null || ShipStatus.Instance.Systems == null) return false;
        if (ShipStatus.Instance.Systems.ContainsKey(SystemTypes.Electrical))
        {
            var electrical = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
            return electrical.ActualSwitches != electrical.ExpectedSwitches;
        }
        return false;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (_isZoomed)
        {
            if (MeetingHud.Instance || IsLightsSabotaged())
            {
                ForceReset();
                return;
            }

            _zoomTimer -= Time.fixedDeltaTime;
            if (_zoomTimer <= 0)
            {
                DoZoomIn();
            }
            else
            {
                Timer = -1f; // Lock cooldown while zoomed

                if (Button != null)
                {
                    Button.SetEnabled();
                    Button.SetFillUp(_zoomTimer, ZoomDuration);
                    Button.cooldownTimerText.text = Mathf.CeilToInt(_zoomTimer).ToString();
                    Button.cooldownTimerText.gameObject.SetActive(true);
                }
            }
        }

        if (!_isZoomed && Button != null)
        {
            OverrideName(TouLocale.GetParsed("ExtensionRoleFalconZoom", "Zoom Out"));
            OverrideSprite(TouExtensionCrewAssets.ZoomOutButtonSprite.LoadAsset());
        }
        else if (_isZoomed && Button != null)
        {
            OverrideName(TouLocale.GetParsed("ExtensionRoleFalconZoomIn", "Zoom In"));
            OverrideSprite(TouExtensionCrewAssets.ZoomInButtonSprite.LoadAsset());
        }
    }

    public void ForceReset()
    {
        if (!_isZoomed) return;

        _isZoomed = false;
        _zoomTimer = 0;
        Timer = Cooldown;

        if (_activeCoroutine != null)
        {
            Coroutines.Stop(_activeCoroutine);
            _activeCoroutine = null;
        }
        
        foreach (var cam in Camera.allCameras)
            cam.orthographicSize = 3f;

        if (HudManager.Instance != null && HudManager.Instance.ShadowQuad != null)
            HudManager.Instance.ShadowQuad.gameObject.SetActive(true);

        ResolutionManager.ResolutionChanged.Invoke(
            (float)Screen.width / Screen.height, Screen.width, Screen.height, Screen.fullScreen);
    }

    private IEnumerator ZoomOutCoroutine()
    {
        if (HudManager.Instance != null && HudManager.Instance.ShadowQuad != null)
            HudManager.Instance.ShadowQuad.gameObject.SetActive(false);
            
        float elapsed = 0f;
        float duration = 0.5f;
        float startSize = Camera.main.orthographicSize;
        float endSize = OptionGroupSingleton<FalconOptions>.Instance.ZoomDistance;

        while (elapsed < duration)
        {
            if (!_isZoomed || MeetingHud.Instance) yield break;
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

        if (HudManager.Instance != null && HudManager.Instance.ShadowQuad != null)
            HudManager.Instance.ShadowQuad.gameObject.SetActive(true);
            
        _activeCoroutine = null;
    }

    public override void ResetCooldownAndOrEffect()
    {
        ForceReset();
        base.ResetCooldownAndOrEffect();
    }
}