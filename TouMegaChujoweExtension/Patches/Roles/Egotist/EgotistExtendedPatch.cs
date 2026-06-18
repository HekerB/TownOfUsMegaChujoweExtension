using AmongUs.GameOptions;
using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using TMPro;
using TownOfUs.Buttons;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs.Roles;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Extensions;
using TownOfUs;
using TownOfUs.Utilities;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Modules;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Egotist;

[HarmonyPatch]
public static class EgotistExtendedPatch
{
    private static float _ventTimer;
    private static float _ventCooldownTimer;
    private static bool _onCooldown;
    private const string TimerTextName = "EgotistVentTimerText";

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    public static class EgotistGameEndPatch
    {
        public static void Postfix() => ResetVentState();
    }

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
    public static class EgotistExileWrapUpPatch
    {
        public static void Postfix() => ResetVentState();
    }

    public static void ResetVentState()
    {
        _ventTimer = 0f;
        _ventCooldownTimer = 0f;
        _onCooldown = false;
    }

    private static bool IsLocalEgotist(out EgotistExtendedOptions? opts)
    {
        opts = null;
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data == null || player.Data.Role == null || player.Data.IsDead) return false;
        if (player.gameObject.GetComponent<ModifierComponent>() == null) return false;
        if (!player.TryGetModifier<EgotistModifier>(out _)) return false;
        opts = OptionGroupSingleton<EgotistExtendedOptions>.Instance;
        return true;
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPrefix]
    public static void HudManagerUpdatePrefix()
    {
        if (!IsLocalEgotist(out var opts) || opts is null) return;

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data == null || player.Data.Role == null) return;


        if (opts.CanVent)
        {
            // If Egotist can vent, we take control over Role.CanVent to suppress vanilla/other mod logic
            // and use our own cooldown system.
            player.Data.Role.CanVent = !_onCooldown;
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void HudManagerUpdatePostfix(HudManager __instance)
    {
        if (!IsLocalEgotist(out var opts) || opts is null) return;

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data == null || player.Data.Role == null) return;

        // Check if the player is an Engineer (vanilla or Tou)
        bool isEngineer = player.Data.Role.Role == RoleTypes.Engineer ||
                          player.Data.Role is EngineerRole ||
                          player.Data.Role is EngineerTouRole;

        if (MeetingHud.Instance != null)
        {
            __instance.ImpostorVentButton?.gameObject.SetActive(false);
            return;
        }

        if (opts.CanVent)
        {
            PlayerControl.LocalPlayer.Data.Role.CanVent = !_onCooldown;

            var fakeVent = CustomButtonSingleton<FakeVentButton>.Instance;

            if (isEngineer)
            {
                // Hide Egotist's red button, use Engineer's native button instead
                __instance.ImpostorVentButton?.gameObject.SetActive(false);

                if (fakeVent != null)
                {
                    fakeVent.Show = true;
                    if (!_onCooldown) fakeVent.Timer = 0f;
                    else fakeVent.Timer = _ventCooldownTimer;
                }
            }
            else
            {
                // Standard Egotist flow (not an Engineer)
                if (fakeVent != null) fakeVent.Show = false;

                if (__instance.ImpostorVentButton != null)
                {
                    bool shouldBeActive = !_onCooldown || _ventCooldownTimer > 0;
                    if (__instance.ImpostorVentButton.gameObject.activeSelf != shouldBeActive)
                        __instance.ImpostorVentButton.gameObject.SetActive(shouldBeActive);

                    if (__instance.ImpostorVentButton.graphic != null)
                        __instance.ImpostorVentButton.graphic.enabled = shouldBeActive;
                }
            }
        }
        else
        {
            var role = player.Data.Role;
            bool isNeutral = role.GetRoleAlignment() is RoleAlignment.NeutralKilling or
                                              RoleAlignment.NeutralEvil or
                                              RoleAlignment.NeutralBenign or
                                              RoleAlignment.NeutralOutlier;

            if (!role.IsImpostor && !isNeutral)
                __instance.ImpostorVentButton?.gameObject.SetActive(false);
        }

        if (_onCooldown)
        {
            _ventCooldownTimer -= Time.deltaTime;
            if (_ventCooldownTimer <= 0f)
            {
                _onCooldown = false;
                _ventCooldownTimer = 0f;
            }
        }

        var button = __instance.ImpostorVentButton;
        if (button == null) return;
        var timerText = GetOrCreateTimerText(button);
        if (timerText == null) return;

        if (player.inVent && opts.CanVent)
        {
            _ventTimer += Time.deltaTime;
            float remainingInVent = Mathf.Max(0f, opts.MaxVentTime - _ventTimer);
            VentUtilities.InitializeVentButton(button);
            VentUtilities.TrySetCooldown(button, remainingInVent, opts.MaxVentTime);
            SetTimerText(timerText, remainingInVent);

            if (_ventTimer >= opts.MaxVentTime)
            {
                if (Vent.currentVent != null)
                {
                    Vent.currentVent.SetButtons(false);
                    player.MyPhysics.RpcExitVent(Vent.currentVent.Id);
                }

                _ventTimer = 0f;
                _onCooldown = true;
                _ventCooldownTimer = opts.VentCooldown;
            }
        }
        else if (_onCooldown)
        {
            VentUtilities.InitializeVentButton(button);
            VentUtilities.TrySetCooldown(button, _ventCooldownTimer, opts.VentCooldown);
            SetTimerText(timerText, _ventCooldownTimer);
        }
        else
        {
            _ventTimer = 0f;
            VentUtilities.InitializeVentButton(button);
            VentUtilities.TrySetCooldown(button, 0f, 1f);
            timerText.gameObject.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.ExitVent))]
    [HarmonyPostfix]
    public static void ExitVentPostfix(PlayerControl pc)
    {
        if (pc == null || !pc.AmOwner) return;
        if (!pc.TryGetModifier<EgotistModifier>(out _)) return;

        var opts = OptionGroupSingleton<EgotistExtendedOptions>.Instance;
        if (opts == null || !opts.CanVent) return;

        // Manual exit also triggers cooldown
        if (!_onCooldown)
        {
            _onCooldown = true;
            _ventCooldownTimer = opts.VentCooldown;
        }
    }

    private static void SetTimerText(TextMeshPro timerText, float seconds)
    {
        if (timerText == null || timerText.gameObject == null) return;
        timerText.gameObject.SetActive(true);
        timerText.enabled = true;
        timerText.text = Mathf.CeilToInt(seconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        timerText.color = Color.white;
    }

    private static TextMeshPro? GetOrCreateTimerText(ActionButton button)
    {
        if (button == null) return null;
        var existing = button.transform.Find(TimerTextName);
        if (existing != null) return existing.GetComponent<TextMeshPro>();

        var source = HudManager.Instance?.KillButton?.cooldownTimerText;
        if (source == null) return null;

        var timer = UnityEngine.Object.Instantiate(source, button.transform);
        timer.name = TimerTextName;
        timer.transform.localPosition = new Vector3(0f, 0f, -1f);
        timer.color = Color.white;
        timer.gameObject.SetActive(true);
        return timer;
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void VentCanUsePostfix(Vent __instance, NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result)
    {
        if (pc?.Object == null) return;

        if (VentOccupancySystem.IsBlocked(__instance.Id) && !(pc.Object.inVent && Vent.currentVent != null && __instance.Id == Vent.currentVent.Id))
        {
            canUse = false;
            couldUse = false;
            __result = float.MaxValue;
            return;
        }

        if (pc.Object.gameObject.GetComponent<ModifierComponent>() == null) return;
        if (!pc.Object.TryGetModifier<EgotistModifier>(out _)) return;
        if (!OptionGroupSingleton<EgotistExtendedOptions>.Instance.CanVent) return;

        if (pc.Object.AmOwner && _onCooldown && !pc.Object.inVent)
        {
            canUse = false;
            couldUse = false;
            __result = float.MaxValue;
            return;
        }

        couldUse = !pc.IsDead && (pc.Object.CanMove || pc.Object.inVent);
        canUse = couldUse;

        if (canUse && !pc.Object.inVent)
        {
            var truePos = pc.Object.GetTruePosition();
            var ventPos = __instance.transform.position;
            var dist = Vector2.Distance(truePos, ventPos);
            canUse = dist <= __instance.UsableDistance &&
                     !PhysicsHelpers.AnythingBetween(truePos, ventPos, Constants.ShipAndObjectsMask, false);
        }

        __result = canUse ? 0f : float.MaxValue;
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
    [HarmonyPostfix]
    public static void CalculateLightRadiusPostfix(ShipStatus __instance, NetworkedPlayerInfo player, ref float __result)
    {
        if (player?.Object == null) return;
        if (player.Object.gameObject.GetComponent<ModifierComponent>() == null) return;
        if (!player.Object.TryGetModifier<EgotistModifier>(out _)) return;
        if (!OptionGroupSingleton<EgotistExtendedOptions>.Instance.ImpostorVision) return;

        float egotistVision = __instance.MaxLightRadius * GameOptionsManager.Instance.currentNormalGameOptions.ImpostorLightMod;
        __result = Mathf.Max(__result, egotistVision);
    }
}
