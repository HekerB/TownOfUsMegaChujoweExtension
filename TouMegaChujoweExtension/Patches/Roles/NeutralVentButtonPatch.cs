using TownOfUs.Utilities;
using HarmonyLib;
using MiraAPI.LocalSettings;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Extensions;
using UnityEngine;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Modules;
using TownOfUs;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Patches.Roles;

[HarmonyPatch]
public static class NeutralVentButtonPatch
{
    private static bool? _lastHasTarget;
    private static bool? _lastCanVent;
    private static bool IsMeetingOpen => MeetingHud.Instance != null || ExileController.Instance != null;
    private static bool OffsetButtonsWhenCantVent =>
        LocalSettingsTabSingleton<TownOfUsLocalSettings>.Instance.OffsetButtonsToggle.Value;

    private static bool? GetBerserkerVentState(PlayerControl? player)
    {
        return player?.Data?.Role switch
        {
            BerserkerRole berserkerRole => berserkerRole.CanVentByState(),
            WarRole warRole => warRole.CanVentByState(),
            _ => null
        };
    }

    private static void CleanupVentButtonForMeeting(VentButton? ventButton)
    {
        _lastHasTarget = null;

        if (ventButton == null)
        {
            return;
        }

        ventButton.currentTarget = null;

        if (ventButton.cooldownTimerText != null)
        {
            ventButton.cooldownTimerText.gameObject.SetActive(false);
        }
    }

    private static void HideVentButtonWhenOffset(VentButton? ventButton)
    {
        _lastHasTarget = null;

        if (ventButton == null)
        {
            return;
        }

        ventButton.currentTarget = null;
        ventButton.gameObject?.SetActive(false);
    }

    private static void RestoreVentButtonGraphic(VentButton? ventButton)
    {
        _lastHasTarget = null;

        if (ventButton == null)
        {
            return;
        }

        ventButton.currentTarget = null;

        if (ventButton.graphic != null)
        {
            if (!ventButton.graphic.gameObject.activeSelf)
            {
                ventButton.graphic.gameObject.SetActive(true);
            }

            ventButton.graphic.enabled = true;
            var color = ventButton.graphic.color;
            color.r = 1f;
            color.g = 1f;
            color.b = 1f;
            color.a = 1f;
            ventButton.graphic.color = color;
        }

        if (ventButton.buttonLabelText != null)
        {
            ventButton.buttonLabelText.gameObject.SetActive(true);
        }

        if (ventButton.cooldownTimerText != null)
        {
            ventButton.cooldownTimerText.gameObject.SetActive(false);
        }
    }

