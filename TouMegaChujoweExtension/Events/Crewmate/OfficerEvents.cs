using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Hud;
using TownOfUs.Buttons.Crewmate;
using TownOfUs.Modules;
using TownOfUs.Roles.Crewmate;
using UnityEngine;

namespace TouMegaChujoweExtension.Events.Crewmate;

public static class OfficerEvents
{
    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        var source = @event.Source;
        if (source.Data.Role is OfficerRole && GameHistory.PlayerStats.TryGetValue(source.PlayerId, out var stats))
        {
            stats.CorrectKills += 1;
        }
    }

    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent _)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.Data.Role is not OfficerRole)
        {
            return;
        }

        var shootButton = CustomButtonSingleton<OfficerShootButton>.Instance;
        var loadButton = CustomButtonSingleton<OfficerLoadButton>.Instance;

        var initialCd = 10f;

        shootButton?.SetTimer(Mathf.Max(shootButton.Timer, initialCd));
        loadButton?.SetTimer(Mathf.Max(loadButton.Timer, initialCd));
    }

    [HarmonyPatch(typeof(OfficerRole), nameof(OfficerRole.OnMeetingStart))]
    public static class OfficerMeetingStartPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(OfficerRole __instance)
        {
            __instance.RoundsBeforeReset--;

            if (__instance.Player.AmOwner)
            {
                if (__instance.RoundsBeforeReset > 0)
                {
                    OfficerRole.RpcOfficerSyncBullets(__instance.Player, __instance.RoundsBeforeReset, __instance.TotalBullets, 0);
                }
                else
                {
                    OfficerRole.RpcOfficerSyncBullets(__instance.Player, 0, __instance.TotalBullets, __instance.LoadedBullets);
                }
            }

            return false;
        }
    }
}