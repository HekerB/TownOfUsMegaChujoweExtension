using HarmonyLib;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Options.Roles.Neutral;

namespace TouMegaChujoweExtension.Patches.Roles.Pope;

[HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Update))]
public static class PopeBlockEmergencyPatch
{
    public static void Postfix(EmergencyMinigame __instance)
    {
        if (!OptionGroupSingleton<PopeOptions>.Instance.BlockEmergencyButtonDuringJudgement) return;
        if (ShipStatus.Instance == null) return;
        var sabId = (SystemTypes)PopeJudgementSystem.SabotageId;
        if (!ShipStatus.Instance.Systems.ContainsKey(sabId)) return;

        var sabo = ShipStatus.Instance.Systems[sabId].TryCast<PopeJudgementSystem>();
        if (sabo != null && sabo.IsActive)
        {
            __instance.StatusText.text = "Sanctify is active!";
            __instance.NumberText.text = string.Empty;
            
            __instance.ButtonActive = false;
            
            __instance.state = 3; 
        }
    }
}