    private static void SetVentButtonState(VentButton ventButton, bool canVent, Color roleColor, Sprite activeSprite)
    {
        if (ventButton == null || ventButton.gameObject == null)
        {
            return;
        }

        if (!_lastCanVent.HasValue || _lastCanVent.Value != canVent)
        {
            _lastCanVent = canVent;
            _lastHasTarget = null;
        }

        if (!canVent && OffsetButtonsWhenCantVent)
        {
            HideVentButtonWhenOffset(ventButton);
            return;
        }

        if (!canVent && ventButton.currentTarget != null)
        {
            ventButton.currentTarget = null;
        }

        if (!ventButton.gameObject.activeSelf)
        {
            ventButton.gameObject.SetActive(true);
        }

        if (ventButton.graphic != null)
        {
            if (!ventButton.graphic.gameObject.activeSelf)
            {
                ventButton.graphic.gameObject.SetActive(true);
            }

            ventButton.graphic.enabled = true;
            ventButton.graphic.sprite = activeSprite;

            if (!canVent)
            {
                var graphicColor = Color.white;
                graphicColor.a = 0.32f;
                ventButton.graphic.color = graphicColor;
            }
        }

        if (ventButton.buttonLabelText != null)
        {
            if (!ventButton.buttonLabelText.gameObject.activeSelf)
            {
                ventButton.buttonLabelText.gameObject.SetActive(true);
            }

            var alpha = canVent ? 1f : 0.45f;
            var outline = roleColor;
            outline.a = alpha;

            ventButton.buttonLabelText.text = canVent
                ? TouLocale.Get("Vent", "Vent")
                : TouLocale.Get("ExtensionRoleCantVent", "Can't Vent");

            if (!canVent)
            {
                ventButton.buttonLabelText.color = new Color(1f, 1f, 1f, alpha);
                ventButton.buttonLabelText.outlineColor = outline;
                ventButton.buttonLabelText.fontMaterial?.SetColor("_OutlineColor", outline);
            }
        }

        if (ventButton.cooldownTimerText != null)
        {
            ventButton.cooldownTimerText.gameObject.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(VentButton), nameof(VentButton.DoClick))]
    [HarmonyPrefix]
    public static bool DoClickPrefix()
    {
        if (IsMeetingOpen)
        {
            return false;
        }

        return GetBerserkerVentState(PlayerControl.LocalPlayer) != false;
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
    [HarmonyPostfix]
    public static void CanUseVentPostfix(
        Vent __instance,
        NetworkedPlayerInfo pc,
        ref bool canUse,
        ref bool couldUse,
        ref float __result)
    {
        if (__instance == null || pc?.Object == null || pc.IsDead || pc.Disconnected)
        {
            return;
        }

        var player = pc.Object;
        var canRoleVent = GetBerserkerVentState(player);

        if (!canRoleVent.HasValue)
        {
            return;
        }

        if (!canRoleVent.Value)
        {
            canUse = false;
            couldUse = false;
            __result = float.MaxValue;
            return;
        }

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

        var truePosition = player.GetTruePosition();
        var ventPosition = (Vector2)__instance.transform.position;
        var distance = Vector2.Distance(truePosition, ventPosition);
        var inRange = distance <= __instance.UsableDistance;
        var clearPath = !PhysicsHelpers.AnythingBetween(truePosition, ventPosition, Constants.ShipOnlyMask, false);

        couldUse = inRange && clearPath;
        canUse = couldUse;
        __result = distance;
    }

    [HarmonyPatch(typeof(VentButton), nameof(VentButton.SetTarget))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void SetTargetPostfix(VentButton __instance)
    {
        if (IsMeetingOpen)
        {
            CleanupVentButtonForMeeting(__instance);
            return;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data == null) return;

        var berserkerVentState = GetBerserkerVentState(player);
        if (berserkerVentState == false)
        {
            var roleColor = player.Data.Role is BerserkerRole berserkerRole ? berserkerRole.RoleColor : TouExtensionColors.War;
            SetVentButtonState(__instance, false, roleColor, TownOfUs.Assets.TouNeutAssets.WerewolfVentSprite.LoadAsset());
            return;
        }

        Sprite? customSprite = null;
        if (player.IsRole<SerialKillerRole>())
        {
            customSprite = TouExtensionNeuAssets.SerialKillerVentButtonSprite.LoadAsset();
        }
        else if (player.IsRole<ShroudRole>())
        {
            customSprite = TouExtensionNeuAssets.ShroudVentSprite.LoadAsset();
        }
        else if (player.IsRole<DoppelgangerRole>())
        {
            customSprite = TouExtensionNeuAssets.DoppelgangerVentSprite.LoadAsset();
        }
        else if (player.IsRole<FamineRole>())
        {
            customSprite = TouExtensionNeuAssets.DoppelgangerVentSprite.LoadAsset();
        }
        else if (player.Data.Role is BerserkerRole { IsWar: true })
        {
            customSprite = TownOfUs.Assets.TouNeutAssets.WerewolfVentSprite.LoadAsset();
        }
        else if (player.IsRole<BerserkerRole>())
        {
            customSprite = TouAssets.VentSprite.LoadAsset();
        }
        else if (player.IsRole<WarRole>())
        {
            customSprite = TownOfUs.Assets.TouNeutAssets.WerewolfVentSprite.LoadAsset();
        }
        else if (player.IsRole<VultureRole>())
        {
            customSprite = TownOfUs.Assets.TouNeutAssets.WerewolfVentSprite.LoadAsset();
        }

        if (customSprite != null && __instance.graphic != null && __instance.graphic.sprite != customSprite)
        {
            __instance.graphic.sprite = customSprite;
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        if (IsMeetingOpen)
        {
            CleanupVentButtonForMeeting(__instance?.ImpostorVentButton);
            return;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data?.Role == null) return;

        var ventButton = __instance.ImpostorVentButton;
        if (ventButton == null || ventButton.buttonLabelText == null) return;

        Color? roleColor = null;
        var role = player.Data.Role;

        if (role is SerialKillerRole) roleColor = TouExtensionColors.SerialKiller;
        else if (role is ShroudRole) roleColor = TouExtensionColors.Shroud;
        else if (role is DoppelgangerRole) roleColor = TouExtensionColors.Doppelganger;
        else if (role is FamineRole) roleColor = TouExtensionColors.Famine;
        else if (role is BerserkerRole berserkerRole)
        {
            var shouldShowVent = berserkerRole.CanVentByState();
            if (player.Data.Role != null && player.Data.Role.CanVent != shouldShowVent)
            {
                player.Data.Role.CanVent = shouldShowVent;
            }

            var ventSprite = berserkerRole.IsWar
                ? TownOfUs.Assets.TouNeutAssets.WerewolfVentSprite.LoadAsset()
                : TouAssets.VentSprite.LoadAsset();
            SetVentButtonState(ventButton, shouldShowVent, berserkerRole.RoleColor, ventSprite);

            if (!shouldShowVent)
            {
                return;
            }

            roleColor = berserkerRole.RoleColor;
        }
        else if (role is WarRole warRole)
        {
            var shouldShowVent = warRole.CanVentByState();
            if (player.Data.Role != null && player.Data.Role.CanVent != shouldShowVent)
            {
                player.Data.Role.CanVent = shouldShowVent;
            }

            SetVentButtonState(ventButton, shouldShowVent, TouExtensionColors.War, TownOfUs.Assets.TouNeutAssets.WerewolfVentSprite.LoadAsset());

            if (!shouldShowVent)
            {
                return;
            }

            roleColor = TouExtensionColors.War;
        }
        else if (role is VultureRole) roleColor = TouExtensionColors.Vulture;
        else if (role is PelicanRole) roleColor = TouExtensionColors.Pelican;
        else if (role is JackalRole) roleColor = TouExtensionColors.Jackal;

        if (roleColor.HasValue)
        {
            var hasTarget = ventButton.currentTarget != null || player.inVent;
            
            if (!_lastHasTarget.HasValue || _lastHasTarget.Value != hasTarget)
            {
                _lastHasTarget = hasTarget;
                var alpha = hasTarget ? 1f : 0.3f;
                var finalColor = roleColor.Value;
                finalColor.a = alpha;

                if (ventButton.graphic != null)
                {
                    ventButton.graphic.color = hasTarget
                        ? Color.white
                        : new Color(0.55f, 0.55f, 0.55f, alpha);
                }

                // Always force the color for these roles to prevent flickering with default Impostor red
                ventButton.buttonLabelText.color = new Color(1f, 1f, 1f, alpha);
                ventButton.buttonLabelText.outlineColor = finalColor;
                
                ventButton.buttonLabelText.fontMaterial?.SetColor("_OutlineColor", finalColor);
            }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
    [HarmonyPostfix]
    public static void MeetingClosePostfix()
    {
        RestoreVentButtonGraphic(HudManager.Instance?.ImpostorVentButton);
    }
}















