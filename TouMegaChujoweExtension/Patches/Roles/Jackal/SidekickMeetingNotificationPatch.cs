using HarmonyLib;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Neutral;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Assets;
using Reactor.Utilities.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Jackal;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
public static class SidekickMeetingNotificationPatch
{
    private static bool _notifiedThisGame = false;

    [HarmonyPostfix]
    public static void Postfix()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null) return;

        if (local.TryGetModifier<SidekickModifier>(out _) && !_notifiedThisGame)
        {
            _notifiedThisGame = true;

            // Show feedback that they were recruited in CHAT (like Lawyer/Lookout)
            string msg = TouLocale.GetParsed("SidekickIntroBlurb");
            if (!string.IsNullOrEmpty(msg))
            {
                MiscUtils.AddTeamChat(
                    local.Data,
                    $"<color=#{TouExtensionColors.Jackal.ToHtmlStringRGBA()}>{TouLocale.Get("ExtensionRoleJackal")}</color>",
                    msg,
                    bubbleType: BubbleType.Other,
                    onLeft: true
                );

                // Flash color
                Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Jackal));
            }
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    [HarmonyPostfix]
    public static void Reset()
    {
        ResetNotification();
    }

    public static void ResetNotification()
    {
        _notifiedThisGame = false;
    }
}
