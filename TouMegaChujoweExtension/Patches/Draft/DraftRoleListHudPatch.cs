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
    private const string DraftEnabledColor = "#00FF00";
    private const string DraftDisabledColor = "#FF2222";

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
            _lastHudText = BuildDraftHudText();
            _nextHudTextRefresh = now + 0.25f;
        }

        if (text.text != _lastHudText)
        {
            text.text = _lastHudText;
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

        if (!text.Contains("<b>Draft Mode:</b>"))
        {
            return text;
        }

        var firstLineEnd = text.IndexOf('\n');
        return firstLineEnd < 0 ? string.Empty : text[(firstLineEnd + 1)..];
    }

    private static string BuildDraftHudText()
    {
        var builder = new StringBuilder();

        builder.Append(BuildDraftStatusLine(true));
        AppendDraftContent(builder);

        return builder.ToString();
    }

    private static string BuildDraftStatusLine(bool enabled)
    {
        return enabled
            ? $"<color={DraftModeTitleColor}><b>Draft Mode:</b></color> <color={DraftEnabledColor}><b>Enabled</b></color>\n"
            : $"<color={DraftModeTitleColor}><b>Draft Mode:</b></color> <color={DraftDisabledColor}><b>Disabled</b></color>\n";
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
        builder.Append(">Neutral</color> Other ");
        builder.Append("<color=#FFD700>Min</color> ");
        builder.Append(Mathf.RoundToInt(options.MinOtherNeutrals.Value));
        builder.Append(" <color=#FFD700>Max</color> ");
        builder.Append(Mathf.RoundToInt(options.MaxOtherNeutrals.Value));
        builder.AppendLine();
        builder.Append("<color=#");
        builder.Append(neutralColor);
        builder.Append(">Neutral</color> Killing ");
        builder.Append("<color=#FFD700>Min</color> ");
        builder.Append(Mathf.RoundToInt(options.MinNeutralKilling.Value));
        builder.Append(" <color=#FFD700>Max</color> ");
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

        AppendColoredLine(builder, CrewColor, $"Crewmates: {crewTotal}");
        AppendColoredLine(builder, CrewColor, $"Crewmate Investigative: {Round(crewOptions.MaxCrewInvestigative.Value)}");
        AppendColoredLine(builder, CrewColor, $"Crewmate Killing: {Round(crewOptions.MaxCrewKilling.Value)}");
        AppendColoredLine(builder, CrewColor, $"Crewmate Power: {Round(crewOptions.MaxCrewPower.Value)}");
        AppendColoredLine(builder, CrewColor, $"Crewmate Protective: {Round(crewOptions.MaxCrewProtective.Value)}");
        AppendColoredLine(builder, CrewColor, $"Crewmate Support: {Round(crewOptions.MaxCrewSupport.Value)}");

        AppendColoredLine(builder, ImpColor, $"Impostors: {impTotal}");
        AppendColoredLine(builder, ImpColor, $"Impostor Concealing: {Round(impOptions.MaxImpConcealing.Value)}");
        AppendColoredLine(builder, ImpColor, $"Impostor Killing: {Round(impOptions.MaxImpKilling.Value)}");
        AppendColoredLine(builder, ImpColor, $"Impostor Power: {Round(impOptions.MaxImpPower.Value)}");
        AppendColoredLine(builder, ImpColor, $"Impostor Support: {Round(impOptions.MaxImpSupport.Value)}");

        AppendColoredLine(builder, NeutralColor, $"Neutrals: {neutralTotal}");
        AppendColoredLine(builder, NeutralColor, $"Neutral Benign: {Round(neutralOptions.MaxNeutralBenign.Value)}");
        AppendColoredLine(builder, NeutralColor, $"Neutral Evil: {Round(neutralOptions.MaxNeutralEvil.Value)}");
        AppendColoredLine(builder, NeutralColor, $"Neutral Killing: {Round(neutralOptions.MaxNeutralKillingRoles.Value)}");
        AppendColoredLine(builder, NeutralColor, $"Neutral Outlier: {Round(neutralOptions.MaxNeutralOutlier.Value)}");
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
            return "<color=#8CFFFF>Crewmate</color> + <color=#B8B8B8>Neutral</color>";
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
