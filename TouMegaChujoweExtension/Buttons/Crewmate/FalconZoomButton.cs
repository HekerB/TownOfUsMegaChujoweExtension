using System.Collections;
using System.Globalization;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Buttons;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Crewmate;

public sealed class FalconZoomButton : TownOfUsRoleButton<FalconRole>
{
    private bool _isZoomed;
    private float _zoomTimer;
    private float _cooldownTimer;
    private IEnumerator? _activeCoroutine;
    private int _lastClickFrame = -1;
    private bool _wasMeeting;

    public override string Name => "Zoom Out";
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Falcon;
    public override float Cooldown => OptionGroupSingleton<FalconOptions>.Instance.ZoomCooldown;
    public override float EffectDuration => 0;
    public override int MaxUses => (int)OptionGroupSingleton<FalconOptions>.Instance.MaxUses;
    public override LoadableAsset<Sprite> Sprite => TouExtensionCrewAssets.ZoomOutButtonSprite;
    public override bool ZeroIsInfinite { get; set; } = true;

    private float ZoomDuration => OptionGroupSingleton<FalconOptions>.Instance.ZoomDuration;
    private float ZoomCooldown => OptionGroupSingleton<FalconOptions>.Instance.ZoomCooldown;

    public override bool CanUse() => true;
    public override bool CanClick() => true;

    private bool CanActivate()
    {
        if (PlayerControl.LocalPlayer == null) return false;
        if (PlayerControl.LocalPlayer.HasDied()) return false;
        if (MeetingHud.Instance) return false;
        if (HudManager.Instance.Chat.IsOpenOrOpening) return false;
        if (IsLightsSabotaged()) return false;
        if (_cooldownTimer > 0) return false;
        if (LimitedUses && UsesLeft <= 0) return false;
        return true;
    }

    public override void ClickHandler()
    {
        if (Time.frameCount == _lastClickFrame) return;
        _lastClickFrame = Time.frameCount;

        if (_isZoomed)
        {
            DoZoomIn();
            return;
        }

        if (!CanActivate()) return;

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

    protected override void OnClick() { }
    public override void OnEffectEnd() { }

    public override void FixedUpdateHandler(PlayerControl playerControl)
    {
        var inMeeting = (bool)MeetingHud.Instance;
        if (_wasMeeting && !inMeeting && !_isZoomed)
        {
            _cooldownTimer = ZoomCooldown;
        }
        _wasMeeting = inMeeting;

        if (_isZoomed)
        {
            _zoomTimer -= Time.deltaTime;
            if (_zoomTimer <= 0)
            {
                DoZoomIn();
            }
        }
        else if (_cooldownTimer > 0)
        {
            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer < 0) _cooldownTimer = 0;
        }

        if (_isZoomed && (IsLightsSabotaged() || MeetingHud.Instance))
        {
            DoZoomIn();
        }

        if (Button)
        {
            if (MeetingHud.Instance)
            {
                Button.gameObject.SetActive(false);
            }
            else
            {
                Button.gameObject.SetActive(
                    HudManager.Instance.UseButton.isActiveAndEnabled ||
                    HudManager.Instance.PetButton.isActiveAndEnabled);
            }

            if (_isZoomed || (_cooldownTimer <= 0 && CanActivate()))
            {
                Button.SetEnabled();
            }
            else
            {
                Button.SetDisabled();
            }

            if (_isZoomed)
            {
                Button.SetFillUp(_zoomTimer, ZoomDuration);
                Button.cooldownTimerText.text = Mathf.Ceil(_zoomTimer).ToString("0", NumberFormatInfo.InvariantInfo);
                Button.cooldownTimerText.gameObject.SetActive(true);
            }
            else if (_cooldownTimer > 0)
            {
                if (Button != null && Button.graphic != null)
                    Button.SetCoolDown(_cooldownTimer, ZoomCooldown);
            }
            else
            {
                if (Button != null && Button.graphic != null)
                    Button.SetCoolDown(0, ZoomCooldown);
            }
        }

        if (_isZoomed)
        {
            OverrideName(TouLocale.GetParsed("ExtensionRoleFalconZoomIn", "Zoom In"));
        }
        else
        {
            OverrideName(TouLocale.GetParsed("ExtensionRoleFalconZoom", "Zoom Out"));
        }
    }

