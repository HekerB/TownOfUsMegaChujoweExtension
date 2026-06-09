using HarmonyLib;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Joker;

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
public static class JokerInputPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool Prefix(PlayerPhysics __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null || __instance.myPlayer != local) return true;

        if (local.Data.Role is JokerRole && JokerCloneSystem.GetActiveCloneCountForJoker(local.PlayerId) > 0)
        {
            Vector2 wasd = Vector2.zero;
            if (Input.GetKey(KeyCode.W)) wasd.y += 1;
            if (Input.GetKey(KeyCode.S)) wasd.y -= 1;
            if (Input.GetKey(KeyCode.A)) wasd.x -= 1;
            if (Input.GetKey(KeyCode.D)) wasd.x += 1;
            
            AdvancedMovementUtilities.ApplyControlledMovement(__instance, wasd.normalized, stopIfZero: true);
            return false;
        }
        return true;
    }
}