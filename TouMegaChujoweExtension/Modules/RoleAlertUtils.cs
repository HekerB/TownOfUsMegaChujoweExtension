using MiraAPI.Utilities;
using System.Collections.Generic;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public static class RoleAlertUtils
{
    private const float ResetWindowSeconds = 0.35f;
    private const float DuplicateWindowSeconds = 0.75f;
    private const int MaxSlots = 4;

    private static readonly Dictionary<string, float> LastShownByKey = [];
    private static float _lastNotificationTime = -100f;
    private static int _nextSlot;

    public static LobbyNotificationMessage? ShowRoleAlert(string text, Color color, Sprite? sprite = null, string? key = null)
    {
        var now = Time.realtimeSinceStartup;
        var duplicateKey = key ?? text;
        if (LastShownByKey.TryGetValue(duplicateKey, out var lastShown) &&
            now - lastShown <= DuplicateWindowSeconds)
        {
            return null;
        }

        LastShownByKey[duplicateKey] = now;

        if (now - _lastNotificationTime > ResetWindowSeconds)
        {
            _nextSlot = 0;
        }

        var slot = _nextSlot;
        _nextSlot = (_nextSlot + 1) % MaxSlots;
        _lastNotificationTime = now;

        var notification = Helpers.CreateAndShowNotification(
            text,
            color,
            new Vector3(0f, 1f - (slot * 0.32f), -20f),
            spr: sprite);

        notification?.AdjustNotification();
        return notification;
    }
}
