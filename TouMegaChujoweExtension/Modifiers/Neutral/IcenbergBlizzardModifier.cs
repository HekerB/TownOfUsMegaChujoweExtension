using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Modifiers;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using MiraAPI.Utilities;
using UnityEngine;
using System;

namespace TouMegaChujoweExtension.Modifiers;

public sealed class IcenbergBlizzardModifier(float duration) : TimedModifier
{
    public override string ModifierName => TouLocale.Get("ExtensionModifierIcenbergBlizzard", "Blizzard");
    public override bool HideOnUi => false;
    public override LoadableAsset<Sprite>? ModifierIcon => TouExtensionNeuAssets.BlizzardButtonSprite;
    public override float Duration => duration;

    private float SpeedCache { get; set; }

    public override void OnDeath(DeathReason reason)
    {
        Player.RemoveModifier(this);
    }

    public override void OnActivate()
    {
        if (Player != null)
        {
            SpeedCache = Player.MyPhysics.Speed;
            Player.MyPhysics.Speed *= 0.5f; // Slow down to 50% speed

            if (Player.AmOwner)
            {
                var msg = TouLocale.Get("ExtensionRoleIcenbergBlizzardNotification", "A blizzard has started!");
                var notif = Helpers.CreateAndShowNotification(
                    msg,
                    TouExtensionColors.Icenberg,
                    new Vector3(0f, 1f, -20f),
                    spr: TouMegaChujoweExtension.Assets.TouExtensionNeuAssets.BlizzardButtonSprite.LoadAsset());
                notif.AdjustNotification();

                IcenbergBlizzardOverlay.Show();
            }
        }
    }

    public override void OnDeactivate()
    {
        if (Player != null)
        {
            Player.MyPhysics.Speed = SpeedCache;

            if (Player.AmOwner)
            {
                IcenbergBlizzardOverlay.Hide();
            }
        }
    }
}

public static class IcenbergBlizzardOverlay
{
    private static GameObject? _overlayRoot;
    private static SpriteRenderer? _stormOverlay;
    private static IcenbergBlizzardOverlayAnimator? _animator;

    public static void Show()
    {
        if (_overlayRoot == null)
        {
            CreateOverlay();
        }

        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(true);
            if (_stormOverlay != null)
            {
                _stormOverlay.gameObject.SetActive(true);
                _stormOverlay.enabled = true;
            }
        }
    }

    public static void Hide()
    {
        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(false);
        }
    }

    private static void CreateOverlay()
    {
        if (HudManager.Instance == null || HudManager.Instance.FullScreen == null)
        {
            return;
        }

        _overlayRoot = new GameObject("IcenbergBlizzardOverlayRoot");
        _overlayRoot.transform.SetParent(HudManager.Instance.FullScreen.transform.parent, false);
        _overlayRoot.transform.localPosition = HudManager.Instance.FullScreen.transform.localPosition;
        _overlayRoot.transform.localScale = Vector3.one;

        _stormOverlay = UnityEngine.Object.Instantiate(HudManager.Instance.FullScreen, _overlayRoot.transform);
        _stormOverlay.name = "IcenbergBlizzardStormOverlay";
        _stormOverlay.transform.localPosition = new Vector3(0f, 0f, -6f);
        _stormOverlay.transform.localRotation = Quaternion.identity;
        _stormOverlay.transform.localScale = HudManager.Instance.FullScreen.transform.localScale;
        _stormOverlay.gameObject.SetActive(true);
        _stormOverlay.enabled = true;
        _stormOverlay.sprite = TouExtensionNeuAssets.IcenbergStormOverlay.LoadAsset();
        _stormOverlay.drawMode = SpriteDrawMode.Sliced;
        _stormOverlay.color = new Color(1f, 1f, 1f, 0.4f);
        _stormOverlay.maskInteraction = SpriteMaskInteraction.None;
        _stormOverlay.sortingLayerID = HudManager.Instance.FullScreen.sortingLayerID;
        _stormOverlay.sortingOrder += 65;
        
        if (_stormOverlay.material != null)
        {
            _stormOverlay.material = new Material(_stormOverlay.material)
            {
                renderQueue = 5100
            };
        }

        _animator = _overlayRoot.AddComponent<IcenbergBlizzardOverlayAnimator>();
        _animator.Initialize(_stormOverlay);

        _overlayRoot.SetActive(false);
    }
}

public sealed class IcenbergBlizzardOverlayAnimator : MonoBehaviour
{
    private SpriteRenderer? _stormOverlay;
    private float _pulse;
    private float _baseShakeAmount;
    private float _baseShakePeriod;
    private Vector3 _baseScale = Vector3.one;

    public void Initialize(SpriteRenderer stormOverlay)
    {
        _stormOverlay = stormOverlay;
        _baseScale = stormOverlay.transform.localScale;

        if (HudManager.Instance != null)
        {
            _baseShakeAmount = HudManager.Instance.PlayerCam.shakeAmount;
            _baseShakePeriod = HudManager.Instance.PlayerCam.shakePeriod;
        }
    }

    private void Update()
    {
        if (_stormOverlay == null)
        {
            return;
        }

        var dt = Time.unscaledDeltaTime;
        _pulse += dt;
        var storm = Mathf.Sin(_pulse * 5.1f);
        var flash = Mathf.Sin(_pulse * 11.5f);
        var stormAlpha = 0.20f + Mathf.Abs(storm) * 0.15f + Mathf.Max(0f, flash) * 0.08f;

        _stormOverlay.color = new Color(1f, 1f, 1f, Mathf.Clamp(stormAlpha, 0.18f, 0.5f));

        var scalePulseX = 1f + Mathf.Sin(_pulse * 2.6f) * 0.05f;
        var scalePulseY = 1f + Mathf.Cos(_pulse * 2.1f) * 0.045f;
        _stormOverlay.transform.localScale = Vector3.Scale(_baseScale, new Vector3(scalePulseX, scalePulseY, 1f));
        _stormOverlay.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_pulse * 1.8f) * 2.2f);

        if (HudManager.Instance != null)
        {
            HudManager.Instance.PlayerCam.shakeAmount = Mathf.Max(_baseShakeAmount, 0.01f + Mathf.Abs(storm) * 0.015f);
            HudManager.Instance.PlayerCam.shakePeriod = 12f;
        }
    }

    private void OnDisable()
    {
        if (_stormOverlay != null)
        {
            _stormOverlay.transform.localScale = _baseScale;
            _stormOverlay.transform.localRotation = Quaternion.identity;
        }

        if (HudManager.Instance != null)
        {
            HudManager.Instance.PlayerCam.shakeAmount = _baseShakeAmount;
            HudManager.Instance.PlayerCam.shakePeriod = _baseShakePeriod;
        }
    }
}
