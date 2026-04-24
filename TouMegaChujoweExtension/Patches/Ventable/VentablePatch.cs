using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Crewmate;
using TouMegaChujoweExtension.Options.Modifiers;
using TMPro;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Ventable;

[HarmonyPatch]
public static class VentablePatch
{
    private const string TimerTextName = "VentableTimerText";

    private static Vector3 _buttonBasePos;
    private static bool _basePosSet;

    [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
    [HarmonyPostfix]
    public static void CanUseVentPostfix(
        Vent __instance,
        NetworkedPlayerInfo pc,
        ref bool canUse,
        ref bool couldUse,
        ref float __result)
    {
        if (__instance == null || pc == null)
            return;

        if (canUse)
            return;

        var player = pc.Object;
        if (player == null || pc.IsDead || pc.Disconnected)
            return;

        var mod = player.GetModifier<VentableModifier>();
        if (mod == null)
            return;

        if (player.inVent)
        {
            if (Vent.currentVent != null && __instance.Id == Vent.currentVent.Id)
            {
                canUse = true;
                couldUse = true;
                __result = 0f;
            }

            return;
        }

        if (mod.VentsRemaining <= 0)
        {
            canUse = false;
            couldUse = false;
            __result = float.MaxValue;
            return;
        }

        Vector2 truePosition = player.GetTruePosition();
        Vector2 ventPosition = __instance.transform.position;
        float distance = Vector2.Distance(truePosition, ventPosition);

        bool inRange = distance <= __instance.UsableDistance;
        bool clearPath = !PhysicsHelpers.AnythingBetween(truePosition, ventPosition, Constants.ShipOnlyMask, false);

        couldUse = inRange && clearPath;
        canUse = couldUse && mod.CooldownTimer <= 0f;
        __result = distance;
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        if (__instance == null)
            return;

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data == null || player.Data.IsDead)
        {
            CleanupUI(__instance);
            return;
        }

        if (player.Data.Role != null && player.Data.Role.IsImpostor)
        {
            CleanupUI(__instance);
            return;
        }

        var mod = player.GetModifier<VentableModifier>();
        var ventButton = __instance.ImpostorVentButton;

        if (mod == null || ventButton == null || ventButton.gameObject == null)
        {
            CleanupUI(__instance);
            return;
        }

        bool inMeeting = MeetingHud.Instance != null || ExileController.Instance != null;
        if (inMeeting)
        {
            if (ventButton.gameObject.activeSelf)
                ventButton.gameObject.SetActive(false);

            CleanupUI(__instance);
            return;
        }

        bool shouldShow = mod.VentsRemaining > 0 || player.inVent;

        if (ventButton.gameObject.activeSelf != shouldShow)
            ventButton.gameObject.SetActive(shouldShow);

        if (ventButton.graphic != null)
        {
            if (ventButton.graphic.gameObject.activeSelf != shouldShow)
                ventButton.graphic.gameObject.SetActive(shouldShow);

            ventButton.graphic.enabled = shouldShow;
        }

        if (!shouldShow)
        {
            CleanupUI(__instance);
            _basePosSet = false;
            return;
        }

        var customSprite = mod.GetVentSprite();
        if (ventButton.graphic != null && ventButton.graphic.sprite != customSprite)
            ventButton.graphic.sprite = customSprite;

        if (ventButton.buttonLabelText != null)
            ventButton.buttonLabelText.outlineColor = Palette.CrewmateRoleHeaderBlue;

        if (mod.IsShaking)
        {
            if (!_basePosSet)
            {
                _buttonBasePos = ventButton.transform.localPosition;
                _basePosSet = true;
            }

            float time = Time.time;
            const float intensity = 0.02f;

            ventButton.transform.localPosition = _buttonBasePos + new Vector3(
                Mathf.Sin(time * 30f) * intensity,
                Mathf.Cos(time * 25f) * intensity,
                0f);
        }
        else if (_basePosSet)
        {
            ventButton.transform.localPosition = _buttonBasePos;
            _basePosSet = false;
        }

        var timerText = GetOrCreateTimerText(ventButton);
        if (timerText == null)
            return;

        if (player.inVent)
        {
            float maxDuration = OptionGroupSingleton<VentableModifierOptions>.Instance.VentDuration.Value;
            if (maxDuration > 0f)
            {
                float remaining = Mathf.Max(0f, maxDuration - mod.VentDurationTimer);
                ventButton.SetCoolDown(remaining, maxDuration);
                SetTimerText(timerText, remaining);
                return;
            }
        }
        else if (mod.CooldownTimer > 0f)
        {
            float maxCd = OptionGroupSingleton<VentableModifierOptions>.Instance.VentCooldown.Value;
            ventButton.SetCoolDown(mod.CooldownTimer, maxCd);
            SetTimerText(timerText, mod.CooldownTimer);
            return;
        }

        ventButton.SetCoolDown(0f, 1f);
        timerText.gameObject.SetActive(false);
    }

    private static void SetTimerText(TextMeshPro timerText, float seconds)
    {
        if (timerText == null || timerText.gameObject == null)
            return;

        timerText.gameObject.SetActive(true);
        timerText.enabled = true;

        if (seconds <= 10f && seconds > 0f)
        {
            int whole = Mathf.FloorToInt(seconds);
            int frac = Mathf.FloorToInt((seconds - whole) * 10f);
            timerText.text = $"{whole}.{frac}";
        }
        else
        {
            timerText.text = Mathf.CeilToInt(seconds).ToString();
        }

        timerText.color = Color.white;
    }

    private static void CleanupUI(HudManager hud)
    {
        if (hud == null)
            return;

        var ventButton = hud.ImpostorVentButton;
        if (ventButton == null || ventButton.transform == null)
            return;

        var existing = ventButton.transform.Find(TimerTextName);
        if (existing != null && existing.gameObject != null)
            existing.gameObject.SetActive(false);

        _basePosSet = false;
    }

    private static TextMeshPro? GetOrCreateTimerText(ActionButton ventButton)
    {
        if (ventButton == null || ventButton.transform == null)
            return null;

        var existing = ventButton.transform.Find(TimerTextName);
        if (existing != null)
            return existing.GetComponent<TextMeshPro>();

        var source = FindSourceTimerText();
        if (source == null)
            return null;

        var timer = UnityEngine.Object.Instantiate(source, ventButton.transform);
        timer.name = TimerTextName;
        timer.transform.localPosition = new Vector3(0f, 0f, -1f);
        timer.color = Color.white;
        timer.fontSize = source.fontSize;
        timer.fontSizeMax = source.fontSizeMax;
        timer.fontSizeMin = source.fontSizeMin;
        timer.enabled = true;
        timer.gameObject.SetActive(true);

        return timer;
    }

    private static TextMeshPro? FindSourceTimerText()
    {
        var hud = HudManager.Instance;
        if (hud == null)
            return null;

        if (hud.AbilityButton?.cooldownTimerText != null)
            return hud.AbilityButton.cooldownTimerText;

        if (hud.KillButton?.cooldownTimerText != null)
            return hud.KillButton.cooldownTimerText;

        if (hud.SabotageButton?.cooldownTimerText != null)
            return hud.SabotageButton.cooldownTimerText;

        return null;
    }
}