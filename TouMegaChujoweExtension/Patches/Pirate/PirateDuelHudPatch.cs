using HarmonyLib;
using TouMegaChujoweExtension.Roles.Neutral;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Pirate;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class PirateDuelHudPatch
{
    private static TMPro.TextMeshPro? _duelCounter;

    public static void Postfix(HudManager __instance)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.Data?.Role is not PirateRole pirateRole)
        {
            if (_duelCounter != null)
            {
                _duelCounter.gameObject.SetActive(false);
            }

            return;
        }

        if (_duelCounter == null)
        {
            var taskText = __instance.TaskPanel?.transform;
            if (taskText == null)
            {
                return;
            }

            var counterObj = new GameObject("PirateDuelCounter");
            counterObj.transform.SetParent(__instance.transform);
            counterObj.transform.localPosition = new Vector3(-4.5f, -2.5f, -5f);

            _duelCounter = counterObj.AddComponent<TMPro.TextMeshPro>();
            _duelCounter.fontSize = 2.2f;
            _duelCounter.alignment = TMPro.TextAlignmentOptions.Left;
            _duelCounter.sortingOrder = 10;
        }

        var duelsNeeded = (int)OptionGroupSingleton<PirateOptions>.Instance.DuelsToWin.Value;
        _duelCounter.text = $"<color=#{ColorUtility.ToHtmlStringRGB(TouExtensionColors.Pirate)}>Duels won: {pirateRole.DuelsWon} / {duelsNeeded}</color>";
        _duelCounter.gameObject.SetActive(true);
    }
}