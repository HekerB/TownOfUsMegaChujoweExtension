using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Modifiers;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Modifiers;

public sealed class IcenbergFrozenModifier(float duration) : DisabledModifier
{
    public override string ModifierName => TouLocale.Get("ExtensionModifierIcenbergFrozen", "Frozen");
    public override bool HideOnUi => false;
    public override LoadableAsset<Sprite>? ModifierIcon => TouExtensionIcons.IcenbergRoleIcon;
    public override float Duration => duration;
    public override bool AutoStart => true;
    public override bool CanUseAbilities => false;
    public override bool CanUseConsoles => false;
    public override bool CanOpenMap => false;
    public override bool CanReport => false;
    public override bool CanBeInteractedWith => true;
    public override bool IsConsideredAlive => true;

    public override string GetDescription()
    {
        var seconds = Mathf.CeilToInt(Mathf.Max(0f, TimeRemaining));
        return TouLocale.Get(
                "ExtensionModifierIcenbergFrozenTabDescription",
                "Icenberg chosen you\nFreeze: <seconds>s")
            .Replace("<seconds>", seconds.ToString());
    }

    public override void OnActivate()
    {
        if (Player != null && Player.AmOwner)
        {
            Player.NetTransform?.Halt();
            Player.moveable = false;
            IcenbergIceOverlay.Show();
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (Player == null || !Player.AmOwner)
        {
            return;
        }

        Player.NetTransform?.Halt();
        Player.moveable = false;
        IcenbergIceOverlay.Show();
    }

    public override void OnDeactivate()
    {
        if (Player != null && Player.AmOwner)
        {
            Player.moveable = true;
            IcenbergIceOverlay.Hide();
        }
    }

    public override bool? CanVent()
    {
        return false;
    }
}

public static class IcenbergIceOverlay
{
    private const int BlueOverlaySortingOffset = 40;
    private const int StormOverlaySortingOffset = 65;

    private static GameObject? _overlayRoot;
    private static SpriteRenderer? _blueBackground;
    private static SpriteRenderer? _stormOverlay;
    private static IcenbergOverlayAnimator? _animator;

    public static void Show()
    {
        if (_overlayRoot == null)
        {
            CreateOverlay();
        }

        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(true);
            if (_blueBackground != null)
            {
                _blueBackground.gameObject.SetActive(true);
                _blueBackground.enabled = true;
            }

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

        _overlayRoot = new GameObject("IcenbergIceOverlayRoot");
        _overlayRoot.transform.SetParent(HudManager.Instance.FullScreen.transform.parent, false);
        _overlayRoot.transform.localPosition = HudManager.Instance.FullScreen.transform.localPosition;
        _overlayRoot.transform.localScale = Vector3.one;

        _blueBackground = Object.Instantiate(HudManager.Instance.FullScreen, _overlayRoot.transform);
        _blueBackground.name = "IcenbergFrozenBlueBackground";
        _blueBackground.transform.localPosition = new Vector3(0f, 0f, -5f);
        _blueBackground.transform.localRotation = Quaternion.identity;
        _blueBackground.transform.localScale = HudManager.Instance.FullScreen.transform.localScale;
        _blueBackground.gameObject.SetActive(true);
        _blueBackground.enabled = true;
        _blueBackground.color = new Color(TouExtensionColors.Icenberg.r, TouExtensionColors.Icenberg.g, TouExtensionColors.Icenberg.b, 0.34f);
        _blueBackground.maskInteraction = SpriteMaskInteraction.None;
        _blueBackground.sortingLayerID = HudManager.Instance.FullScreen.sortingLayerID;
        _blueBackground.sortingOrder += BlueOverlaySortingOffset;
        PromoteOverlayMaterial(_blueBackground, 5000);

        _stormOverlay = Object.Instantiate(HudManager.Instance.FullScreen, _overlayRoot.transform);
        _stormOverlay.name = "IcenbergFrozenStormOverlay";
        _stormOverlay.transform.localPosition = new Vector3(0f, 0f, -6f);
        _stormOverlay.transform.localRotation = Quaternion.identity;
        _stormOverlay.transform.localScale = HudManager.Instance.FullScreen.transform.localScale;
        _stormOverlay.gameObject.SetActive(true);
        _stormOverlay.enabled = true;
        _stormOverlay.sprite = TouExtensionNeuAssets.IcenbergStormOverlay.LoadAsset();
        _stormOverlay.drawMode = SpriteDrawMode.Sliced;
        _stormOverlay.color = new Color(1f, 1f, 1f, 0.5f);
        _stormOverlay.maskInteraction = SpriteMaskInteraction.None;
        _stormOverlay.sortingLayerID = HudManager.Instance.FullScreen.sortingLayerID;
        _stormOverlay.sortingOrder += StormOverlaySortingOffset;
        PromoteOverlayMaterial(_stormOverlay, 5100);

        _animator = _overlayRoot.AddComponent<IcenbergOverlayAnimator>();
        _animator.Initialize(_blueBackground, _stormOverlay);

        _overlayRoot.SetActive(false);
    }

    private static void PromoteOverlayMaterial(SpriteRenderer renderer, int renderQueue)
    {
        if (renderer.material == null)
        {
            return;
        }

        renderer.material = new Material(renderer.material)
        {
            renderQueue = renderQueue
        };
    }
}

public sealed class IcenbergOverlayAnimator : MonoBehaviour
{
    private SpriteRenderer? _blueBackground;
    private SpriteRenderer? _stormOverlay;
    private float _pulse;
    private float _baseShakeAmount;
    private float _baseShakePeriod;
    private Vector3 _baseScale = Vector3.one;

    public void Initialize(SpriteRenderer blueBackground, SpriteRenderer stormOverlay)
    {
        _blueBackground = blueBackground;
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
        if (_blueBackground == null || _stormOverlay == null)
        {
            return;
        }

        var dt = Time.unscaledDeltaTime;
        _pulse += dt;
        var bg = TouExtensionColors.Icenberg;
        var storm = Mathf.Sin(_pulse * 5.1f);
        var flash = Mathf.Sin(_pulse * 11.5f);
        var blueAlpha = 0.24f + Mathf.Abs(Mathf.Sin(_pulse * 2.8f)) * 0.10f;
        var stormAlpha = 0.30f + Mathf.Abs(storm) * 0.18f + Mathf.Max(0f, flash) * 0.10f;

        _blueBackground.color = new Color(bg.r, bg.g, bg.b, Mathf.Clamp(blueAlpha, 0.22f, 0.38f));
        _stormOverlay.color = new Color(1f, 1f, 1f, Mathf.Clamp(stormAlpha, 0.28f, 0.68f));

        var scalePulseX = 1f + Mathf.Sin(_pulse * 2.6f) * 0.05f;
        var scalePulseY = 1f + Mathf.Cos(_pulse * 2.1f) * 0.045f;
        _stormOverlay.transform.localScale = Vector3.Scale(_baseScale, new Vector3(scalePulseX, scalePulseY, 1f));
        _stormOverlay.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_pulse * 1.8f) * 2.2f);

        if (HudManager.Instance != null)
        {
            HudManager.Instance.PlayerCam.shakeAmount = Mathf.Max(_baseShakeAmount, 0.02f + Mathf.Abs(storm) * 0.03f);
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
