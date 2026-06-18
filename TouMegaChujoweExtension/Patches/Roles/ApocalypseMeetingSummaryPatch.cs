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

namespace TouMegaChujoweExtension.Patches.Roles;

[HarmonyPatch(typeof(MeetingHud))]
public static class ApocalypseMeetingSummaryPatch
{
    private static readonly Color SoulCollectorSummaryColor = new Color32(119, 122, 168, 255);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(MeetingHud.UpdateTimerText))]
    public static void TimerUpdatePostfix(MeetingHud __instance)
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
            if (!string.IsNullOrEmpty(summary))
            {
                timerText.text += $"\n{summary}";
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[TOUMCE] Apocalypse meeting summary failed: {ex}");
        }
    }

    private static string GetSummaryLine(PlayerControl localPlayer)
    {
        var soulCollectorRole = localPlayer.GetRole<SoulCollectorRole>();
        var soulOptions = OptionGroupSingleton<SoulCollectorOptions>.Instance;
        if (soulCollectorRole != null && soulOptions != null)
        {
            var soulsNeeded = SoulCollectorRole.GetEffectiveSoulGoal(localPlayer);
            var activeMarks = SoulCollectorRole.GetActiveMarkCount(localPlayer.PlayerId);
            var maxMarks = (int)soulOptions.MaxMarks;
            var soulText = TouLocale.Get(
                    "ExtensionRoleSoulCollectorMeetingSummary",
                    "Souls Claimed: {0} / {1} | Reap Marks: {2} / {3}")
                .Replace("{0}", soulCollectorRole.SoulsCollected.ToString(CultureInfo.InvariantCulture))
                .Replace("{1}", soulsNeeded.ToString(CultureInfo.InvariantCulture))
                .Replace("{2}", activeMarks.ToString(CultureInfo.InvariantCulture))
                .Replace("{3}", maxMarks.ToString(CultureInfo.InvariantCulture));

            return $"{SoulCollectorSummaryColor.ToTextColor()}{soulText}</color>";
        }

        var bakerRole = localPlayer.GetRole<BakerRole>();
        if (bakerRole != null)
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

        var famineRole = localPlayer.GetRole<FamineRole>();
        if (famineRole != null)
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

        var berserkerRole = localPlayer.GetRole<BerserkerRole>();
        var berserkerOptions = OptionGroupSingleton<BerserkerOptions>.Instance;
        if (berserkerRole != null && berserkerOptions != null)
        {
            if (berserkerRole.IsWar)
            {
                var warText = TouLocale.Get("ExtensionRoleWarMeetingSummary", "War Evolved: the battlefield is yours.");
                return $"{TouExtensionColors.War.ToTextColor()}{warText}</color>";
            }

            var needed = (int)berserkerOptions.KillsNeededToTransform;
            var kills = Math.Min(berserkerRole.KillCount, needed);
            var berserkerText = TouLocale.Get("ExtensionRoleBerserkerMeetingSummary", "Kills to become War: {0} / {1}")
                .Replace("{0}", kills.ToString(CultureInfo.InvariantCulture))
                .Replace("{1}", needed.ToString(CultureInfo.InvariantCulture));

            return $"{TouExtensionColors.Berserker.ToTextColor()}{berserkerText}</color>";
        }

        var warRole = localPlayer.GetRole<WarRole>();
        if (warRole != null)
        {
            var warText = TouLocale.Get("ExtensionRoleWarMeetingSummary", "War Evolved: the battlefield is yours.");
            return $"{TouExtensionColors.War.ToTextColor()}{warText}</color>";
        }

        var deathRole = localPlayer.GetRole<DeathRole>();
        if (deathRole != null)
        {
            var deathText = TouLocale.Get("ExtensionRoleDeathMeetingSummary", "Death Evolved: no soul escapes.");
            return $"{TouExtensionColors.Death.ToTextColor()}{deathText}</color>";
        }

        return string.Empty;
    }
}
