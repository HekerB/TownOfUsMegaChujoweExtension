using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Modules;

public static class GrimReaperSystem
{
    private const float SoulScale = 0.7f;

    public class ActiveSoul
    {
        public byte PlayerId;
        public Vector2 Position;
        public float RoundsRemaining;
        public GameObject? GameObject;
    }

    public static readonly Dictionary<byte, ActiveSoul> ActiveSouls = new();

    public static void ClearAll()
    {
        foreach (var soul in ActiveSouls.Values)
        {
            if (soul.GameObject != null)
            {
                UnityEngine.Object.Destroy(soul.GameObject);
            }
        }
        ActiveSouls.Clear();
    }

    public static bool HasReaperInGame()
    {
        return PlayerControl.AllPlayerControls.ToArray().Any(x => x != null && x.Data != null && x.Data.Role is GrimReaperRole);
    }

    public static void SpawnSoul(byte playerId, Vector2 position)
    {
        if (ActiveSouls.ContainsKey(playerId)) return;

        var options = OptionGroupSingleton<GrimReaperOptions>.Instance;
        var soul = new ActiveSoul
        {
            PlayerId = playerId,
            Position = position,
            RoundsRemaining = options.SoulDurationRounds,
            GameObject = new GameObject("GrimReaperSoul_" + playerId)
        };

        var sr = soul.GameObject.AddComponent<SpriteRenderer>();
        sr.sprite = TouExtensionAssets.ReaperSoulSprite.LoadAsset();
        sr.color = new Color(0.6f, 0.9f, 1f, 0.7f); // Translucent cyan glow

        ConfigureVisionMask(soul.GameObject, sr, out var zAxis);
        soul.GameObject.transform.position = new Vector3(position.x, position.y, zAxis);
        soul.GameObject.transform.localScale = new Vector3(SoulScale, SoulScale, 1f);

        // Add a simple update component to handle dynamic visibility and subtle floating animation
        var anim = soul.GameObject.AddComponent<SoulAnimator>();
        anim.SpriteRenderer = sr;
        anim.PlayerId = playerId;

        ActiveSouls[playerId] = soul;
    }

    public static void ReapSoul(byte playerId)
    {
        if (ActiveSouls.TryGetValue(playerId, out var soul))
        {
            if (soul.GameObject != null)
            {
                UnityEngine.Object.Destroy(soul.GameObject);
            }
            ActiveSouls.Remove(playerId);
        }
    }

    public static void OnMeetingStart()
    {
        var options = OptionGroupSingleton<GrimReaperOptions>.Instance;
        var toRemove = new List<byte>();

        foreach (var kvp in ActiveSouls)
        {
            var soul = kvp.Value;

            if (options.SoulsDisappearOnMeeting)
            {
                if (soul.GameObject != null)
                {
                    UnityEngine.Object.Destroy(soul.GameObject);
                }
                toRemove.Add(kvp.Key);
            }
            else if (options.SoulDurationRounds > 0f)
            {
                soul.RoundsRemaining--;
                if (soul.RoundsRemaining <= 0)
                {
                    if (soul.GameObject != null)
                    {
                        UnityEngine.Object.Destroy(soul.GameObject);
                    }
                    toRemove.Add(kvp.Key);
                }
            }
        }

        foreach (var id in toRemove)
        {
            ActiveSouls.Remove(id);
        }
    }

    private static SpriteRenderer? PickBestPlayerWorldRenderer(PlayerControl local)
    {
        if (local == null) return null;
        var direct = local.GetComponent<SpriteRenderer>();
        if (direct != null) return direct;

        var rends = local.GetComponentsInChildren<SpriteRenderer>(true);
        if (rends == null || rends.Length == 0) return null;

        SpriteRenderer? best = null;
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

    private static void ConfigureVisionMask(GameObject go, SpriteRenderer targetRenderer, out float zAxis)
    {
        zAxis = 0f;
        if (targetRenderer == null || go == null) return;

        if (ShipStatus.Instance != null && go.transform.parent != ShipStatus.Instance.transform)
            go.transform.SetParent(ShipStatus.Instance.transform, true);

        SpriteRenderer? src = null;
        var local = PlayerControl.LocalPlayer;
        if (local != null)
        {
            src = PickBestPlayerWorldRenderer(local);
        }

        if (src == null)
        {
            var vent = UnityEngine.Object.FindObjectOfType<Vent>();
            if (vent != null) src = vent.GetComponent<SpriteRenderer>();
        }

        if (src == null)
        {
            go.layer = 8;
            targetRenderer.sortingLayerName = "Players";
            targetRenderer.sortingOrder = 10;
            targetRenderer.maskInteraction = SpriteMaskInteraction.None;
            return;
        }

        // Place slightly in front of players/background assets (lower Z value in 2D space)
        zAxis = src.transform.position.z - 0.05f;
        go.layer = src.gameObject.layer;
        targetRenderer.sortingLayerID = src.sortingLayerID;
        targetRenderer.sortingOrder = src.sortingOrder + 1; // Render on top of dead bodies
        targetRenderer.maskInteraction = src.maskInteraction;
    }
}

// Simple Mono class to handle visibility and floating animation
public class SoulAnimator : MonoBehaviour
{
    public SpriteRenderer? SpriteRenderer { get; set; }
    public byte PlayerId { get; set; }
    private float _startY;
    private float _timeOffset;

    public SoulAnimator(IntPtr ptr) : base(ptr) { }

    private void Start()
    {
        _startY = transform.position.y;
        _timeOffset = UnityEngine.Random.value * Mathf.PI * 2f;
    }

    private void Update()
    {
        // 1. Everyone has to see these souls
        bool isVisible = true;

        if (SpriteRenderer != null)
        {
            SpriteRenderer.enabled = isVisible;
            if (isVisible)
            {
                // Check if the local player is a Grim Reaper to allow highlighting target
                var local = PlayerControl.LocalPlayer;
                bool isReaper = local != null && local.Data != null && local.Data.Role is GrimReaperRole;

                bool isTargeted = false;
                if (isReaper)
                {
                    var collectButton = MiraAPI.Hud.CustomButtonSingleton<GrimReaperCollectButton>.Instance;
                    if (collectButton != null && collectButton.Target != null && collectButton.Target.ParentId == PlayerId)
                    {
                        isTargeted = true;
                    }
                }

                if (isTargeted)
                {
                    // Make it glow brighter (cyan/white) and scale slightly larger
                    SpriteRenderer.color = new Color(0.9f, 1f, 1f, 1f);
                    transform.localScale = new Vector3(0.7f * 1.3f, 0.7f * 1.3f, 1f);
                }
                else
                {
                    // Normal state
                    SpriteRenderer.color = new Color(0.6f, 0.9f, 1f, 0.7f);
                    transform.localScale = new Vector3(0.7f, 0.7f, 1f);
                }
            }
        }

        // 2. Subtle floating visual effect
        var pos = transform.position;
        pos.y = _startY + Mathf.Sin(Time.time * 2f + _timeOffset) * 0.1f;
        transform.position = pos;
    }
}
