using System.Collections;
using HarmonyLib;
using Reactor.Utilities;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Shifter;

[HarmonyPatch]
public static class ShifterStolenRoleMeetingIntroPatch
{
    private static bool pendingIntro;
    private static bool shownThisMeeting;
    private static bool forceIntroText;

    public static void QueueForLocalPlayer()
    {
        pendingIntro = true;
        shownThisMeeting = false;
        TryShowIntro();
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    public static void MeetingStartPostfix()
    {
        shownThisMeeting = false;
        TryShowIntro();
        Coroutines.Start(CoRetryIntro());
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    [HarmonyPostfix]
    public static void ResetOnGameEnd()
    {
        pendingIntro = false;
        shownThisMeeting = false;
        forceIntroText = false;
    }

    private static void TryShowIntro()
    {
        if (!pendingIntro ||
            shownThisMeeting ||
            MeetingHud.Instance == null ||
            HudManager.Instance == null ||
            PlayerControl.LocalPlayer == null ||
            PlayerControl.LocalPlayer.Data.IsDead)
        {
            return;
        }

        pendingIntro = false;
        shownThisMeeting = true;
        Coroutines.Start(MeetingShhh());
    }

    private static IEnumerator CoRetryIntro()
    {
        for (var i = 0; i < 90 && MeetingHud.Instance != null && !shownThisMeeting; i++)
        {
            TryShowIntro();
            yield return null;
        }
    }

    private static IEnumerator MeetingShhh()
    {
        yield return HudManager.Instance.CoFadeFullScreen(Color.clear, new Color(0f, 0f, 0f, 0.98f));

        var emblem = HudManager.Instance.shhhEmblem;
        var previousPosition = emblem.transform.localPosition;
        var previousDuration = emblem.HoldDuration;
        var previousText = emblem.TextImage.text;
        var introText = TouLocale.Get("ExtensionShifterRoleStolenIntro", "ROLE STOLEN");

        emblem.transform.localPosition = new Vector3(
            emblem.transform.localPosition.x,
            emblem.transform.localPosition.y,
            HudManager.Instance.FullScreen.transform.position.z + 1f);
        emblem.TextImage.text = introText;
        emblem.HoldDuration = 2.5f;

        forceIntroText = true;
        Coroutines.Start(CoKeepIntroText(introText));
        yield return HudManager.Instance.ShowEmblem(true);
        forceIntroText = false;

        emblem.TextImage.text = previousText;
        emblem.transform.localPosition = previousPosition;
        emblem.HoldDuration = previousDuration;

        yield return HudManager.Instance.CoFadeFullScreen(new Color(0f, 0f, 0f, 0.98f), Color.clear);
        yield return null;
    }

    private static IEnumerator CoKeepIntroText(string introText)
    {
        while (forceIntroText)
        {
            if (HudManager.Instance?.shhhEmblem?.TextImage != null)
            {
                HudManager.Instance.shhhEmblem.TextImage.text = introText;
            }

            yield return null;
        }
    }
}
