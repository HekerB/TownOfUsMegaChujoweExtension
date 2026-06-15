using HarmonyLib;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.Linq;
using MiraAPI.Modifiers.Types;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Utilities.Appearances;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Sonar;

[HarmonyPatch]
public static class SonarBetterSonarPatch
{
    public static readonly Dictionary<byte, GameObject> TrackerIcons = new();
    private static readonly Dictionary<byte, Queue<(float time, Vector3 pos)>> PositionHistory = new();
    private static readonly List<SonarTracker> LocalTrackers = [];
    private static readonly HashSet<byte> TrackedIds = [];
    private static readonly List<byte> IdsToRemove = [];

    private readonly record struct SonarTracker(
        TimedModifier Modifier,
        PlayerControl Owner,
        PlayerControl Player,
        Vector3? TargetPosition);

    public static bool GetTrackerResetEveryRound()
    {
        try
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var type = assembly.GetType("MiraAPI.Options.Roles.Crewmate.SonarOptions") ??
                           assembly.GetType("TownOfUs.Options.Roles.Crewmate.SonarOptions") ??
                           assembly.GetType("TownOfUs.Options.Roles.Crewmate.TrackerOptions") ??
                           assembly.GetType("TownOfUs.Options.TrackerOptions");

                if (type != null)
                {
                    var singletonType = typeof(OptionGroupSingleton<>).MakeGenericType(type);
                    var instanceProp = singletonType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var instance = instanceProp?.GetValue(null);
                    if (instance != null)
                    {
                        var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        foreach (var prop in props.Where(p => p.Name.Contains("Reset", System.StringComparison.OrdinalIgnoreCase)))
                        {
                            var val = prop.GetValue(instance);
                            if (val is bool b) return b;

                            var valueProp = prop.PropertyType.GetProperty("Value");
                            if (valueProp != null)
                            {
                                var optVal = valueProp.GetValue(val);
                                if (optVal is bool b2) return b2;
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Fallback
        }
        return false;
    }

    public static float GetTrackerDelay()
    {
        try
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var type = assembly.GetType("MiraAPI.Options.Roles.Crewmate.SonarOptions") ??
                           assembly.GetType("TownOfUs.Options.Roles.Crewmate.SonarOptions") ??
                           assembly.GetType("TownOfUs.Options.Roles.Crewmate.TrackerOptions") ??
                           assembly.GetType("TownOfUs.Options.TrackerOptions");
                
                if (type != null)
                {
                    var singletonType = typeof(OptionGroupSingleton<>).MakeGenericType(type);
                    var instanceProp = singletonType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var instance = instanceProp?.GetValue(null);
                    if (instance != null)
                    {
                        var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        foreach (var prop in props.Where(p => p.Name.Contains("Delay", System.StringComparison.OrdinalIgnoreCase) ||
                                                             p.Name.Contains("Interval", System.StringComparison.OrdinalIgnoreCase)))
                        {
                            var val = prop.GetValue(instance);
                            if (val is float f) return f;
                            if (val is int i) return i;
                            if (val is double d) return (float)d;
                            
                            var valueProp = prop.PropertyType.GetProperty("Value");
                            if (valueProp != null)
                            {
                                var optVal = valueProp.GetValue(val);
                                if (optVal is float f2) return f2;
                                if (optVal is int i2) return i2;
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Fallback
        }
        return 0f;
    }

    [HarmonyPatch(typeof(SonarArrowTargetModifier), nameof(SonarArrowTargetModifier.OnActivate))]
    [HarmonyPostfix]
    public static void TrackerArrowOnActivatePostfix(SonarArrowTargetModifier __instance)
    {
        var opts = OptionGroupSingleton<SonarExtendedOptions>.Instance;
        if (opts.BetterSonar && opts.Mode == SonarDisplayMode.MapOnly && __instance.Arrow != null)
        {
            __instance.Arrow.gameObject.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(SonarHeartbeatTargetModifier), nameof(SonarHeartbeatTargetModifier.OnActivate))]
    [HarmonyPostfix]
    public static void TrackerHeartbeatOnActivatePostfix(SonarHeartbeatTargetModifier __instance)
    {
        var opts = OptionGroupSingleton<SonarExtendedOptions>.Instance;
        if (opts.BetterSonar && opts.Mode == SonarDisplayMode.MapOnly && __instance.Arrow != null)
        {
            __instance.Arrow.gameObject.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap))]
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowCountOverlay))]
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowNormalMap))]
    [HarmonyPostfix]
    public static void MapBehaviourShowPostfix(MapBehaviour __instance)
    {
        UpdateTrackerIcons(__instance);
    }

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.FixedUpdate))]
    [HarmonyPostfix]
    public static void MapBehaviourFixedUpdatePostfix(MapBehaviour __instance)
    {
        if (__instance != null && __instance.gameObject.activeSelf)
        {
            UpdateTrackerIcons(__instance);
        }
    }

    private static void UpdateTrackerIcons(MapBehaviour __instance)
    {
        var opts = OptionGroupSingleton<SonarExtendedOptions>.Instance;
        if (!opts.BetterSonar)
        {
            ClearIcons();
            return;
        }

        LocalTrackers.Clear();
        AddLocalTrackers();

        if (LocalTrackers.Count == 0)
        {
            ClearIcons();
            return;
        }

        var xPos = TownOfUs.Utilities.MiscUtils.GetCurrentMap is TownOfUs.Utilities.ExpandedMapNames.Dleks ? -1 : 1;

        TrackedIds.Clear();
        foreach (var tracker in LocalTrackers)
        {
            var trackedPlayer = tracker.Player;
            if (trackedPlayer == null || trackedPlayer.Data == null || trackedPlayer.Data.IsDead)
            {
                continue;
            }

            var playerId = trackedPlayer.PlayerId;
            TrackedIds.Add(playerId);

            // Use the arrow's target, which already respects the update interval
            Vector3 targetPos;
            if (tracker.TargetPosition.HasValue)
            {
                targetPos = tracker.TargetPosition.Value;
            }
            else
            {
                targetPos = trackedPlayer.transform.position;
            }

            var location = targetPos / ShipStatus.Instance.MapScale;
            location.x *= xPos;
            location.z = -1.99f;

            if (!TrackerIcons.TryGetValue(playerId, out var icon) || icon == null)
            {
                var renderer = Object.Instantiate(__instance.TrackedHerePoint, __instance.HerePoint.transform.parent);
                renderer.material = TownOfUs.Modules.Anims.AnimStore.SetSpriteColourMatch(trackedPlayer, renderer.material);
                icon = renderer.gameObject;
                icon.name = $"Better Sonar Tracker Icon {playerId}";
                icon.SetActive(true);
                TrackerIcons[playerId] = icon;
            }

            if (icon != null)
            {
                icon.transform.localPosition = location;
            }
        }

        // Remove icons for players who are no longer tracked
        IdsToRemove.Clear();
        foreach (var id in TrackerIcons.Keys)
        {
            if (!TrackedIds.Contains(id))
            {
                IdsToRemove.Add(id);
            }
        }

        foreach (var id in IdsToRemove)
        {
            if (TrackerIcons.TryGetValue(id, out var icon) && icon != null)
            {
                Object.Destroy(icon);
            }
            TrackerIcons.Remove(id);
        }
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
    [HarmonyPostfix]
    public static void ShipStatusBeginPostfix()
    {
        ClearIcons();
    }

    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (@event.TriggeredByIntro)
            return;

        var opts = OptionGroupSingleton<SonarExtendedOptions>.Instance;
        if (!opts.BetterSonar || !GetTrackerResetEveryRound())
            return;

        LocalTrackers.Clear();
        AddLocalTrackers();

        foreach (var tracker in LocalTrackers)
        {
            tracker.Owner.RemoveModifier(tracker.Modifier);
        }

        ClearIcons();
    }

    private static void AddLocalTrackers()
    {
        foreach (var mod in ModifierUtils.GetActiveModifiers<SonarArrowTargetModifier>())
        {
            if (mod.Owner == PlayerControl.LocalPlayer)
            {
                LocalTrackers.Add(new SonarTracker(mod, mod.Owner, mod.Player, mod.Arrow?.target));
            }
        }

        foreach (var mod in ModifierUtils.GetActiveModifiers<SonarHeartbeatTargetModifier>())
        {
            if (mod.Owner == PlayerControl.LocalPlayer)
            {
                LocalTrackers.Add(new SonarTracker(mod, mod.Owner, mod.Player, mod.Arrow?.target));
            }
        }
    }

    public static void ClearIcons()
    {
        foreach (var icon in TrackerIcons.Values)
        {
            if (icon != null)
            {
                Object.Destroy(icon);
            }
        }
        TrackerIcons.Clear();
        PositionHistory.Clear();
    }
}














