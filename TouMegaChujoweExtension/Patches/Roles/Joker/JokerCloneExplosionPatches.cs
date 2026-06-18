using HarmonyLib;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Modules;
using TownOfUs.Options.Roles.Impostor;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Joker;

[HarmonyPatch(typeof(Bomb), nameof(Bomb.Detonate))]
public static class JokerCloneBomberBombPatch
{
    private static readonly AccessTools.FieldRef<Bomb, GameObject> BombObjectRef =
        AccessTools.FieldRefAccess<Bomb, GameObject>("_obj");

    private static readonly AccessTools.FieldRef<Bomb, PlayerControl> BomberRef =
        AccessTools.FieldRefAccess<Bomb, PlayerControl>("_bomber");

    public static void Postfix(Bomb __instance)
    {
        var bombObject = BombObjectRef(__instance);
        var bomber = BomberRef(__instance);
        if (bombObject == null || bomber == null)
        {
            return;
        }

        var radius = OptionGroupSingleton<BomberOptions>.Instance.DetonateRadius * ShipStatus.Instance.MaxLightRadius;
        JokerCloneSystem.TriggerClonesInRadius(bomber, bombObject.transform.position, radius);
    }
}
