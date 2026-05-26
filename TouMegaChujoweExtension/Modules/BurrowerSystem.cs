using System.Collections.Generic;
using System.Linq;
using TownOfUs.Modules;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public static class BurrowerSystem
{
    private static readonly List<Vent> SpawnedVents = [];

    public static bool TryFindVentPlacementPosition(PlayerControl player, Vector2 desiredPosition, out Vector2 position)
    {
        position = desiredPosition;
        if (CanPlaceVentAt(player, desiredPosition))
        {
            return true;
        }

        var candidates = new List<Vector2>();
        for (var radius = 0.25f; radius <= 1.5f; radius += 0.25f)
        {
            for (var i = 0; i < 16; i++)
            {
                var angle = i * Mathf.PI * 2f / 16f;
                candidates.Add(desiredPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        foreach (var candidate in candidates.OrderBy(candidate => Vector2.Distance(desiredPosition, candidate)))
        {
            if (!CanPlaceVentAt(player, candidate))
            {
                continue;
            }

            position = candidate;
            return true;
        }

        return false;
    }

    public static bool CanPlaceVentAt(PlayerControl player, Vector2 position)
    {
        if (ShipStatus.Instance?.AllVents == null || ShipStatus.Instance.AllVents.Length == 0)
        {
            return false;
        }

        var ventPrefab = GetVentPrefab(position);
        var collider = ventPrefab.GetComponent<BoxCollider2D>();
        var ventSize = collider != null
            ? Vector2.Scale(collider.size, ventPrefab.transform.localScale) * 0.75f
            : Vector2.one * 0.55f;

        var hits = Physics2D.OverlapBoxAll(position, ventSize, 0f);
        hits = hits.Where(collider2D =>
            collider2D != null &&
            (collider2D.name.Contains("Vent") || collider2D.name.Contains("Door") || !collider2D.isTrigger) &&
            collider2D.gameObject.layer != 8 &&
            collider2D.gameObject.layer != 5).ToArray();

        var noWallConflict = player.Collider == null ||
                             !PhysicsHelpers.AnythingBetween(
                                 player.Collider,
                                 player.Collider.bounds.center,
                                 position,
                                 Constants.ShipAndAllObjectsMask,
                                 false);

        return hits.Length == 0 && noWallConflict && !ModCompatibility.GetPlayerElevator(player).Item1;
    }

    public static Vent SpawnVent(PlayerControl player, int ventId, Vector2 position)
    {
        var ventPrefab = GetVentPrefab(position);

        var vent = UnityEngine.Object.Instantiate(ventPrefab, ventPrefab.transform.parent);
        vent.name = $"BurrowerVent-{player.PlayerId}-{ventId}";
        vent.Id = ventId;
        vent.Left = null;
        vent.Right = null;
        vent.transform.position = new Vector3(position.x, position.y, ventPrefab.transform.position.z);

        var allVents = ShipStatus.Instance.AllVents.ToList();
        allVents.Add(vent);
        ShipStatus.Instance.AllVents = allVents.ToArray();

        SpawnedVents.Add(vent);
        return vent;
    }

    public static void RemoveVent(Vent? vent)
    {
        if (vent == null)
        {
            return;
        }

        if (ShipStatus.Instance?.AllVents != null)
        {
            var allVents = ShipStatus.Instance.AllVents.ToList();
            allVents.RemoveAll(existing => existing == null || existing.Id == vent.Id);
            ShipStatus.Instance.AllVents = allVents.ToArray();
        }

        SpawnedVents.Remove(vent);

        if (vent.gameObject != null)
        {
            UnityEngine.Object.Destroy(vent.gameObject);
        }
    }

    private static Vent GetVentPrefab(Vector2 position)
    {
        var ventPrefab = ShipStatus.Instance.AllVents[0];

        if (ModCompatibility.IsSubmerged() && ShipStatus.Instance.AllVents.Length > 15)
        {
            ventPrefab = position.y > -7 ? ShipStatus.Instance.AllVents[5] : ShipStatus.Instance.AllVents[15];
        }

        return ventPrefab;
    }

    public static void Reset()
    {
        if (ShipStatus.Instance != null && ShipStatus.Instance.AllVents != null)
        {
            var list = ShipStatus.Instance.AllVents.ToList();
            list.RemoveAll(v => v == null || v.name.StartsWith("BurrowerVent-"));
            ShipStatus.Instance.AllVents = list.ToArray();
        }

        foreach (var vent in SpawnedVents)
        {
            if (vent != null)
            {
                UnityEngine.Object.Destroy(vent.gameObject);
            }
        }

        SpawnedVents.Clear();
    }
}
