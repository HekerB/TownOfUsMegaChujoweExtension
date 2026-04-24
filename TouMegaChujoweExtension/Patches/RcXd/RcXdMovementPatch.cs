using AmongUs.GameOptions;
using HarmonyLib;
using TouMegaChujoweExtension.Roles.Impostor;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.RcXd;

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
public static class RcXdMovementPatch
{
    [HarmonyPrefix]
    public static bool Prefix(PlayerPhysics __instance)
    {
        var player = __instance.myPlayer;
        if (player == null || !player.AmOwner) return true;
        if (player.Data?.Role is not RcXdRole role) return true;
        if (role.ActiveCar == null || !role.ActiveCar.IsDriving) return true;

        if (__instance.body != null)
            __instance.body.velocity = Vector2.zero;

        __instance.HandleAnimation(player.Data.IsDead);
        return false;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class RcXdPlayerControlPatch
{
    private static bool _weSetMoveable;

    [HarmonyPrefix]
    public static void Prefix(PlayerControl __instance)
    {
        if (!__instance.AmOwner) return;
        if (__instance.Data?.Role is not RcXdRole role) return;

        if (role.ActiveCar != null && role.ActiveCar.IsDriving)
        {
            if (__instance.moveable)
            {
                __instance.moveable = false;
                _weSetMoveable = true;
            }
        }
    }

    [HarmonyPostfix]
    public static void Postfix(PlayerControl __instance)
    {
        if (!__instance.AmOwner) return;
        if (__instance.Data?.Role is not RcXdRole role) return;

        if (role.ActiveCar == null || !role.ActiveCar.IsDriving)
        {
            if (_weSetMoveable)
            {
                __instance.moveable = true;
                _weSetMoveable = false;
            }
        }
    }
}

[HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.FixedUpdate))]
public static class RcXdNetTransformPatch
{
    [HarmonyPrefix]
    public static bool Prefix(CustomNetworkTransform __instance)
    {
        var player = __instance.myPlayer;
        if (player == null || !player.AmOwner) return true;
        if (player.Data?.Role is not RcXdRole role) return true;
        if (role.ActiveCar == null || !role.ActiveCar.IsDriving) return true;

        return false;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.GetTruePosition))]
public static class RcXdTruePositionPatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerControl __instance, ref Vector2 __result)
    {
        if (!__instance.AmOwner) return;
        if (__instance.Data?.Role is not RcXdRole role) return;
        if (role.ActiveCar == null || !role.ActiveCar.IsDriving) return;

        __result = role.ActiveCar.Position;
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
public static class RcXdVisionPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(ref float __result)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;
        if (player.Data?.Role is not RcXdRole role) return;
        if (role.ActiveCar == null || !role.ActiveCar.IsDriving) return;

        __result = 999f;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
public static class RcXdDeathPatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerControl __instance)
    {
        if (__instance.Data?.Role is not RcXdRole role) return;
        if (role.ActiveCar == null) return;

        role.ActiveCar.DoDestroy();
        role.ActiveCar = null;
    }
}

[HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
public static class RcXdVentBlockPatch
{
    [HarmonyPostfix]
    public static void Postfix(Vent __instance, ref float __result, ref bool canUse, ref bool couldUse,
        [HarmonyArgument(0)] NetworkedPlayerInfo pc)
    {
        if (pc == null || pc.Object == null || !pc.Object.AmOwner) return;
        if (pc.Role is not RcXdRole role) return;
        if (role.ActiveCar == null || !role.ActiveCar.IsDriving) return;

        canUse = false;
        couldUse = false;
        __result = float.MaxValue;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.NetTransform))]
public static class RcXdTransportSafety
{
    // Backup
}