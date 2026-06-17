using HarmonyLib;
using System;
using System.Linq;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.UpdateTimerText))]
public static class ApocalypseMeetingSummaryPatch
{
    public static void Postfix(MeetingHud __instance)
    {
        var timerText = __instance?.TimerText;
        if (timerText == null || string.IsNullOrEmpty(timerText.text))
        {
            return;
        }

        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.HasDied() || localPlayer.Data?.Role == null)
        {
            return;
        }

        var summary = GetSummaryLine(localPlayer);
        if (string.IsNullOrEmpty(summary))
        {
            return;
        }

        var lines = timerText.text.Split('\n')
            .Where(line => !line.Contains("data-toumce-apocalypse-summary"))
            .ToList();
        lines.Add($"<size=75%><alpha=#00>data-toumce-apocalypse-summary</alpha>{summary}</size>");
        timerText.text = string.Join("\n", lines);
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
                    "Souls: {0} / {1} | Reaped: {2} / {3}")
                .Replace("{0}", soulCollectorRole.SoulsCollected.ToString())
                .Replace("{1}", soulsNeeded.ToString())
                .Replace("{2}", activeMarks.ToString())
                .Replace("{3}", maxMarks.ToString());

            return $"{TouExtensionColors.SoulCollector.ToTextColor()}{soulText}</color>";
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
                    "Breaded: {0} / {1}")
                .Replace("{0}", breadCount.ToString())
                .Replace("{1}", breadNeeded.ToString());

            return $"{TouExtensionColors.Baker.ToTextColor()}{bakerText}</color>";
        }

        if (localPlayer.Data.Role is FamineRole famineRole)
        {
            var starvingCount = PlayerControl.AllPlayerControls.ToArray()
                .Count(player => player != null &&
                                 !player.HasDied() &&
                                 player.HasModifier<FamineStarvedModifier>());
            var famineText = famineRole.CanStarveAnyone
                ? TouLocale.Get("ExtensionRoleFamineMeetingSummaryUnlocked", "Famine is unleashed. Starve anyone.")
                : TouLocale.Get("ExtensionRoleFamineMeetingSummary", "Starving: {0}")
                    .Replace("{0}", starvingCount.ToString());

            return $"{TouExtensionColors.Famine.ToTextColor()}{famineText}</color>";
        }

        if (localPlayer.Data.Role is BerserkerRole berserkerRole)
        {
            if (berserkerRole.IsWar)
            {
                var warText = TouLocale.Get("ExtensionRoleWarMeetingSummary", "War active");
                return $"{TouExtensionColors.War.ToTextColor()}{warText}</color>";
            }

            var needed = (int)OptionGroupSingleton<BerserkerOptions>.Instance.KillsNeededToTransform;
            var kills = Math.Min(berserkerRole.KillCount, needed);
            var berserkerText = TouLocale.Get("ExtensionRoleBerserkerMeetingSummary", "Kills: {0} / {1}")
                .Replace("{0}", kills.ToString())
                .Replace("{1}", needed.ToString());

            return $"{TouExtensionColors.Berserker.ToTextColor()}{berserkerText}</color>";
        }

        if (localPlayer.Data.Role is WarRole)
        {
            var warText = TouLocale.Get("ExtensionRoleWarMeetingSummary", "War active");
            return $"{TouExtensionColors.War.ToTextColor()}{warText}</color>";
        }

        return string.Empty;
    }
}
