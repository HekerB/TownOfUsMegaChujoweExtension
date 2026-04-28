using HarmonyLib;
using TouMegaChujoweExtension.Modules;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Pope;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
public static class PopeSystemTickPatch
{
    public static void Postfix()
    {
        var sabId = (SystemTypes)PopeJudgementSystem.SabotageId;
        if (!ShipStatus.Instance.Systems.ContainsKey(sabId))
            return;

        var system = ShipStatus.Instance.Systems[sabId].TryCast<PopeJudgementSystem>();
        if (system == null) return;

        system.Deteriorate(Time.fixedDeltaTime);
    }
}
