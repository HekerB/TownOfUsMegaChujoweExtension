using HarmonyLib;
using MiraAPI.GameOptions;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Pirate;

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
        var counter = TouLocale.Get("ExtensionPirateDuelsWonCounter", "Duels won: {0} / {1}")
            .Replace("{0}", pirateRole.DuelsWon.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Replace("{1}", duelsNeeded.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _duelCounter.text = $"<color=#{ColorUtility.ToHtmlStringRGB(TouExtensionColors.Pirate)}>{counter}</color>";
        _duelCounter.gameObject.SetActive(true);
    }
}














