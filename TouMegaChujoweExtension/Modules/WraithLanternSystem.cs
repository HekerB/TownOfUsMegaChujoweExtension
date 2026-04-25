using TouMegaChujoweExtension.Assets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Modules;

public static class WraithLanternSystem
{
    private const float LanternScale = 0.65f;

    private sealed record ActiveLantern(Vector2 Position, float PlacedAt, float ExpiresAt);

    private static readonly Dictionary<byte, ActiveLantern> Active = new();
    private static readonly List<Vector2> BrokenEvidence = new();
    private static readonly Dictionary<byte, GameObject> ActiveVisuals = new();
    private static readonly List<GameObject> BrokenVisuals = new();

    public static void ClearAll()
    {
        Active.Clear();
        BrokenEvidence.Clear();
        ClearAllVisuals();
    }

    public static void ClearForPlayer(byte wraithId)
    {
        Active.Remove(wraithId);
        if (ActiveVisuals.TryGetValue(wraithId, out var go) && go != null)
        {
            Object.Destroy(go);
        }
        ActiveVisuals.Remove(wraithId);
    }

    public static bool HasActive(byte wraithId) => Active.ContainsKey(wraithId);

    public static bool TryGetActivePosition(byte wraithId, out Vector2 pos)
    {
        if (Active.TryGetValue(wraithId, out var entry))
        {
            pos = entry.Position;
            return true;
        }

        pos = default;
        return false;
    }

    public static void PlaceLantern(byte wraithId, Vector2 pos, float durationSeconds)
    {
        var now = Time.time;
        Active[wraithId] = new ActiveLantern(pos, now, now + Mathf.Max(0f, durationSeconds));

        if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == wraithId)
        {
            RemoveActiveVisual(wraithId);
            SpawnOrMoveActiveVisual(wraithId, pos);
        }
    }

    public static bool TryReturnLantern(byte wraithId, out Vector2 pos)
    {
        if (!Active.TryGetValue(wraithId, out var entry))
        {
            pos = default;
            return false;
        }

        Active.Remove(wraithId);
        pos = entry.Position;

        if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == wraithId)
        {
            RemoveActiveVisual(wraithId);
        }

        return true;
    }

    public static void BreakLantern(byte wraithId, Vector2 pos)
    {
        Active.Remove(wraithId);
        RemoveActiveVisual(wraithId);

        BrokenEvidence.Add(pos);
        SpawnBrokenVisual(pos);
    }


    private static SpriteRenderer PickBestPlayerWorldRenderer(PlayerControl local)
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
            if (sl.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0) score += 50;
            if (sl.IndexOf("UI", StringComparison.OrdinalIgnoreCase) >= 0) score -= 200;

            var nm = r.name ?? string.Empty;
            if (nm.IndexOf("Body", StringComparison.OrdinalIgnoreCase) >= 0) score += 40;
            if (nm.IndexOf("Sprite", StringComparison.OrdinalIgnoreCase) >= 0) score += 20;
            if (nm.IndexOf("Name", StringComparison.OrdinalIgnoreCase) >= 0) score -= 100;

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
        if (targetRenderer == null || go == null)
        {
            return;
        }

        if (ShipStatus.Instance != null && go.transform.parent != ShipStatus.Instance.transform)
            go.transform.SetParent(ShipStatus.Instance.transform, true);

        SpriteRenderer src = null;
        var local = PlayerControl.LocalPlayer;
        if (local != null)
        {
            src = PickBestPlayerWorldRenderer(local);
        }

        if (src == null)
        {
            var vent = Object.FindObjectOfType<Vent>();
            if (vent != null) src = vent.GetComponent<SpriteRenderer>();
        }

        if (src == null)
        {
            // Fallback to standard Ship layer if nothing found
            go.layer = 8;
            return;
        }

        zAxis = src.transform.position.z;
        go.layer = src.gameObject.layer;
        targetRenderer.sortingLayerID = src.sortingLayerID;
        targetRenderer.sortingOrder = src.sortingOrder;
        targetRenderer.maskInteraction = src.maskInteraction;
    }

    private static void ClearAllVisuals()
    {
        foreach (var visual in ActiveVisuals.Values.Where(v => v != null))
        {
            Object.Destroy(visual);
        }
        ActiveVisuals.Clear();

        foreach (var go in BrokenVisuals.Where(g => g != null))
        {
            Object.Destroy(go);
        }
        BrokenVisuals.Clear();
    }

    private static void SpawnOrMoveActiveVisual(byte wraithId, Vector2 pos)
    {
        if (!ActiveVisuals.TryGetValue(wraithId, out var go) || go == null)
        {
            go = new GameObject("WraithLantern");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = TouExtensionAssets.LanternSprite.LoadAsset();
            sr.color = new Color(1f, 1f, 1f, 0.65f);
            
            ConfigureVisionMask(go, sr, out var z);
            
            go.transform.localScale = new Vector3(LanternScale, LanternScale, 1f);
            ActiveVisuals[wraithId] = go;
        }

        ConfigureVisionMask(go, go.GetComponent<SpriteRenderer>(), out var zAxis);
        go.transform.position = new Vector3(pos.x, pos.y, zAxis);
    }

    private static void RemoveActiveVisual(byte wraithId)
    {
        if (ActiveVisuals.TryGetValue(wraithId, out var go) && go != null)
        {
            Object.Destroy(go);
        }
        ActiveVisuals.Remove(wraithId);
    }

    private static void SpawnBrokenVisual(Vector2 pos)
    {
        var go = new GameObject("WraithBrokenLantern");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = TouExtensionAssets.BrokenLanternSprite.LoadAsset();
        sr.color = new Color(0.7f, 0.7f, 0.7f, 0.95f);
        ConfigureVisionMask(go, sr, out var zAxis);
        go.transform.localScale = new Vector3(LanternScale, LanternScale, 1f);
        go.transform.position = new Vector3(pos.x, pos.y, zAxis);
        BrokenVisuals.Add(go);
    }
}



