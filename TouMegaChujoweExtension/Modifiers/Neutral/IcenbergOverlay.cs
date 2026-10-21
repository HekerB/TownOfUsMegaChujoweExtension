using System;
using Reactor.Utilities.Attributes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Modifiers;

/// <summary>
/// Full-screen ice effect for the local player: a blue tint (frozen only) under the storm texture.
/// Lives under the HUD, sized from the main camera every frame like TOU's Parasite overlay, so a new HUD,
/// a resolution change or zooming never leaves it the wrong size; it is rebuilt when the HUD is.
/// </summary>
public static class IcenbergOverlay
{
    private static GameObject? _root;
    private static HudManager? _hud;
    private static IcenbergOverlayAnimator? _animator;
    private static bool _frozen;
    private static bool _blizzard;

    public static void SetFrozen(bool active)
    {
        _frozen = active;
        Refresh();
    }

    public static void SetBlizzard(bool active)
    {
        _blizzard = active;
        Refresh();
    }

    public static void Clear()
    {
        _frozen = false;
        _blizzard = false;
        Refresh();
    }

    private static void Refresh()
    {
        var wanted = _frozen || _blizzard;
        if (!wanted)
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }

            return;
        }

        if (!Ensure())
        {
            return;
        }

        _animator!.Configure(_frozen);
        _root!.SetActive(true);
    }

    private static bool Ensure()
    {
        if (!HudManager.InstanceExists)
        {
            return false;
        }

        if (_root != null && _hud == HudManager.Instance)
        {
            return true;
        }

        if (_root != null)
        {
            Object.Destroy(_root);
        }

        _hud = HudManager.Instance;
        var layer = LayerMask.NameToLayer("UI");
        _root = new GameObject("IcenbergOverlay") { layer = layer };
        _root.transform.SetParent(_hud.transform, false);

        var tint = CreateRenderer("IcenbergOverlayTint", _hud.FullScreen != null ? _hud.FullScreen.sprite : null, 999, layer);
        var storm = CreateRenderer("IcenbergOverlayStorm", TouExtensionNeuAssets.IcenbergStormOverlay.LoadAsset(), 1000, layer);

        _animator = _root.AddComponent<IcenbergOverlayAnimator>();
        _animator.Initialize(tint, storm);
        _root.SetActive(false);
        return true;
    }

    private static SpriteRenderer CreateRenderer(string name, Sprite? sprite, int sortingOrder, int layer)
    {
        var go = new GameObject(name) { layer = layer };
        go.transform.SetParent(_root!.transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        renderer.maskInteraction = SpriteMaskInteraction.None;
        return renderer;
    }
}

[RegisterInIl2Cpp]
public sealed class IcenbergOverlayAnimator(IntPtr cppPtr) : MonoBehaviour(cppPtr)
{
    private const float CoverFactor = 1.42f;

    private SpriteRenderer? _tint;
    private SpriteRenderer? _storm;
    private bool _frozen;
    private float _time;
    private float _baseShakeAmount;
    private float _baseShakePeriod;
    private bool _shaking;

    public void Initialize(SpriteRenderer tint, SpriteRenderer storm)
    {
        _tint = tint;
        _storm = storm;
    }

    public void Configure(bool frozen)
    {
        _frozen = frozen;
        if (_tint != null)
        {
            _tint.enabled = frozen && _tint.sprite != null;
        }

        if (!frozen)
        {
            StopShake();
        }
    }

    private void Update()
    {
        if (_storm == null || _tint == null)
        {
            return;
        }

        // Nothing of the effect shows over a meeting or the end screen.
        var hidden = MeetingHud.Instance != null || ExileController.Instance != null;
        _storm.enabled = !hidden && _storm.sprite != null;
        _tint.enabled = !hidden && _frozen && _tint.sprite != null;
        if (hidden)
        {
            StopShake();
            return;
        }

        _time += Time.unscaledDeltaTime;
        var storm = Mathf.Abs(Mathf.Sin(_time * 5.1f));
        var flash = Mathf.Max(0f, Mathf.Sin(_time * 11.5f));

        var ice = TouExtensionColors.Icenberg;
        _tint.color = new Color(ice.r, ice.g, ice.b, 0.24f + Mathf.Abs(Mathf.Sin(_time * 2.8f)) * 0.1f);
        var stormAlpha = _frozen ? 0.3f + storm * 0.18f + flash * 0.1f : 0.2f + storm * 0.15f + flash * 0.08f;
        _storm.color = new Color(1f, 1f, 1f, Mathf.Clamp(stormAlpha, 0.18f, 0.68f));

        Layout(new Vector2(1f + Mathf.Sin(_time * 2.6f) * 0.05f, 1f + Mathf.Cos(_time * 2.1f) * 0.045f));
        _storm.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_time * 1.8f) * 2.2f);

        if (_frozen && HudManager.InstanceExists && HudManager.Instance.PlayerCam != null)
        {
            var cam = HudManager.Instance.PlayerCam;
            if (!_shaking)
            {
                _baseShakeAmount = cam.shakeAmount;
                _baseShakePeriod = cam.shakePeriod;
                _shaking = true;
            }

            cam.shakeAmount = Mathf.Max(_baseShakeAmount, 0.02f + storm * 0.03f);
            cam.shakePeriod = 12f;
        }
    }

    private void Layout(Vector2 pulse)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        var bottomLeft = cam.ScreenToWorldPoint(new Vector3(0f, 0f, cam.nearClipPlane));
        var topRight = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, cam.nearClipPlane));
        var z = HudManager.Instance.FullScreen != null ? HudManager.Instance.FullScreen.transform.position.z + 1f : 0f;
        transform.position = new Vector3((bottomLeft.x + topRight.x) * 0.5f, (bottomLeft.y + topRight.y) * 0.5f, z);
        transform.localScale = Vector3.one;

        var width = Mathf.Abs(topRight.x - bottomLeft.x) * CoverFactor;
        var height = Mathf.Abs(topRight.y - bottomLeft.y) * CoverFactor;
        Fit(_tint!, width, height, Vector2.one);
        Fit(_storm!, width, height, pulse);
    }

    private static void Fit(SpriteRenderer renderer, float width, float height, Vector2 pulse)
    {
        if (renderer.sprite == null)
        {
            return;
        }

        var size = renderer.sprite.bounds.size;
        if (size.x <= 0f || size.y <= 0f)
        {
            return;
        }

        var parentScale = renderer.transform.parent != null ? renderer.transform.parent.lossyScale : Vector3.one;
        renderer.transform.localScale = new Vector3(
            width / size.x / Mathf.Max(parentScale.x, 0.0001f) * pulse.x,
            height / size.y / Mathf.Max(parentScale.y, 0.0001f) * pulse.y,
            1f);
    }

    private void StopShake()
    {
        if (!_shaking)
        {
            return;
        }

        _shaking = false;
        if (HudManager.InstanceExists && HudManager.Instance.PlayerCam != null)
        {
            HudManager.Instance.PlayerCam.shakeAmount = _baseShakeAmount;
            HudManager.Instance.PlayerCam.shakePeriod = _baseShakePeriod;
        }
    }

    private void OnDisable()
    {
        StopShake();
    }
}
