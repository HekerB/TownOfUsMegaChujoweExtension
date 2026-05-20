using System;
using System.Reflection;
using HarmonyLib;
using TownOfUs.Buttons;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Pelican;

public static class PelicanTargetBlockPatches
{
    public static void Init()
    {
        try
        {
            var harmony = TouMegaChujoweExtensionPlugin.Harmony;
            
            // 1. Patch GetClosestLivingPlayer dynamically across all loaded assemblies
            var getClosestMethod = FindMethod("GetClosestLivingPlayer");
            if (getClosestMethod != null)
            {
                var prefix = typeof(PelicanTargetBlockPatches).GetMethod(nameof(GetClosestLivingPlayerPrefix), BindingFlags.Static | BindingFlags.Public);
                if (prefix != null)
                {
                    harmony.Patch(getClosestMethod, prefix: new HarmonyMethod(prefix));
                    Info("Successfully patched GetClosestLivingPlayer dynamically!");
                }
            }
            else
            {
                Warning("Could not find method GetClosestLivingPlayer to patch!");
            }

            // 2. Patch IsTargetValid dynamically on TownOfUsTargetButton<PlayerControl>
            var isTargetValidMethod = AccessTools.Method(typeof(TownOfUsTargetButton<PlayerControl>), nameof(TownOfUsTargetButton<PlayerControl>.IsTargetValid));
            if (isTargetValidMethod != null)
            {
                var postfix = typeof(PelicanTargetBlockPatches).GetMethod(nameof(IsTargetValidPostfix), BindingFlags.Static | BindingFlags.Public);
                if (postfix != null)
                {
                    harmony.Patch(isTargetValidMethod, postfix: new HarmonyMethod(postfix));
                    Info("Successfully patched TownOfUsTargetButton<PlayerControl>.IsTargetValid dynamically!");
                }
            }
            else
            {
                Warning("Could not find method TownOfUsTargetButton<PlayerControl>.IsTargetValid to patch!");
            }
        }
        catch (Exception ex)
        {
            Error($"Error in PelicanTargetBlockPatches.Init: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static MethodInfo? FindMethod(string methodName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type == null) continue;
                    foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (method.Name == methodName)
                        {
                            return method;
                        }
                    }
                }
            }
            catch { }
        }
        return null;
    }

    public static void GetClosestLivingPlayerPrefix(ref System.Predicate<PlayerControl>? predicate)
    {
        var originalPredicate = predicate;
        predicate = new System.Predicate<PlayerControl>(x =>
        {
            if (x == null || PelicanSystem.IsSwallowed(x.PlayerId)) return false;
            return originalPredicate == null || originalPredicate(x);
        });
    }

    public static void IsTargetValidPostfix(PlayerControl? target, ref bool __result)
    {
        if (target != null && PelicanSystem.IsSwallowed(target.PlayerId))
        {
            __result = false;
        }
    }
}