    private void DoZoomIn()
    {
        if (!_isZoomed) return;

        _isZoomed = false;
        _zoomTimer = 0;
        _cooldownTimer = ZoomCooldown;

        if (_activeCoroutine != null) Coroutines.Stop(_activeCoroutine);
        _activeCoroutine = ZoomInCoroutine();
        Coroutines.Start(_activeCoroutine);
    }

    public void ForceReset()
    {
        if (!_isZoomed) return;

        _isZoomed = false;
        _zoomTimer = 0;
        _cooldownTimer = ZoomCooldown;

        if (_activeCoroutine != null)
        {
            Coroutines.Stop(_activeCoroutine);
            _activeCoroutine = null;
        }

        foreach (var cam in Camera.allCameras)
            cam.orthographicSize = 3f;

        if (HudManager.Instance != null)
            HudManager.Instance.ShadowQuad.gameObject.SetActive(true);

        ResolutionManager.ResolutionChanged.Invoke(
            (float)Screen.width / Screen.height, Screen.width, Screen.height, Screen.fullScreen);
    }

    private IEnumerator ZoomOutCoroutine()
    {
        HudManager.Instance.ShadowQuad.gameObject.SetActive(false);
        var zoomDistance = OptionGroupSingleton<FalconOptions>.Instance.ZoomDistance;

        for (var ft = Camera.main!.orthographicSize; ft < zoomDistance; ft += 0.3f)
        {
            if (!_isZoomed || MeetingHud.Instance)
                yield break;

            Camera.main.orthographicSize = ft;
            ResolutionManager.ResolutionChanged.Invoke(
                (float)Screen.width / Screen.height, Screen.width, Screen.height, Screen.fullScreen);

            foreach (var cam in Camera.allCameras)
                cam.orthographicSize = Camera.main.orthographicSize;

            yield return null;
        }

        if (_isZoomed && !MeetingHud.Instance)
        {
            foreach (var cam in Camera.allCameras)
                cam.orthographicSize = zoomDistance;

            ResolutionManager.ResolutionChanged.Invoke(
                (float)Screen.width / Screen.height, Screen.width, Screen.height, Screen.fullScreen);
        }
    }

    private IEnumerator ZoomInCoroutine()
    {
        for (var ft = Camera.main!.orthographicSize; ft > 3f; ft -= 0.3f)
        {
            Camera.main.orthographicSize = MeetingHud.Instance ? 3f : ft;
            ResolutionManager.ResolutionChanged.Invoke(
                (float)Screen.width / Screen.height, Screen.width, Screen.height, Screen.fullScreen);

            foreach (var cam in Camera.allCameras)
                cam.orthographicSize = Camera.main.orthographicSize;

            yield return null;
        }

        foreach (var cam in Camera.allCameras)
            cam.orthographicSize = 3f;

        if (HudManager.Instance != null)
            HudManager.Instance.ShadowQuad.gameObject.SetActive(true);

        ResolutionManager.ResolutionChanged.Invoke(
            (float)Screen.width / Screen.height, Screen.width, Screen.height, Screen.fullScreen);

        _activeCoroutine = null;
    }

    private static bool IsLightsSabotaged()
    {
        if (ShipStatus.Instance == null) return false;
        var systemType = ShipStatus.Instance.Systems;
        if (systemType == null || !systemType.ContainsKey(SystemTypes.Electrical)) return false;
        var electrical = systemType[SystemTypes.Electrical];
        if (electrical == null) return false;
        var switchSystem = electrical.TryCast<SwitchSystem>();
        return switchSystem != null && switchSystem.IsActive;
    }
}
