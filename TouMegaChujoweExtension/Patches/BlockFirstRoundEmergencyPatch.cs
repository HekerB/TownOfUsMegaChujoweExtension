using HarmonyLib;
using MiraAPI.GameOptions;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Patches;

[HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Update))]
public static class BlockFirstRoundEmergencyPatch
{
    public static void Postfix(EmergencyMinigame __instance)
    {
        var options = OptionGroupSingleton<Options.ExtensionGeneralOptions>.Instance;
        if (!options.BlockFirstRoundEmergency) return;

        if (TownOfUs.Events.DeathEventHandlers.CurrentRound <= 1)
        {
            __instance.StatusText.text = TouLocale.GetParsed(
                "ExtensionBlockFirstRoundEmergencyStatus",
                "Emergency meetings are disabled in the first round!");
            __instance.NumberText.text = string.Empty;
            __instance.ButtonActive = false;
            __instance.state = 3;
        }
    }
}