using System.Collections;
using HarmonyLib;
using MiraAPI.Modifiers;
using Reactor.Utilities;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TownOfUs.Modules.Localization;
using TownOfUs.Patches.Roles;
using TMPro;
using UnityEngine;

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

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
public static class VoodooMuteMeetingIntroPatch
{
    public static void Postfix(MeetingHud __instance)
    {
        if (PlayerControl.LocalPlayer != null &&
            !PlayerControl.LocalPlayer.Data.IsDead &&
            PlayerControl.LocalPlayer.HasModifier<VoodooMutedModifier>())
        {
            Coroutines.Start(CoVoodooMutedIntro());
        }
    }

    private static IEnumerator CoVoodooMutedIntro()
    {
        yield return HudManager.Instance.CoFadeFullScreen(Color.clear, new Color(0f, 0f, 0f, 0.98f));
        var tempPosition = HudManager.Instance.shhhEmblem.transform.localPosition;
        var tempDuration = HudManager.Instance.shhhEmblem.HoldDuration;
        HudManager.Instance.shhhEmblem.transform.localPosition = new Vector3(
            HudManager.Instance.shhhEmblem.transform.localPosition.x,
            HudManager.Instance.shhhEmblem.transform.localPosition.y,
            HudManager.Instance.FullScreen.transform.position.z + 1f);
        HudManager.Instance.shhhEmblem.TextImage.text = TouLocale.Get("ExtensionVoodooMutedIntro", "YOU ARE MUTED!");
        HudManager.Instance.shhhEmblem.HoldDuration = 2.5f;
        yield return HudManager.Instance.ShowEmblem(true);
        HudManager.Instance.shhhEmblem.transform.localPosition = tempPosition;
        HudManager.Instance.shhhEmblem.HoldDuration = tempDuration;
        yield return HudManager.Instance.CoFadeFullScreen(new Color(0f, 0f, 0f, 0.98f), Color.clear);
        yield return null;
    }
}
