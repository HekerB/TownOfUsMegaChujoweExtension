using TownOfUs.Utilities;
using TownOfUs.Roles.Crewmate;

namespace TouMegaChujoweExtension.Modules;

/// <summary>
/// Tracks which players are currently in which vents.
/// </summary>
public static class VentOccupancySystem
{
    private static readonly Dictionary<int, byte> VentOccupants = new();

    public static void SetOccupant(int ventId, byte playerId)
    {
        if (playerId == 0)
        {
            VentOccupants.Remove(ventId);
        }
        else
        {
            VentOccupants[ventId] = playerId;
        }
    }

    public static bool TryGetOccupant(int ventId, out byte playerId)
    {
        return VentOccupants.TryGetValue(ventId, out playerId);
    }

    public static bool IsOccupied(int ventId)
    {
        return VentOccupants.ContainsKey(ventId);
    }

    public static void ClearAll()
    {
        VentOccupants.Clear();
    }

    public static void ClearForPlayer(byte playerId)
    {
        var toRemove = new List<int>();
        foreach (var kvp in VentOccupants)
        {
            if (kvp.Value == playerId)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var ventId in toRemove)
        {
            VentOccupants.Remove(ventId);
        }
    }

    public static PlayerControl GetOccupantPlayer(int ventId)
    {
        if (TryGetOccupant(ventId, out var playerId))
        {
            var player = MiscUtils.PlayerById(playerId);
            return player != null ? player : null!;
        }
        return null!;
    }

    private static System.Reflection.PropertyInfo? _ventsBlockedProp;
    private static System.Reflection.PropertyInfo? _ventFlushSetProp;
    private static System.Reflection.FieldInfo? _ventsBlockedField;
    private static System.Reflection.FieldInfo? _ventFlushSetField;
    private static bool _checkedProperties = false;

    /// <summary>
    /// Checks if a vent is currently blocked by a Plumber's barricade or flushing action.
    /// </summary>
    public static bool IsBlocked(int ventId)
    {
        try
        {
            if (!_checkedProperties)
            {
                _checkedProperties = true;
                var plumberType = typeof(PlumberRole);
                _ventsBlockedProp = plumberType.GetProperty("VentsBlocked", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                _ventFlushSetProp = plumberType.GetProperty("VentFlushSet", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (_ventsBlockedProp == null)
                {
                    _ventsBlockedField = plumberType.GetField("VentsBlocked", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                }
                if (_ventFlushSetProp == null)
                {
                    _ventFlushSetField = plumberType.GetField("VentFlushSet", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                }
            }

            System.Collections.Generic.Dictionary<int, int>? ventsBlocked = null;
            if (_ventsBlockedProp != null)
            {
                ventsBlocked = _ventsBlockedProp.GetValue(null) as System.Collections.Generic.Dictionary<int, int>;
            }
            else if (_ventsBlockedField != null)
            {
                ventsBlocked = _ventsBlockedField.GetValue(null) as System.Collections.Generic.Dictionary<int, int>;
            }

            System.Collections.Generic.HashSet<int>? ventFlushSet = null;
            if (_ventFlushSetProp != null)
            {
                ventFlushSet = _ventFlushSetProp.GetValue(null) as System.Collections.Generic.HashSet<int>;
            }
            else if (_ventFlushSetField != null)
            {
                ventFlushSet = _ventFlushSetField.GetValue(null) as System.Collections.Generic.HashSet<int>;
            }

            if (ventsBlocked == null || ventFlushSet == null)
            {
                return false;
            }

            return ventsBlocked.ContainsKey(ventId) || ventFlushSet.Contains(ventId);
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"[TOUMCE] VentOccupancySystem.IsBlocked Error: {ex}");
            return false;
        }
    }
}



