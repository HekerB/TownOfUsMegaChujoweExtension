using System.Text;
using HarmonyLib;
using MiraAPI.GameOptions.OptionTypes;
using TMPro;
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
    [HarmonyPriority(Priority.Last)]
    [HarmonyPostfix]
    public static void HudManagerUpdatePostfix()
    {
        if (!DraftSystem.IsEnabled || !LobbyBehaviour.Instance)
        {
            return;
        }

        if (!HudManagerPatches.RoleList || !HudManagerPatches.RoleListTextComp)
        {
            return;
        }

        var text = HudManagerPatches.RoleListTextComp;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.verticalAlignment = VerticalAlignmentOptions.Top;
        text.fontSize = text.fontSizeMin = text.fontSizeMax = 3f;
        text.text = BuildDraftRoleListText();

        HudManagerPatches.RoleList.SetActive(true);
    }

    private static string BuildDraftRoleListText()
    {
        var builder = new StringBuilder();
        var options = OptionGroupSingleton<DraftModeOptions>.Instance;

        builder.Append("<color=#FFD700>");
        builder.Append(options.PoolMode.Value == DraftPoolMode.OldDraft
            ? TouLocale.Get("ExtensionOldDraftRoleListTitle", "Old Draft Mode")
            : TouLocale.Get("ExtensionDraftRoleListTitle", "Draft Mode"));
        builder.Append(":</color>\n");

        if (options.PoolMode.Value == DraftPoolMode.MinMax)
        {
            AppendMinMaxPreview(builder, options);
            return builder.ToString();
        }

        if (options.PoolMode.Value == DraftPoolMode.RoleList)
        {
            AppendRoleListPreview(builder, options);
            return builder.ToString();
        }

        AppendOldDraftSettingsPreview(builder);
        return builder.ToString();
    }

    private static void AppendOldDraftSettingsPreview(StringBuilder builder)
    {
        var options = OptionGroupSingleton<DraftOldSettingsOptions>.Instance;
        var neutralColor = ColorUtility.ToHtmlStringRGB(TownOfUsColors.Neutral);
        builder.Append("<color=#");
        builder.Append(neutralColor);
        builder.Append(">Neutral</color> Other <color=#FFD700>Min</color> ");
        builder.Append(Mathf.RoundToInt(options.MinOtherNeutrals.Value));
        builder.Append(" <color=#FFD700>Max</color> ");
        builder.Append(Mathf.RoundToInt(options.MaxOtherNeutrals.Value));
        builder.AppendLine();

        builder.Append("<color=#");
        builder.Append(neutralColor);
        builder.Append(">Neutral</color> Killing <color=#FFD700>Min</color> ");
        builder.Append(Mathf.RoundToInt(options.MinNeutralKilling.Value));
        builder.Append(" <color=#FFD700>Max</color> ");
        builder.Append(Mathf.RoundToInt(options.MaxNeutralKilling.Value));
        builder.AppendLine();
    }

    private static void AppendMinMaxPreview(StringBuilder builder, DraftModeOptions _)
    {
        var crewOptions = OptionGroupSingleton<DraftCrewmateSettingsOptions>.Instance;
        var impOptions = OptionGroupSingleton<DraftImpostorSettingsOptions>.Instance;
        var neutralOptions = OptionGroupSingleton<DraftNeutralSettingsOptions>.Instance;

        AppendPreviewHeader(builder, "#8CFFFF", "CREWMATE SETTINGS");
        AppendPreviewLine(builder, "#8CFFFF", "Total Crew", GetTotal(
            crewOptions.MaxCrewInvestigative, crewOptions.MaxCrewKilling, crewOptions.MaxCrewPower,
            crewOptions.MaxCrewProtective, crewOptions.MaxCrewSupport));
        AppendPreviewLine(builder, "#8CFFFF", "Investigative", crewOptions.MaxCrewInvestigative.Value);
        AppendPreviewLine(builder, "#8CFFFF", "Crew Killing", crewOptions.MaxCrewKilling.Value);
        AppendPreviewLine(builder, "#8CFFFF", "Crew Power", crewOptions.MaxCrewPower.Value);
        AppendPreviewLine(builder, "#8CFFFF", "Protective", crewOptions.MaxCrewProtective.Value);
        AppendPreviewLine(builder, "#8CFFFF", "Crew Support", crewOptions.MaxCrewSupport.Value);

        builder.AppendLine();
        AppendPreviewHeader(builder, "#FF5555", "IMPOSTOR SETTINGS");
        AppendPreviewLine(builder, "#FF5555", "Total Impostors", impOptions.MaxImpostorsTotal.Value);
        AppendPreviewLine(builder, "#FF1919", "Imp Concealing", impOptions.MaxImpConcealing.Value);
        AppendPreviewLine(builder, "#FF1919", "Imp Killing", impOptions.MaxImpKilling.Value);
        AppendPreviewLine(builder, "#FF1919", "Imp Power", impOptions.MaxImpPower.Value);
        AppendPreviewLine(builder, "#FF1919", "Imp Support", impOptions.MaxImpSupport.Value);

        builder.AppendLine();
        AppendPreviewHeader(builder, "#A0A0A0", "NEUTRAL SETTINGS");
        AppendPreviewLine(builder, "#A0A0A0", "Total Neutral", neutralOptions.MaxNeutralTotal.Value);
        AppendPreviewLine(builder, "#A0A0A0", "Benign", neutralOptions.MaxNeutralBenign.Value);
        AppendPreviewLine(builder, "#A0A0A0", "Evil", neutralOptions.MaxNeutralEvil.Value);
        AppendPreviewLine(builder, "#A0A0A0", "Neutral Killing", neutralOptions.MaxNeutralKillingRoles.Value);
        AppendPreviewLine(builder, "#A0A0A0", "Outlier", neutralOptions.MaxNeutralOutlier.Value);
    }

    private static float GetTotal(params ModdedNumberOption[] options)
    {
        return options.Sum(option => option.Value);
    }

    private static void AppendPreviewHeader(StringBuilder builder, string color, string label)
    {
        builder.Append("<color=");
        builder.Append(color);
        builder.Append("><b>");
        builder.Append(label);
        builder.AppendLine(":</b></color>");
    }

    private static void AppendPreviewLine(StringBuilder builder, string color, string label, float value)
    {
        builder.Append("<color=");
        builder.Append(color);
        builder.Append('>');
        builder.Append(label);
        builder.Append(": ");
        builder.Append(Mathf.RoundToInt(value));
        builder.AppendLine("</color>");
    }

    private static void AppendRoleListPreview(StringBuilder builder, DraftModeOptions _)
    {
        var slots = GetRoleListSlots(OptionGroupSingleton<DraftRoleListSettingsOptions>.Instance);
        var slotCount = Math.Clamp(DraftSystem.GetVisibleRoleListSlotCount(), 1, Math.Min(DraftSystem.MaxRoleListSlots, slots.Length));

        for (var i = 0; i < slotCount; i++)
        {
            var slot = DraftSystem.RoleListSlotOrder.Count > i
                ? DraftSystem.GetRoleListBucketForPickIndex(i)
                : slots[i];
            builder.Append(HudManagerPatches.GetRoleForSlot(slot));
            builder.AppendLine();
        }
    }

    private static RoleListOption[] GetRoleListSlots(DraftRoleListSettingsOptions options)
    {
        return
        [
            options.Slot1.Value, options.Slot2.Value, options.Slot3.Value, options.Slot4.Value,
            options.Slot5.Value, options.Slot6.Value, options.Slot7.Value, options.Slot8.Value,
            options.Slot9.Value, options.Slot10.Value, options.Slot11.Value, options.Slot12.Value,
            options.Slot13.Value, options.Slot14.Value, options.Slot15.Value, options.Slot16.Value,
            options.Slot17.Value, options.Slot18.Value, options.Slot19.Value, options.Slot20.Value
        ];
    }
}
