using HarmonyLib;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Patches.Pope;

[HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Update))]
public static class PopeBlockEmergencyPatch
{
    public static void Postfix(EmergencyMinigame __instance)
    {
        if (ShipStatus.Instance == null) return;
        var sabId = (SystemTypes)PopeJudgementSystem.SabotageId;
        if (!ShipStatus.Instance.Systems.ContainsKey(sabId)) return;

        var sabo = ShipStatus.Instance.Systems[sabId].TryCast<PopeJudgementSystem>();
        if (sabo != null && sabo.IsActive)
        {
            __instance.StatusText.text = "Divine Judgement is active!";
            __instance.NumberText.text = string.Empty;
            
            __instance.ButtonActive = false;
            
            __instance.state = 3; 
        }
    }
}
