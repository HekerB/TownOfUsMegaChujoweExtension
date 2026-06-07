using HarmonyLib;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TownOfUs.Patches.Roles;
using TMPro;

namespace TouMegaChujoweExtension.Patches.Roles.VoodooMaster;

[HarmonyPatch]
public static class VoodooPatches
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool SendChatPrefix(ChatController __instance)
    {
        if (PlayerControl.LocalPlayer == null)
        {
            return true;
        }

        if (PlayerControl.LocalPlayer.HasModifier<VoodooMutedModifier>())
        {
            __instance.UpdateChatMode();
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.UpdateChatMode))]
    [HarmonyPostfix]
    public static void UpdateChatModePostfix(ChatController __instance)
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data.IsDead)
        {
            return;
        }

        var field = typeof(ChatControllerPatches).GetField("_noticeText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var noticeText = (TextMeshPro?)field?.GetValue(null);

        if (PlayerControl.LocalPlayer.HasModifier<VoodooMutedModifier>())
        {
            if (noticeText != null)
            {
                noticeText.text = "You have been muted by a Voodoo curse.";
            }

            __instance.freeChatField.SetVisible(false);
            __instance.quickChatField.SetVisible(false);
        }
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
public static class VoodooVisionPatch
{
    public static void Postfix(NetworkedPlayerInfo player, ref float __result)
    {
        if (player == null || player.IsDead || player.Object == null)
        {
            return;
        }

        if (player.Object.TryGetModifier<VoodooBlindModifier>(out var blindModifier))
        {
            __result *= blindModifier.VisionPerc;
        }
    }
}
