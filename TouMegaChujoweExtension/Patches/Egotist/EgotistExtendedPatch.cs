using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Options.Modifiers;
using TownOfUs.Buttons;
using TownOfUs.Modifiers.Game.Alliance;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Egotist;

[HarmonyPatch]
public static class EgotistExtendedPatch
{
    private static float _ventTimer;
    private static float _ventCooldownTimer;
    private static bool _onCooldown;

    public static void ResetVentState()
    {
        _ventTimer = 0f;
        _ventCooldownTimer = 0f;
        _onCooldown = false;
    }

    private static bool IsLocalEgotist(out EgotistExtendedOptions opts)
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
        if (!IsLocalEgotist(out var opts)) return;
        if (!opts.CanVent) return;

        PlayerControl.LocalPlayer.Data.Role.CanVent = !_onCooldown;
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void HudManagerUpdatePostfix(HudManager __instance)
    {
        if (!IsLocalEgotist(out var opts)) return;

        if (MeetingHud.Instance != null)
        {
            try
            {
                if (__instance.ImpostorVentButton != null)
                    __instance.ImpostorVentButton.gameObject.SetActive(false);
            }
            catch { }
            return;
        }

        if (opts.CanVent)
        {
            PlayerControl.LocalPlayer.Data.Role.CanVent = !_onCooldown;

            try { CustomButtonSingleton<FakeVentButton>.Instance.Show = false; } catch { }

            try
            {
                if (__instance.ImpostorVentButton != null && !_onCooldown)
                    __instance.ImpostorVentButton.gameObject.SetActive(true);
            }
            catch { }
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

        var player = PlayerControl.LocalPlayer;
        if (player.inVent && opts.CanVent)
        {
            _ventTimer += Time.deltaTime;
            if (_ventTimer >= opts.MaxVentTime.Value)
            {
                if (Vent.currentVent != null)
                {
                    Vent.currentVent.SetButtons(false);
                    player.MyPhysics.RpcExitVent(Vent.currentVent.Id);
                }

                _ventTimer = 0f;
                _onCooldown = true;
                _ventCooldownTimer = opts.VentCooldown.Value;
            }
        }
        else
        {
            _ventTimer = 0f;
        }
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void VentCanUsePostfix(Vent __instance, NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result)
    {
        if (pc?.Object == null) return;
        if (pc.Object.gameObject.GetComponent<ModifierComponent>() == null) return;
        if (!pc.Object.TryGetModifier<EgotistModifier>(out _)) return;
        if (!OptionGroupSingleton<EgotistExtendedOptions>.Instance.CanVent) return;

        if (_onCooldown && !pc.Object.inVent)
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

        __result = __instance.MaxLightRadius * GameOptionsManager.Instance.currentNormalGameOptions.ImpostorLightMod;
    }
}