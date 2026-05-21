using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using MiraAPI.GameOptions;
using TownOfUs.Assets;
using TownOfUs.Utilities;
using TownOfUs.Extensions;
using TownOfUs.Modules;
using HarmonyLib;

namespace TouMegaChujoweExtension.Modules;

public static class SandwormSystem
{
    private static readonly List<Vent> SpawnedVents = new();

    public static Vent SpawnVent(PlayerControl player, int ventId, Vector2 position)
    {
        var ventPrefab = ShipStatus.Instance.AllVents[0];
        
        // Handle submerged map if needed
        if (ModCompatibility.IsSubmerged() && ShipStatus.Instance.AllVents.Length > 15)
        {
            ventPrefab = (position.y > -7) ? ShipStatus.Instance.AllVents[5] : ShipStatus.Instance.AllVents[15];
        }

        var vent = UnityEngine.Object.Instantiate(ventPrefab, ventPrefab.transform.parent);
        vent.name = $"SandwormVent-{player.PlayerId}-{ventId}";
        vent.Id = ventId;
        
        // Clear default connections inherited from prefab to isolate it from the map's vent network
        vent.Left = null;
        vent.Right = null;
        
        // Z-axis positioning to avoid clipping
        vent.transform.position = new Vector3(position.x, position.y, ventPrefab.transform.position.z);
        
        // Link to ShipStatus
        var allVents = ShipStatus.Instance.AllVents.ToList();
        allVents.Add(vent);
        ShipStatus.Instance.AllVents = allVents.ToArray();
        
        SpawnedVents.Add(vent);
        return vent;
    }

    public static void Reset()
    {
        if (ShipStatus.Instance != null && ShipStatus.Instance.AllVents != null)
        {
            var list = ShipStatus.Instance.AllVents.ToList();
            list.RemoveAll(v => v == null || v.name.StartsWith("SandwormVent-"));
            ShipStatus.Instance.AllVents = list.ToArray();
        }

        foreach (var v in SpawnedVents)
        {
            if (v != null) UnityEngine.Object.Destroy(v.gameObject);
        }
        SpawnedVents.Clear();
    }
}
