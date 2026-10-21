using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmongUs.GameOptions;
using HarmonyLib;
using MiraAPI.GameOptions.OptionTypes;
using TMPro;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options;
using TownOfUs;
using TownOfUs.Modules.Localization;
using TownOfUs.Options;
using TownOfUs.Patches;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Draft;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class DraftRoleListHudPatch
{
    private const string DraftModeTitleColor = "#FF9999";
    private const string DraftDisabledColor = "#FF2222";
    private const string DraftStatusMarker = "<link=\"TOUMCE_DRAFT_STATUS\">";
    private const string DraftStatusMarkerEnd = "</link>";

    private const string TitleColor = "#FFD700";
    private const string CrewColor = "#8CFFFF";
    private const string ImpColor = "#FF4444";
    private const string NeutralColor = "#B8B8B8";

    private static readonly Dictionary<DraftRoleListOption, string> RoleListSlotTextCache = [];
    private static string? _lastHudText;
    private static float _nextHudTextRefresh;

    [HarmonyPriority(Priority.Last)]
    [HarmonyPostfix]
    public static void HudManagerUpdatePostfix()
    {
        if (!LobbyBehaviour.Instance)
        {
            return;
        }

        if (!HudManagerPatches.RoleList || !HudManagerPatches.RoleListTextComp)
        {
            return;
        }

        if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Joined)
        {
            HudManagerPatches.RoleList.SetActive(false);
            return;
        }

        var text = HudManagerPatches.RoleListTextComp;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.verticalAlignment = VerticalAlignmentOptions.Top;
        text.fontSize = text.fontSizeMin = text.fontSizeMax = GetHudFontSize();

        if (DraftLobbyPatch.DraftInProgress || DraftLobbyPatch.DraftCompletedWaitingForStart)
        {
            HudManagerPatches.RoleList.SetActive(false);
            return;
        }

        if (!DraftSystem.IsEnabled)
        {
            PrefixDraftStatusToMiraText(text);
            HudManagerPatches.RoleList.SetActive(true);
            return;
        }

        var now = Time.time;
        if (_lastHudText == null || now >= _nextHudTextRefresh)
        {
            _lastHudText = BuildDraftContentText();
            _nextHudTextRefresh = now + 0.25f;
        }

        var desiredText = BuildDraftStatusLine(true) + _lastHudText;
        if (text.text != desiredText)
        {
            text.text = desiredText;
        }

        HudManagerPatches.RoleList.SetActive(true);
    }

    private static float GetHudFontSize()
    {
        if (!DraftSystem.IsEnabled)
        {
            return 3f;
        }

        var poolMode = OptionGroupSingleton<DraftModeOptions>.Instance.PoolMode.Value;
        return poolMode == DraftPoolMode.MinMax ? 2.75f : 3f;
    }

    private static void PrefixDraftStatusToMiraText(TMP_Text text)
    {
        var miraText = RemoveDraftStatusPrefix(text.text);
        text.text = BuildDraftStatusLine(false) + miraText;
    }

    private static string RemoveDraftStatusPrefix(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (!text.StartsWith(DraftStatusMarker, StringComparison.Ordinal) &&
            !text.StartsWith($"<color={DraftModeTitleColor}><b>", StringComparison.Ordinal))
        {
            return text;
        }

        var firstLineEnd = text.IndexOf('\n');
        return firstLineEnd < 0 ? string.Empty : text[(firstLineEnd + 1)..];
    }

    private static string BuildDraftContentText()
    {
        var builder = new StringBuilder();
        AppendDraftContent(builder);
        return builder.ToString();
    }

    private static string BuildDraftStatusLine(bool enabled)
    {
        var title = TouLocale.Get("ExtensionDraftModeGroupName", "Draft Mode");
        var status = enabled
            ? TouLocale.Get("ExtensionDraftModeEnabled", "Enabled")
            : TouLocale.Get("ExtensionDraftModeDisabled", "Disabled");
        var builder = new StringBuilder(DraftStatusMarker);
        var separatorIndex = title.LastIndexOf(' ');

        builder.Append("<b>");
        if (separatorIndex > 0 && separatorIndex < title.Length - 1)
        {
            var firstPart = title[..separatorIndex];
            var secondPart = title[(separatorIndex + 1)..];
            AppendShimmerText(builder, firstPart, new Color(1f, 0.6f, 0.6f), Color.white, Time.time, 0);
            builder.Append(' ');
            AppendShimmerText(builder, secondPart, new Color(1f, 0.6f, 0.6f), Color.white, Time.time, firstPart.Length + 1);
        }
        else
        {
            AppendShimmerText(builder, title, new Color(1f, 0.6f, 0.6f), Color.white, Time.time, 0);
        }

        builder.Append(":</b> <b>");
        if (enabled)
        {
            AppendShimmerText(builder, status, new Color(0f, 1f, 0f), Color.white, Time.time, title.Length + 2);
        }
        else
        {
            AppendShimmerText(builder, status, new Color(1f, 0.13f, 0.13f), Color.white, Time.time, title.Length + 2);
        }
        builder.Append("</b>").Append(DraftStatusMarkerEnd).Append('\n');
        return builder.ToString();
    }

    private static void AppendShimmerText(
        StringBuilder builder,
        string text,
        Color baseColor,
        Color targetColor,
        float time,
        int startIndex)
    {
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            var shimmer = (Mathf.Sin(time * 2.2f - (startIndex + index) * 0.6f) + 1f) * 0.5f;
            shimmer *= shimmer;
            var color = Color.Lerp(baseColor, targetColor, shimmer);
            builder.Append("<color=#")
                .Append(ColorUtility.ToHtmlStringRGB(color))
                .Append('>')
                .Append(character)
                .Append("</color>");
        }
    }

    private static void AppendDraftContent(StringBuilder builder)
    {
        var options = OptionGroupSingleton<DraftModeOptions>.Instance;
        var poolMode = options.PoolMode.Value;

        if (poolMode == DraftPoolMode.RoleList)
        {
            var slots = GetRoleListSlots(OptionGroupSingleton<DraftRoleListSettingsOptions>.Instance);
            var slotCount = GetVisibleRoleListSlotCount(slots);

            AppendTitle(builder, $"{TouLocale.Get("ExtensionRoleListTitle", "Set Role List")} ({slotCount}/{GetLobbyCapacity()})");
            AppendRoleListPreview(builder, slots, slotCount);
            return;
        }

        if (poolMode == DraftPoolMode.MinMax)
        {
            AppendTitle(builder, TouLocale.Get("ExtensionFactionListTitle", "Faction List"));
            AppendMinMaxPreview(builder);
            return;
        }

        AppendTitle(builder, TouLocale.Get("ExtensionOldDraftSettingsTitle", "Draft Mode Settings (Old)"));
        AppendOldDraftSettingsPreview(builder);
    }

    private static void AppendTitle(StringBuilder builder, string title)
    {
        builder.Append("<color=");
        builder.Append(TitleColor);
        builder.Append("><b>");
        builder.Append(title);
        builder.AppendLine(":</b></color>");
    }

    private static void AppendOldDraftSettingsPreview(StringBuilder builder)
    {
        var options = OptionGroupSingleton<DraftOldSettingsOptions>.Instance;
        var neutralColor = ColorUtility.ToHtmlStringRGB(TownOfUsColors.Neutral);

        builder.Append("<color=#");
        builder.Append(neutralColor);
        builder.Append('>').Append(TouLocale.Get("Neutral", "Neutral")).Append("</color> ")
            .Append(TouLocale.Get("ExtensionDraftModeOther", "Other")).Append(' ');
        builder.Append("<color=#FFD700>").Append(TouLocale.Get("ExtensionDraftModeMin", "Min")).Append("</color> ");
        builder.Append(Mathf.RoundToInt(options.MinOtherNeutrals.Value));
        builder.Append(" <color=#FFD700>").Append(TouLocale.Get("ExtensionDraftModeMax", "Max")).Append("</color> ");
        builder.Append(Mathf.RoundToInt(options.MaxOtherNeutrals.Value));
        builder.AppendLine();
        builder.Append("<color=#");
        builder.Append(neutralColor);
        builder.Append('>').Append(TouLocale.Get("Neutral", "Neutral")).Append("</color> ")
            .Append(TouLocale.Get("ExtensionDraftModeKilling", "Killing")).Append(' ');
        builder.Append("<color=#FFD700>").Append(TouLocale.Get("ExtensionDraftModeMin", "Min")).Append("</color> ");
        builder.Append(Mathf.RoundToInt(options.MinNeutralKilling.Value));
        builder.Append(" <color=#FFD700>").Append(TouLocale.Get("ExtensionDraftModeMax", "Max")).Append("</color> ");
        builder.Append(Mathf.RoundToInt(options.MaxNeutralKilling.Value));
        builder.AppendLine();
    }

    private static void AppendMinMaxPreview(StringBuilder builder)
    {
        var crewOptions = OptionGroupSingleton<DraftCrewmateSettingsOptions>.Instance;
        var impOptions = OptionGroupSingleton<DraftImpostorSettingsOptions>.Instance;
        var neutralOptions = OptionGroupSingleton<DraftNeutralSettingsOptions>.Instance;

        var crewTotal = Round(GetTotal(
            crewOptions.MaxCrewInvestigative,
            crewOptions.MaxCrewKilling,
            crewOptions.MaxCrewPower,
            crewOptions.MaxCrewProtective,
            crewOptions.MaxCrewSupport));

        var impTotal = Round(impOptions.MaxImpostorsTotal.Value);
        var neutralTotal = Round(neutralOptions.MaxNeutralTotal.Value);

        AppendLocalizedCount(builder, CrewColor, "ExtensionDraftHudCrewmates", "Crewmates", crewTotal);
        AppendLocalizedCount(builder, CrewColor, "ExtensionDraftHudCrewInvestigative", "Crewmate Investigative", Round(crewOptions.MaxCrewInvestigative.Value));
        AppendLocalizedCount(builder, CrewColor, "ExtensionDraftHudCrewKilling", "Crewmate Killing", Round(crewOptions.MaxCrewKilling.Value));
        AppendLocalizedCount(builder, CrewColor, "ExtensionDraftHudCrewPower", "Crewmate Power", Round(crewOptions.MaxCrewPower.Value));
        AppendLocalizedCount(builder, CrewColor, "ExtensionDraftHudCrewProtective", "Crewmate Protective", Round(crewOptions.MaxCrewProtective.Value));
        AppendLocalizedCount(builder, CrewColor, "ExtensionDraftHudCrewSupport", "Crewmate Support", Round(crewOptions.MaxCrewSupport.Value));

        AppendLocalizedCount(builder, ImpColor, "ExtensionDraftHudImpostors", "Impostors", impTotal);
        AppendLocalizedCount(builder, ImpColor, "ExtensionDraftHudImpConcealing", "Impostor Concealing", Round(impOptions.MaxImpConcealing.Value));
        AppendLocalizedCount(builder, ImpColor, "ExtensionDraftHudImpKilling", "Impostor Killing", Round(impOptions.MaxImpKilling.Value));
        AppendLocalizedCount(builder, ImpColor, "ExtensionDraftHudImpPower", "Impostor Power", Round(impOptions.MaxImpPower.Value));
        AppendLocalizedCount(builder, ImpColor, "ExtensionDraftHudImpSupport", "Impostor Support", Round(impOptions.MaxImpSupport.Value));

        AppendLocalizedCount(builder, NeutralColor, "ExtensionDraftHudNeutrals", "Neutrals", neutralTotal);
        AppendLocalizedCount(builder, NeutralColor, "ExtensionDraftHudNeutralBenign", "Neutral Benign", Round(neutralOptions.MaxNeutralBenign.Value));
        AppendLocalizedCount(builder, NeutralColor, "ExtensionDraftHudNeutralEvil", "Neutral Evil", Round(neutralOptions.MaxNeutralEvil.Value));
        AppendLocalizedCount(builder, NeutralColor, "ExtensionDraftHudNeutralKilling", "Neutral Killing", Round(neutralOptions.MaxNeutralKillingRoles.Value));
        AppendLocalizedCount(builder, NeutralColor, "ExtensionDraftHudNeutralOutlier", "Neutral Outlier", Round(neutralOptions.MaxNeutralOutlier.Value));
    }

    private static void AppendLocalizedCount(StringBuilder builder, string color, string key, string fallback, int count)
    {
        AppendColoredLine(builder, color, $"{TouLocale.Get(key, fallback)}: {count}");
    }

    private static void AppendColoredLine(StringBuilder builder, string color, string line)
    {
        builder.Append("<color=");
        builder.Append(color);
        builder.Append('>');
        builder.Append(line);
        builder.AppendLine("</color>");
    }

    private static void AppendRoleListPreview(StringBuilder builder, DraftRoleListOption[] slots, int slotCount)
    {
        for (var i = 0; i < slotCount; i++)
        {
            var slot = DraftSystem.RoleListSlotOrder.Count > i
                ? DraftSystem.GetRoleListBucketForPickIndex(i)
                : slots[i];

            builder.Append(GetRoleForDraftSlot(slot));
            builder.AppendLine();
        }
    }

    private static string GetRoleForDraftSlot(DraftRoleListOption slot)
    {
        if (slot == DraftRoleListOption.CrewNeu)
        {
            return $"<color=#8CFFFF>{TouLocale.Get("ExtensionDraftHudCrewmate", "Crewmate")}</color> + " +
                   $"<color=#B8B8B8>{TouLocale.Get("Neutral", "Neutral")}</color>";
        }

        if (RoleListSlotTextCache.TryGetValue(slot, out var cached))
        {
            return cached;
        }

        var text = HudManagerPatches.GetRoleForSlot(ToTownOfUsRoleListOption(slot));
        RoleListSlotTextCache[slot] = text;
        return text;
    }

    private static RoleListOption ToTownOfUsRoleListOption(DraftRoleListOption slot)
    {
        return slot switch
        {
            DraftRoleListOption.CrewInvest => RoleListOption.CrewInvest,
            DraftRoleListOption.CrewKilling => RoleListOption.CrewKilling,
            DraftRoleListOption.CrewProtective => RoleListOption.CrewProtective,
            DraftRoleListOption.CrewPower => RoleListOption.CrewPower,
            DraftRoleListOption.CrewSupport => RoleListOption.CrewSupport,
            DraftRoleListOption.CrewCommon => RoleListOption.CrewCommon,
            DraftRoleListOption.CrewSpecial => RoleListOption.CrewSpecial,
            DraftRoleListOption.CrewRandom => RoleListOption.CrewRandom,

            DraftRoleListOption.NeutBenign => RoleListOption.NeutBenign,
            DraftRoleListOption.NeutEvil => RoleListOption.NeutEvil,
            DraftRoleListOption.NeutKilling => RoleListOption.NeutKilling,
            DraftRoleListOption.NeutOutlier => RoleListOption.NeutOutlier,
            DraftRoleListOption.NeutCommon => RoleListOption.NeutCommon,
            DraftRoleListOption.NeutSpecial => RoleListOption.NeutSpecial,
            DraftRoleListOption.NeutWildcard => RoleListOption.NeutWildcard,
            DraftRoleListOption.NeutRandom => RoleListOption.NeutRandom,

            DraftRoleListOption.ImpConceal => RoleListOption.ImpConceal,
            DraftRoleListOption.ImpKilling => RoleListOption.ImpKilling,
            DraftRoleListOption.ImpPower => RoleListOption.ImpPower,
            DraftRoleListOption.ImpSupport => RoleListOption.ImpSupport,
            DraftRoleListOption.ImpCommon => RoleListOption.ImpCommon,
            DraftRoleListOption.ImpSpecial => RoleListOption.ImpSpecial,
            DraftRoleListOption.ImpRandom => RoleListOption.ImpRandom,

            DraftRoleListOption.NonImp => RoleListOption.NonImp,
            DraftRoleListOption.Any => RoleListOption.Any,
            _ => RoleListOption.NonImp
        };
    }

    private static int GetVisibleRoleListSlotCount(DraftRoleListOption[] slots)
    {
        return Math.Clamp(
            DraftSystem.GetVisibleRoleListSlotCount(),
            1,
            slots.Length);
    }

    private static int GetLobbyCapacity()
    {
        try
        {
            return GameOptionsManager.Instance.CurrentGameOptions.GetInt(Int32OptionNames.MaxPlayers);
        }
        catch
        {
            return 15;
        }
    }

    private static int Round(float value)
    {
        return Mathf.RoundToInt(value);
    }

    private static float GetTotal(params ModdedNumberOption[] options)
    {
        return options.Sum(option => option.Value);
    }

    private static DraftRoleListOption[] GetRoleListSlots(DraftRoleListSettingsOptions options)
    {
        return
        [
            options.Slot1.Value, options.Slot2.Value, options.Slot3.Value, options.Slot4.Value, options.Slot5.Value,
            options.Slot6.Value, options.Slot7.Value, options.Slot8.Value, options.Slot9.Value, options.Slot10.Value,
            options.Slot11.Value, options.Slot12.Value, options.Slot13.Value, options.Slot14.Value, options.Slot15.Value,
            options.Slot16.Value, options.Slot17.Value, options.Slot18.Value, options.Slot19.Value, options.Slot20.Value,
            options.Slot21.Value, options.Slot22.Value, options.Slot23.Value, options.Slot24.Value, options.Slot25.Value,
            options.Slot26.Value, options.Slot27.Value, options.Slot28.Value, options.Slot29.Value, options.Slot30.Value,
            options.Slot31.Value, options.Slot32.Value, options.Slot33.Value, options.Slot34.Value, options.Slot35.Value
        ];
    }
}
