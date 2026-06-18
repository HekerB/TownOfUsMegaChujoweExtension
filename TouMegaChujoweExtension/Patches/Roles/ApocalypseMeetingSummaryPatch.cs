using HarmonyLib;
using System;
using System.Globalization;
using System.Linq;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TMPro;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Patches.Roles;

[HarmonyPatch(typeof(MeetingHud))]
public static class ApocalypseMeetingSummaryPatch
{
    private const float SummaryFontScale = 0.52f;
    private static readonly Vector3 SummaryOffset = new(0f, -0.18f, -0.01f);
    private static readonly Color SoulCollectorSummaryColor = new Color32(119, 122, 168, 255);
    private static TextMeshPro? _summaryText;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(MeetingHud.Start))]
    public static void StartPostfix(MeetingHud __instance)
    {
        UpdateSummaryText(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(nameof(MeetingHud.UpdateTimerText))]
    public static void UpdateTimerTextPostfix(MeetingHud __instance)
    {
        UpdateSummaryText(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(MeetingHud.Close))]
    public static void ClosePostfix()
    {
        CleanupSummaryText();
    }

    private static void UpdateSummaryText(MeetingHud __instance)
    {
        try
        {
            var timerText = __instance?.TimerText;
            var localPlayer = PlayerControl.LocalPlayer;
            if (timerText == null ||
                localPlayer == null ||
                localPlayer.HasDied() ||
                localPlayer.Data?.Role == null)
            {
                return;
            }

            var summary = GetSummaryLine(localPlayer);
            if (string.IsNullOrEmpty(summary))
            {
                CleanupSummaryText();
                return;
            }

            EnsureSummaryText(timerText);
            if (_summaryText != null)
            {
                _summaryText.text = summary;
                _summaryText.gameObject.SetActive(true);
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[TOUMCE] Apocalypse meeting summary failed: {ex}");
        }
    }

    private static void EnsureSummaryText(TextMeshPro timerText)
    {
        if (_summaryText != null)
        {
            SetSummaryFontSize(timerText);
            _summaryText.transform.localPosition = timerText.transform.localPosition + SummaryOffset;
            _summaryText.transform.localScale = timerText.transform.localScale;
            return;
        }

        _summaryText = Object.Instantiate(timerText, timerText.transform.parent);
        _summaryText.name = "TOUMCE_ApocalypseMeetingSummary";
        Object.Destroy(_summaryText.GetComponent<TextTranslatorTMP>());
        Object.Destroy(_summaryText.GetComponent<TmpMiraTranslator>());
        _summaryText.color = Color.white;
        _summaryText.enableAutoSizing = false;
        SetSummaryFontSize(timerText);
        _summaryText.alignment = TextAlignmentOptions.Right;
        _summaryText.enableWordWrapping = false;
        _summaryText.richText = true;
        _summaryText.sortingLayerID = timerText.sortingLayerID;
        _summaryText.sortingOrder = timerText.sortingOrder + 1;
        _summaryText.transform.localScale = timerText.transform.localScale;
        _summaryText.transform.localPosition = timerText.transform.localPosition + SummaryOffset;
        _summaryText.gameObject.SetActive(true);
    }

    private static void SetSummaryFontSize(TextMeshPro timerText)
    {
        if (_summaryText == null)
        {
            return;
        }

        var fontSize = timerText.fontSize * SummaryFontScale;
        _summaryText.fontSize = fontSize;
        _summaryText.fontSizeMin = fontSize;
        _summaryText.fontSizeMax = fontSize;
    }

    private static void CleanupSummaryText()
    {
        if (_summaryText == null)
        {
            return;
        }

        Object.Destroy(_summaryText.gameObject);
        _summaryText = null;
    }

    private static string GetSummaryLine(PlayerControl localPlayer)
    {
        if (localPlayer.Data.Role is SoulCollectorRole soulCollectorRole)
        {
            var soulsNeeded = SoulCollectorRole.GetEffectiveSoulGoal(localPlayer);
            var activeMarks = SoulCollectorRole.GetActiveMarkCount(localPlayer.PlayerId);
            var maxMarks = (int)OptionGroupSingleton<SoulCollectorOptions>.Instance.MaxMarks;
            var soulText = TouLocale.Get(
                    "ExtensionRoleSoulCollectorMeetingSummary",
                    "Souls Claimed: {0} / {1} | Reap Marks: {2} / {3}")
                .Replace("{0}", soulCollectorRole.SoulsCollected.ToString(CultureInfo.InvariantCulture))
                .Replace("{1}", soulsNeeded.ToString(CultureInfo.InvariantCulture))
                .Replace("{2}", activeMarks.ToString(CultureInfo.InvariantCulture))
                .Replace("{3}", maxMarks.ToString(CultureInfo.InvariantCulture));

            return $"{SoulCollectorSummaryColor.ToTextColor()}{soulText}</color>";
        }

        if (localPlayer.Data.Role is BakerRole)
        {
            var breadCount = PlayerControl.AllPlayerControls.ToArray()
                .Count(player => player != null &&
                                 !player.HasDied() &&
                                 player.HasModifier<BakerBreadModifier>());
            var breadNeeded = BakerRole.GetEffectiveBreadNeeded(localPlayer);
            var bakerText = TouLocale.Get(
                    "ExtensionRoleBakerMeetingSummary",
                    "Bread Given: {0} / {1}")
                .Replace("{0}", breadCount.ToString(CultureInfo.InvariantCulture))
                .Replace("{1}", breadNeeded.ToString(CultureInfo.InvariantCulture));

            return $"{TouExtensionColors.Baker.ToTextColor()}{bakerText}</color>";
        }

        if (localPlayer.Data.Role is FamineRole famineRole)
        {
            var starvingCount = PlayerControl.AllPlayerControls.ToArray()
                .Count(player => player != null &&
                                 !player.HasDied() &&
                                 player.HasModifier<FamineStarvedModifier>());
            var famineText = famineRole.CanStarveAnyone
                ? TouLocale.Get("ExtensionRoleFamineMeetingSummaryUnlocked", "Famine Evolved: the hunger spreads to all.")
                : TouLocale.Get("ExtensionRoleFamineMeetingSummary", "Marked for Famine: {0}")
                    .Replace("{0}", starvingCount.ToString(CultureInfo.InvariantCulture));

            return $"{TouExtensionColors.Famine.ToTextColor()}{famineText}</color>";
        }

        if (localPlayer.Data.Role is BerserkerRole berserkerRole)
        {
            if (berserkerRole.IsWar)
            {
                var warText = TouLocale.Get("ExtensionRoleWarMeetingSummary", "War Evolved: the battlefield is yours.");
                return $"{TouExtensionColors.War.ToTextColor()}{warText}</color>";
            }

            var needed = (int)OptionGroupSingleton<BerserkerOptions>.Instance.KillsNeededToTransform;
            var kills = Math.Min(berserkerRole.KillCount, needed);
            var berserkerText = TouLocale.Get("ExtensionRoleBerserkerMeetingSummary", "Kills to become War: {0} / {1}")
                .Replace("{0}", kills.ToString(CultureInfo.InvariantCulture))
                .Replace("{1}", needed.ToString(CultureInfo.InvariantCulture));

            return $"{TouExtensionColors.Berserker.ToTextColor()}{berserkerText}</color>";
        }

        if (localPlayer.Data.Role is WarRole)
        {
            var warText = TouLocale.Get("ExtensionRoleWarMeetingSummary", "War Evolved: the battlefield is yours.");
            return $"{TouExtensionColors.War.ToTextColor()}{warText}</color>";
        }

        if (localPlayer.Data.Role is DeathRole)
        {
            var deathText = TouLocale.Get("ExtensionRoleDeathMeetingSummary", "Death Evolved: no soul escapes.");
            return $"{TouExtensionColors.Death.ToTextColor()}{deathText}</color>";
        }

        return string.Empty;
    }
}
