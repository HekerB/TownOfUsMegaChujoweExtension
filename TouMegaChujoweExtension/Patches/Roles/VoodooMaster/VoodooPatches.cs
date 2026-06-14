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
    private static bool shownThisMeeting;
    private static bool forceVoodooIntroText;

    public static void Postfix(MeetingHud __instance)
    {
        shownThisMeeting = false;
        TryShowMutedIntro(ignoreShownFlag: false);
        Coroutines.Start(CoRetryMutedIntro());
    }

    public static void TryShowMutedIntro(bool ignoreShownFlag = false)
    {
        if ((!ignoreShownFlag && shownThisMeeting) ||
            MeetingHud.Instance == null ||
            HudManager.Instance == null ||
            PlayerControl.LocalPlayer == null ||
            PlayerControl.LocalPlayer.Data.IsDead ||
            !HasPendingOrActiveLocalMute())
        {
            return;
        }

        shownThisMeeting = true;
        Coroutines.Start(MeetingShhh());
    }

    private static bool HasPendingOrActiveLocalMute()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null)
        {
            return false;
        }

        return local.HasModifier<VoodooMutedModifier>() ||
               local.GetModifiers<VoodooScheduledCurseModifier>().Any(x => x.CurseType == VoodooEffect.Mute);
    }

    private static IEnumerator CoRetryMutedIntro()
    {
        for (var i = 0; i < 90 && MeetingHud.Instance != null && !shownThisMeeting; i++)
        {
            TryShowMutedIntro(ignoreShownFlag: false);
            yield return null;
        }
    }

    private static IEnumerator MeetingShhh()
    {
        yield return HudManager.Instance.CoFadeFullScreen(Color.clear, new Color(0f, 0f, 0f, 0.98f));
        var tempPosition = HudManager.Instance.shhhEmblem.transform.localPosition;
        var tempDuration = HudManager.Instance.shhhEmblem.HoldDuration;
        var tempText = HudManager.Instance.shhhEmblem.TextImage.text;
        var introText = TouLocale.Get("ExtensionVoodooMutedIntro", "YOU ARE MUTED!");
        HudManager.Instance.shhhEmblem.transform.localPosition = new Vector3(
            HudManager.Instance.shhhEmblem.transform.localPosition.x,
            HudManager.Instance.shhhEmblem.transform.localPosition.y,
            HudManager.Instance.FullScreen.transform.position.z + 1f);
        HudManager.Instance.shhhEmblem.TextImage.text = introText;
        HudManager.Instance.shhhEmblem.HoldDuration = 2.5f;
        forceVoodooIntroText = true;
        Coroutines.Start(CoKeepVoodooIntroText(introText));
        yield return HudManager.Instance.ShowEmblem(true);
        forceVoodooIntroText = false;
        HudManager.Instance.shhhEmblem.TextImage.text = tempText;
        HudManager.Instance.shhhEmblem.transform.localPosition = tempPosition;
        HudManager.Instance.shhhEmblem.HoldDuration = tempDuration;
        yield return HudManager.Instance.CoFadeFullScreen(new Color(0f, 0f, 0f, 0.98f), Color.clear);
        yield return null;
    }

    private static IEnumerator CoKeepVoodooIntroText(string introText)
    {
        while (forceVoodooIntroText)
        {
            if (HudManager.Instance?.shhhEmblem?.TextImage != null)
            {
                HudManager.Instance.shhhEmblem.TextImage.text = introText;
            }

            yield return null;
        }
    }
}
