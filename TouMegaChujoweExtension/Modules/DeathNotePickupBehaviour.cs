using System;
using Il2CppInterop.Runtime.Attributes;
using Reactor.Utilities.Attributes;
using TouMegaChujoweExtension.Modifiers.Neutral;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Modules;

[RegisterInIl2Cpp]
public class DeathNotePickupBehaviour : MonoBehaviour
{
public static readonly List<DeathNotePickupBehaviour> Instances = new();
    private DeathNoteModifier? _modifier;
    private SpriteRenderer? _renderer;
    private const float PickupRange = 0.75f;
    private const float FadeSpeed = 12f;
    private const bool OwnerOnly = true;
    private float _currentAlpha = 0f;
    private bool _renderConfigured = false;

    public DeathNotePickupBehaviour(IntPtr ptr) : base(ptr) { }

    [HideFromIl2Cpp]
    private void Log(string msg)
    {
    }

    [HideFromIl2Cpp]
    private SpriteRenderer PickBestPlayerWorldRenderer(PlayerControl local)
    {
        if (local == null) return null;
        var direct = local.GetComponent<SpriteRenderer>();
        if (direct != null) return direct;

        var rends = local.GetComponentsInChildren<SpriteRenderer>(true);
        if (rends == null || rends.Length == 0) return null;

        SpriteRenderer best = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < rends.Length; i++)
        {
            var r = rends[i];
            if (r == null) continue;
            int score = 0;
            if (r.maskInteraction != SpriteMaskInteraction.None) score += 100;
            var sl = r.sortingLayerName ?? string.Empty;
            if (sl.Contains("Player", StringComparison.OrdinalIgnoreCase)) score += 50;
            if (sl.Contains("UI", StringComparison.OrdinalIgnoreCase)) score -= 200;

            var nm = r.name ?? string.Empty;

            if (nm.Contains("Body", StringComparison.OrdinalIgnoreCase)) score += 40;
            if (nm.Contains("Sprite", StringComparison.OrdinalIgnoreCase)) score += 20;
            if (nm.Contains("Name", StringComparison.OrdinalIgnoreCase)) score -= 100;

            score += Mathf.Clamp(r.sortingOrder, -50, 50);

            if (score > bestScore)
            {
                bestScore = score;
                best = r;
            }
        }

        return best;
    }

    [HideFromIl2Cpp]
    private void CopyRenderSettingsFrom(SpriteRenderer source)
    {
        if (_renderer == null || source == null) return;

        _renderer.maskInteraction = source.maskInteraction;


        _renderer.sortingLayerID = source.sortingLayerID;
        _renderer.sortingOrder = source.sortingOrder; 


        gameObject.layer = source.gameObject.layer;
    }

    [HideFromIl2Cpp]
    private bool TryConfigureWorldRenderingOnce()
    {
        if (_renderer == null) return false;


        if (ShipStatus.Instance != null && transform.parent != ShipStatus.Instance.transform)
            transform.SetParent(ShipStatus.Instance.transform, true);

        var local = PlayerControl.LocalPlayer;

        if (local != null)
        {
            var pr = PickBestPlayerWorldRenderer(local);
            if (pr != null)
            {
                CopyRenderSettingsFrom(pr);

                Log($"Configured from PLAYER: mask={_renderer.maskInteraction} " +
                    $"sorting={_renderer.sortingLayerName}:{_renderer.sortingOrder} " +
                    $"layer={LayerMask.LayerToName(gameObject.layer)} source={pr.name}/{pr.sortingLayerName}:{pr.sortingOrder}");

                return true;
            }
        }


        var vent = Object.FindObjectOfType<Vent>();
        if (vent != null)
        {
            var vr = vent.GetComponent<SpriteRenderer>();
            if (vr != null)
            {
                CopyRenderSettingsFrom(vr);
                Log($"Configured from VENT: mask={_renderer.maskInteraction} sorting={_renderer.sortingLayerName}:{_renderer.sortingOrder}");
                return true;
            }
        }

        return false;
    }

    [HideFromIl2Cpp]
    public void Initialize(DeathNoteModifier mod)
    {
        if (!Instances.Contains(this))
            Instances.Add(this);
        _modifier = mod;

        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer == null) return;

        _renderer.enabled = true;
        _currentAlpha = 0f;
        _renderer.color = new Color(1f, 1f, 1f, 0f);

  
        _renderConfigured = TryConfigureWorldRenderingOnce();
    }

    [HideFromIl2Cpp]
    public bool IsInRange()
    {
        if (_modifier == null || _modifier.Player == null) return false;
        if (OwnerOnly && !_modifier.Player.AmOwner) return false;
        if (_modifier.IsUsed) return false;

        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null) return false;
        if (local.Data.IsDead) return false;

        Vector2 playerPos = local.GetTruePosition();
        Vector2 notePos = transform.position;

        return Vector2.Distance(playerPos, notePos) <= PickupRange;
    }

    private void Update()
    {
        if (_renderer == null) return;

        if (!_renderConfigured)
            _renderConfigured = TryConfigureWorldRenderingOnce();
       
        if (!_renderConfigured)
            _renderer.maskInteraction = SpriteMaskInteraction.None;

        if (_modifier == null || _modifier.IsUsed)
        {
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, 0f, FadeSpeed * Time.deltaTime);
            _renderer.color = new Color(1f, 1f, 1f, _currentAlpha);
            return;
        }

        float targetAlpha = ShouldBeVisibleForLocal() ? 1f : 0f;

        _currentAlpha = Mathf.Lerp(_currentAlpha, targetAlpha, FadeSpeed * Time.deltaTime);
        if (_currentAlpha < 0.01f) _currentAlpha = 0f;

        _renderer.color = new Color(1f, 1f, 1f, _currentAlpha);
    }

    private bool ShouldBeVisibleForLocal()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null) return false;

        if (_modifier == null || _modifier.Player == null) return false;

        if (OwnerOnly && !_modifier.Player.AmOwner) return false;

        if (_modifier.IsUsed) return false;

        return true;
    }

    private void OnDestroy()
    {
        Instances.Remove(this);
        _modifier = null;
    }
}
