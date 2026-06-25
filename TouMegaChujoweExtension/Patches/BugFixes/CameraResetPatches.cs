using HarmonyLib;
using UnityEngine;
using MiraAPI.Hud;
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
            if (HudManager.Instance != null && HudManager.Instance.ShadowQuad != null)
            {
                HudManager.Instance.ShadowQuad.gameObject.SetActive(true);
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
