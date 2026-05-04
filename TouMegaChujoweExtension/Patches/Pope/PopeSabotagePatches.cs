using HarmonyLib;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Pope;

[HarmonyPatch]
public static class PopeSabotagePatches
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.OnEnable))]
    [HarmonyPatch(typeof(PolusShipStatus), nameof(PolusShipStatus.OnEnable))]
    [HarmonyPatch(typeof(AirshipStatus), nameof(AirshipStatus.OnEnable))]
    [HarmonyPatch(typeof(FungleShipStatus), nameof(FungleShipStatus.OnEnable))]
    [HarmonyPostfix]
    public static void AddPopeSabotageSystem(ShipStatus __instance)
    {
        var sabId = (SystemTypes)PopeJudgementSystem.SabotageId;
        if (__instance.Systems.ContainsKey(sabId))
            return;

        var JudgementSabo = new PopeJudgementSystem(
            OptionGroupSingleton<PopeOptions>.Instance.JudgementDuration);

        __instance.Systems.Add(sabId, JudgementSabo.Cast<ISystemType>());

        var saboSystem = __instance.Systems[SystemTypes.Sabotage].Cast<SabotageSystemType>();
        saboSystem.specials.Add(JudgementSabo.Cast<IActivatable>());
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.AddSystemTask))]
    [HarmonyPrefix]
    public static bool AddPopeTask(PlayerControl __instance, ref SystemTypes system,
        ref PlayerTask __result)
    {
        if (!__instance.AmOwner) return true;

        if (system == (SystemTypes)PopeJudgementSystem.SabotageId)
        {
            var task = new GameObject("PopeJudgementTask").AddComponent<PopeJudgementTask>();
            task.gameObject.transform.SetParent(__instance.gameObject.transform);
            task.Id = 253U;
            task.Owner = __instance;
            task.Initialize();
            __instance.myTasks.Add(task);
            __result = task;
            return false;
        }
        return true;
    }
}
