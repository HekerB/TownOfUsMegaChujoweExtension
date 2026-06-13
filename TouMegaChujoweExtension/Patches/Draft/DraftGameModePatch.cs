using HarmonyLib;
using MiraAPI.GameOptions;
using TMPro;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Draft;

[HarmonyPatch]
public static class DraftGameModePatch
{
    private const string DraftModeText = "<color=#00FF00>DRAFT MODE</color>";
    private const string VanillaModeText = "<color=#FF0000>VANILLA MODE</color>";

    private static TextMeshPro? _cachedModeText;
    private static bool? _lastDraftMode;
    private static int _cachedDraftModeFrame = -1;
    private static bool _cachedDraftMode;

    public static bool IsDraftMode()
    {
        if (_cachedDraftModeFrame == Time.frameCount)
        {
            return _cachedDraftMode;
        }

        try
        {
            _cachedDraftMode = OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode;
        }
        catch
        {
            _cachedDraftMode = false;
        }

        _cachedDraftModeFrame = Time.frameCount;
        return _cachedDraftMode;
    }

    [HarmonyPatch(typeof(MiscUtils), nameof(MiscUtils.CurrentGamemode))]
    [HarmonyPostfix]
    public static void CurrentGamemodePostfix(ref TouGamemode __result)
    {
        if (IsDraftMode())
        {
            __result = TouGamemode.Normal;
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        if (_cachedModeText == null)
        {
            var modeValue = __instance.transform.Find("LobbyInfoPane/AspectSize/ModeValue");
            if (modeValue == null)
            {
                return;
            }

            foreach (var text in modeValue.GetComponentsInChildren<TextMeshPro>(true))
            {
                if (!IsModeText(text))
                {
                    continue;
                }

                _cachedModeText = text;
                break;
            }
        }

        if (_cachedModeText == null)
        {
            return;
        }

        var isDraftMode = IsDraftMode();
        var desiredText = isDraftMode ? DraftModeText : VanillaModeText;
        if (_lastDraftMode == isDraftMode && _cachedModeText.text == desiredText)
        {
            return;
        }

        _cachedModeText.text = desiredText;
        _lastDraftMode = isDraftMode;
    }

    private static bool IsModeText(TextMeshPro text)
    {
        if (text.name.Contains("GameModeText") || text.name.Contains("Preset", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var value = text.text;
        return value.Contains("CUSTOM", System.StringComparison.OrdinalIgnoreCase) ||
               value.Contains("DRAFT MODE", System.StringComparison.OrdinalIgnoreCase) ||
               value.Contains("VANILLA MODE", System.StringComparison.OrdinalIgnoreCase);
    }
}
