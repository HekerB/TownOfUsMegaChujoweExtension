using HarmonyLib;
using MiraAPI.GameOptions;
using TMPro;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Draft;

[HarmonyPatch]
public static class DraftGameModePatch
{
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
        ApplyModeText(__instance);
    }

    // The lobby pane rewrites its own preset label (back to the preset name, e.g. "Classic") when it refreshes;
    // applying right after it means its text never wins the frame.
    [HarmonyPatch(typeof(LobbyInfoPane), nameof(LobbyInfoPane.Update))]
    [HarmonyPostfix]
    public static void LobbyInfoPaneUpdatePostfix()
    {
        if (HudManager.InstanceExists)
        {
            ApplyModeText(HudManager.Instance);
        }
    }

    [HarmonyPatch(typeof(LobbyInfoPane), nameof(LobbyInfoPane.RefreshPane))]
    [HarmonyPostfix]
    public static void LobbyInfoPaneRefreshPostfix()
    {
        if (HudManager.InstanceExists)
        {
            ApplyModeText(HudManager.Instance);
        }
    }

    private static void ApplyModeText(HudManager hud)
    {
        if (_cachedModeText == null)
        {
            _cachedModeText = null;
            var modeValue = hud.transform.Find("LobbyInfoPane/AspectSize/ModeValue");
            if (modeValue == null)
            {
                return;
            }

            TextMeshPro? fallback = null;
            foreach (var text in modeValue.GetComponentsInChildren<TextMeshPro>(true))
            {
                fallback ??= text;
                if (!IsModeText(text))
                {
                    continue;
                }

                _cachedModeText = text;
                break;
            }

            // A pane rebuilt on returning to the lobby only says the preset's name ("Classic"); the value text is
            // still the one under ModeValue.
            if (_cachedModeText == null)
            {
                _cachedModeText = fallback;
            }
        }

        if (_cachedModeText == null)
        {
            return;
        }

        var isDraftMode = IsDraftMode();
        var desiredText = isDraftMode
            ? $"<color=#00FF00>{TouLocale.Get("ExtensionDraftModeHeader", "DRAFT MODE")}</color>"
            : $"<color=#FF0000>{TouLocale.Get("ExtensionDraftModeVanillaMode", "VANILLA MODE")}</color>";
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
