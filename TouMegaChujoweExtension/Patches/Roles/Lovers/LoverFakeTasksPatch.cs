using HarmonyLib;
using MiraAPI.Modifiers;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Lovers;

/// <summary>
/// A Crewmate in love with an Impostor or a killer does no real tasks: TOU-Mira leaves them out of the task bar
/// (<see cref="LoverModifier.DoesTasks"/>) but never says so, so the player kept doing them. This puts a
/// fake-tasks header on top of their task list for as long as that holds.
/// </summary>
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class LoverFakeTasksPatch
{
    private const string HeaderName = "ExtensionLoverFakeTasks";
    private const float CheckInterval = 0.5f;

    private static float _nextCheck;

    [HarmonyPostfix]
    public static void Postfix()
    {
        if (Time.time < _nextCheck)
        {
            return;
        }

        _nextCheck = Time.time + CheckInterval;

        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null || local.myTasks == null || !ShipStatus.Instance)
        {
            return;
        }

        var header = FindHeader(local);
        var wanted = local.IsCrewmate() &&
                     local.TryGetModifier<LoverModifier>(out var lover) &&
                     !lover.DoesTasks;

        if (wanted && header == null)
        {
            var go = new GameObject(HeaderName);
            go.transform.SetParent(local.transform, false);
            var task = go.AddComponent<ImportantTextTask>();
            task.Text = $"<color=#FF66CCFF>{TouLocale.Get(HeaderName, "You are in love with an evil player. Your tasks do not count.\nFake Tasks:")}</color>";
            local.myTasks.Insert(0, task);
        }
        else if (!wanted && header != null)
        {
            local.myTasks.Remove(header);
            UnityEngine.Object.Destroy(header.gameObject);
        }
    }

    private static PlayerTask? FindHeader(PlayerControl player)
    {
        foreach (var task in player.myTasks)
        {
            if (task != null && task.gameObject.name == HeaderName)
            {
                return task;
            }
        }

        return null;
    }
}
