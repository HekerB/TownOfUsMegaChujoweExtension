using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using TownOfUs;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TownOfUs.Buttons;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Options.Roles.Crewmate;

namespace TouMegaChujoweExtension.Patches.Roles.Impostor
{
    [HarmonyPatch]
    public static class ImpostorFriendlyFirePatches
    {
        [HarmonyPatch(typeof(PlayerControl), "GetClosestLivingPlayer")]
        [HarmonyPrefix]
        public static void GetClosestLivingPlayerPrefix(ref bool countImpostors)
        {
            var options = OptionGroupSingleton<AgentOptions>.Instance;
            if (options != null && options.ImpostorsCanKillEachOther)
            {
                countImpostors = true;
            }
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            var methods = new List<MethodBase>();
            try
            {
                var killButtonTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a =>
                    {
                        try { return a.GetTypes(); }
                        catch { return Type.EmptyTypes; }
                    })
                    .Where(t => t != null && typeof(IKillButton).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var type in killButtonTypes)
                {
                    var method = AccessTools.Method(type, "IsTargetValid", [typeof(PlayerControl)]);
                    if (method != null)
                    {
                        methods.Add(method);
                    }
                }
            }
            catch (Exception)
            {
            }
            return methods.Distinct();
        }

        [HarmonyPostfix]
        public static void Postfix(ref bool __result, object __instance, object? target)
        {
            if (target == null || __instance == null) return;
            if (__instance is not IKillButton) return;
            if (target is not PlayerControl playerTarget) return;

            var options = OptionGroupSingleton<AgentOptions>.Instance;
            if (options == null || !options.ImpostorsCanKillEachOther) return;

            var local = PlayerControl.LocalPlayer;
            if (local == null) return;

            if (local.IsImpostorAligned() && playerTarget.IsImpostorAligned() && local.PlayerId != playerTarget.PlayerId)
            {
                __result = true;
            }
        }
    }
}
