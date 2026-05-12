using HarmonyLib;
using MiraAPI.Modifiers;
using TownOfUs.Buttons;
using TouMegaChujoweExtension.Modifiers.Crewmate;

namespace TouMegaChujoweExtension.Patches.Roles.Injector;

[HarmonyPatch(typeof(TownOfUsButton))]
public static class InjectorRegenerationPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(TownOfUsButton.FixedUpdateHandler))]
    public static void FixedUpdateHandlerPostfix(TownOfUsButton __instance, PlayerControl playerControl)
    {
        if (playerControl == null || !playerControl.AmOwner)
        {
            return;
        }

        if (playerControl.HasModifier<InjectedRegenerationModifier>())
        {
            // Regeneration makes cooldowns tick 1.5x faster
            if (__instance.Timer > 0 && !__instance.TimerPaused)
            {
                __instance.Timer -= UnityEngine.Time.deltaTime * 0.5f; // Additional 0.5x = 1.5x total
            }
        }
        else if (playerControl.HasModifier<DoctorRegenerationModifier>())
        {
            // Doctor Regeneration makes cooldowns tick 2x faster
            if (__instance.Timer > 0 && !__instance.TimerPaused)
            {
                __instance.Timer -= UnityEngine.Time.deltaTime * 1.0f; // Additional 1.0x = 2.0x total
            }
        }
    }
}