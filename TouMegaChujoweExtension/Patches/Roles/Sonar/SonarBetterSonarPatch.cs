using HarmonyLib;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.Linq;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Utilities.Appearances;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Sonar;

[HarmonyPatch]
public static class SonarBetterSonarPatch
{
    public static readonly Dictionary<byte, GameObject> TrackerIcons = new();
    private static readonly Dictionary<byte, Queue<(float time, Vector3 pos)>> PositionHistory = new();

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
                        foreach (var prop in props)
                        {
                            if (prop.Name.Contains("Reset", System.StringComparison.OrdinalIgnoreCase))
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
                        foreach (var prop in props)
                        {
                            if (prop.Name.Contains("Delay", System.StringComparison.OrdinalIgnoreCase) ||
                                prop.Name.Contains("Interval", System.StringComparison.OrdinalIgnoreCase))
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
        }
        catch
        {
            // Fallback
        }
        return 0f;
    }

    [HarmonyPatch(typeof(TrackerArrowTargetModifier), nameof(TrackerArrowTargetModifier.OnActivate))]
    [HarmonyPostfix]
    public static void TrackerArrowOnActivatePostfix(TrackerArrowTargetModifier __instance)
    {
        var opts = OptionGroupSingleton<SonarExtendedOptions>.Instance;
        if (opts.BetterSonar && opts.Mode == SonarDisplayMode.MapOnly)
        {
            if (__instance.Arrow != null)
            {
                __instance.Arrow.gameObject.SetActive(false);
            }
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

        var localTrackers = ModifierUtils.GetActiveModifiers<TrackerArrowTargetModifier>()
            .ToArray()
            .Where(mod => mod.Owner == PlayerControl.LocalPlayer)
            .ToList();

        if (localTrackers.Count == 0)
        {
            ClearIcons();
            return;
        }

        var xPos = TownOfUs.Utilities.MiscUtils.GetCurrentMap is TownOfUs.Utilities.ExpandedMapNames.Dleks ? -1 : 1;

        foreach (var tracker in localTrackers)
        {
            var trackedPlayer = tracker.Player;
            if (trackedPlayer == null || trackedPlayer.Data.IsDead)
            {
                continue;
            }

            var playerId = trackedPlayer.PlayerId;

            // Use the arrow's target, which already respects the update interval
            Vector3 targetPos;
            if (tracker.Arrow != null)
            {
                targetPos = tracker.Arrow.target;
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
        var trackedIds = localTrackers.Where(t => t.Player != null).Select(t => t.Player.PlayerId).ToHashSet();
        var idsToRemove = TrackerIcons.Keys.Where(id => !trackedIds.Contains(id)).ToList();

        foreach (var id in idsToRemove)
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

        var localTrackers = ModifierUtils.GetActiveModifiers<TrackerArrowTargetModifier>()
            .Where(mod => mod.Owner == PlayerControl.LocalPlayer)
            .ToList();

        localTrackers.ForEach(tracker => tracker?.Owner.RemoveModifier(tracker));

        ClearIcons();
    }

    public static void ClearIcons()
    {
        foreach (var icon in TrackerIcons.Values.Where(icon => icon != null))
        {
            Object.Destroy(icon);
        }
        TrackerIcons.Clear();
        PositionHistory.Clear();
    }
}














