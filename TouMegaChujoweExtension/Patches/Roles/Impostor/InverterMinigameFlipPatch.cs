using HarmonyLib;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Impostor;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Impostor;

[HarmonyPatch(typeof(Minigame), nameof(Minigame.Begin))]
public static class InverterMinigameBeginPatch
{
    public static void Postfix(Minigame __instance)
    {
        if (PlayerControl.LocalPlayer == null ||
            !PlayerControl.LocalPlayer.HasModifier<InverterDisorientedModifier>())
        {
            return;
        }

        var euler = __instance.transform.localEulerAngles;
        __instance.transform.localEulerAngles = new Vector3(euler.x, euler.y, 180f);
    }
}

[HarmonyPatch(typeof(Minigame), nameof(Minigame.Close), new System.Type[0])]
public static class InverterMinigameClosePatch
{
    public static void Prefix(Minigame __instance)
    {
        var euler = __instance.transform.localEulerAngles;
        __instance.transform.localEulerAngles = new Vector3(euler.x, euler.y, 0f);
    }
}
