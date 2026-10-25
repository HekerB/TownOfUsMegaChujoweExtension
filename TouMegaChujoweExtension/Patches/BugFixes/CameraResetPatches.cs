using HarmonyLib;
using UnityEngine;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Buttons.Classic.Crewmate;
using TouMegaChujoweExtension.Buttons.Classic.Impostor;

namespace TouMegaChujoweExtension.Patches.BugFixes;

[HarmonyPatch]
public static class CameraResetPatches
{
    public static void ResetCameraAndShadows()
    {
        try
        {
            var falconBtn = CustomButtonSingleton<FalconZoomButton>.Instance;
            if (falconBtn != null)
            {
                falconBtn.ResetCooldownAndOrEffect();
            }
        }
        catch { }

        try
        {
            var sniperBtn = CustomButtonSingleton<SniperShootButton>.Instance;
            if (sniperBtn != null)
            {
                sniperBtn.ResetCooldownAndOrEffect();
            }
        }
        catch { }

        try
        {
            foreach (var cam in Camera.allCameras)
            {
                if (cam != null)
                {
                    cam.orthographicSize = 3f;
                }
            }
        }
        catch { }

        try
        {
            // Never leave a meeting, the end screen or the lobby upside down after an Inverter flip.
            var local = PlayerControl.LocalPlayer;
            if (local == null || !local.HasModifier<TouMegaChujoweExtension.Modifiers.Impostor.InverterDisorientedModifier>())
            {
                TouMegaChujoweExtension.Modules.InverterCameraBehaviour.ForceUpright(Camera.main);
            }
        }
        catch { }

        try
        {
            // InstanceExists, not a null-check: the Instance getter creates a blank HudManager when
            // none exists, and this runs at game end / lobby load where that zombie then kills the real
            // HUD.
            if (HudManager.InstanceExists && HudManager.Instance.ShadowQuad != null)
            {
                // Not simply on: a ghost keeps seeing the whole map after a meeting.
                ShadowQuadUtils.Restore();
            }
        }
        catch { }

        try
        {
            ResolutionManager.ResolutionChanged?.Invoke(
                (float)Screen.width / Screen.height, Screen.width, Screen.height, Screen.fullScreen);
        }
        catch { }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    [HarmonyPostfix]
    public static void OnGameEndPostfix()
    {
        ResetCameraAndShadows();
    }

    [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
    [HarmonyPostfix]
    public static void LobbyBehaviourStartPostfix()
    {
        ResetCameraAndShadows();
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.OnDestroy))]
    [HarmonyPostfix]
    public static void ShipStatusOnDestroyPostfix()
    {
        ResetCameraAndShadows();
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    public static void MeetingHudStartPostfix()
    {
        ResetCameraAndShadows();
    }
}
