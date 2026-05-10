using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TMPro;
using TownOfUs.Assets;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Ventable;

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

    [HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
    [HarmonyPrefix]
    public static void EnterVentPrefix(PlayerControl pc, out bool __state)
    {
        __state = pc != null && pc.inVent;
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
    [HarmonyPostfix]
    public static void EnterVentPostfix(PlayerControl pc, bool __state)
    {
        if (pc == null || !pc.AmOwner || __state)
            return;

        var mod = pc.GetModifier<VentableModifier>();
        if (mod == null)
            return;

        mod.VentsRemaining--;
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.ExitVent))]
    [HarmonyPostfix]
    public static void ExitVentPostfix(PlayerControl pc)
    {
        if (pc == null || !pc.AmOwner)
            return;

        var mod = pc.GetModifier<VentableModifier>();
        if (mod == null)
            return;

        mod.CooldownTimer = OptionGroupSingleton<VentableModifierOptions>.Instance.VentCooldown.Value;
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

        var usesCounter = GetOrCreateUsesCounter(ventButton);
        if (usesCounter != null)
        {
            usesCounter.spriteRenderer.sprite = TouAssets.AbilityCounterVentSprite.LoadAsset();
            usesCounter.textMesh.text = mod.VentsRemaining.ToString();
            
            // Show only if MaxVentUses > 1 (as requested "dependent on settings")
            bool showCounter = OptionGroupSingleton<VentableModifierOptions>.Instance.MaxVentUses.Value > 1f;
            usesCounter.gameObject.SetActive(showCounter);
        }

        if (mod != null && mod.IsShaking)
        {
            if (!_basePosSet)
            {
                _buttonBasePos = ventButton.transform.localPosition;
                _basePosSet = true;
            }

            float maxDuration = OptionGroupSingleton<VentableModifierOptions>.Instance.VentDuration.Value;
            float remaining = Mathf.Max(0f, maxDuration - mod.VentDurationTimer);

            var urgency = Mathf.Clamp01((3f - remaining) / 3f);
            var amp = Mathf.Lerp(0.01f, 0.06f, urgency);
            var speed = Mathf.Lerp(18f, 35f, urgency);

            var nx = Mathf.PerlinNoise(Time.time * speed, 0.123f) - 0.5f;
            var ny = Mathf.PerlinNoise(0.456f, Time.time * speed) - 0.5f;

            ventButton.transform.localPosition = _buttonBasePos + new Vector3(nx * amp, ny * amp, 0f);
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
                if (ventButton != null && ventButton.graphic != null)
                    ventButton.SetCoolDown(remaining, maxDuration);
                SetTimerText(timerText, remaining);
                return;
            }
        }
        else if (mod.CooldownTimer > 0f)
        {
            float maxCd = OptionGroupSingleton<VentableModifierOptions>.Instance.VentCooldown.Value;
            if (ventButton != null && ventButton.graphic != null)
                ventButton.SetCoolDown(mod.CooldownTimer, maxCd);
            SetTimerText(timerText, mod.CooldownTimer);
            return;
        }

        if (ventButton != null && ventButton.graphic != null)
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

        var existingTimer = ventButton.transform.Find(TimerTextName);
        if (existingTimer != null && existingTimer.gameObject != null)
            existingTimer.gameObject.SetActive(false);

        var existingCounter = ventButton.transform.Find("VentableUsesCounter");
        if (existingCounter != null && existingCounter.gameObject != null)
            existingCounter.gameObject.SetActive(false);

        _basePosSet = false;
    }

    private sealed class UsesCounterRefs
    {
        public GameObject gameObject;
        public SpriteRenderer spriteRenderer;
        public TextMeshPro textMesh;
    }

    private static UsesCounterRefs? GetOrCreateUsesCounter(ActionButton ventButton)
    {
        if (ventButton == null || ventButton.transform == null)
            return null;

        var existing = ventButton.transform.Find("VentableUsesCounter");
        if (existing != null)
        {
            return new UsesCounterRefs
            {
                gameObject = existing.gameObject,
                spriteRenderer = existing.GetComponent<SpriteRenderer>(),
                textMesh = existing.transform.Find("Text")?.GetComponent<TextMeshPro>()
            };
        }

        // Try to find a source to clone from (like the kill button's uses counter)
        var hud = HudManager.Instance;
        if (hud == null) return null;

        var killButton = hud.KillButton;
        if (killButton == null || killButton.usesRemainingSprite == null) return null;

        var counterObj = UnityEngine.Object.Instantiate(killButton.usesRemainingSprite.gameObject, ventButton.transform);
        counterObj.name = "VentableUsesCounter";
        counterObj.transform.localPosition = new Vector3(-0.35f, 0.35f, -1f); // Top left-ish
        counterObj.transform.localScale = Vector3.one * 0.8f;

        var sr = counterObj.GetComponent<SpriteRenderer>();
        var text = counterObj.transform.Find("Text")?.GetComponent<TextMeshPro>();

        if (text == null)
        {
            // If Text child is missing, try to find it in the clone or create it
            text = counterObj.GetComponentInChildren<TextMeshPro>();
        }

        return new UsesCounterRefs
        {
            gameObject = counterObj,
            spriteRenderer = sr,
            textMesh = text
        };
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















