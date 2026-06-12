using System;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using MiraAPI.GameOptions;
using TownOfUs.Options;
using TownOfUs.Patches;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options;

namespace TouMegaChujoweExtension.Patches.Draft;

[HarmonyPatch(typeof(RoleListHoverComponent), nameof(RoleListHoverComponent.Update))]
public static class DraftRoleListHoverPatch
{
    private static readonly FieldInfo TooltipInfoRolesField =
        typeof(BucketTooltipData.TooltipInfo).GetField("Roles", BindingFlags.NonPublic | BindingFlags.Instance);

    private static BucketTooltipData.RoleEntry[] GetRoles(BucketTooltipData.TooltipInfo info)
    {
        if (TooltipInfoRolesField == null) return [];
        return (BucketTooltipData.RoleEntry[])TooltipInfoRolesField.GetValue(info);
    }

    private static readonly AccessTools.FieldRef<RoleListHoverComponent, int> LastLineRef =
        AccessTools.FieldRefAccess<RoleListHoverComponent, int>("_lastLine");

    private static readonly AccessTools.FieldRef<RoleListHoverComponent, string> OriginalTextRef =
        AccessTools.FieldRefAccess<RoleListHoverComponent, string>("_originalText");

    private static readonly AccessTools.FieldRef<RoleListHoverComponent, GameObject> TooltipGoRef =
        AccessTools.FieldRefAccess<RoleListHoverComponent, GameObject>("_tooltipGo");

    private static readonly AccessTools.FieldRef<RoleListHoverComponent, TextMeshPro> TooltipTmpRef =
        AccessTools.FieldRefAccess<RoleListHoverComponent, TextMeshPro>("_tooltipTmp");

    private static readonly AccessTools.FieldRef<RoleListHoverComponent, AspectPosition> TooltipApRef =
        AccessTools.FieldRefAccess<RoleListHoverComponent, AspectPosition>("_tooltipAp");

    private static readonly AccessTools.FieldRef<RoleListHoverComponent, string> TooltipBaseTextRef =
        AccessTools.FieldRefAccess<RoleListHoverComponent, string>("_tooltipBaseText");

    private static readonly AccessTools.FieldRef<RoleListHoverComponent, float> HideDelayRef =
        AccessTools.FieldRefAccess<RoleListHoverComponent, float>("_hideDelay");

    private static readonly MethodInfo HideTooltipMethod =
        AccessTools.Method(typeof(RoleListHoverComponent), "HideTooltip");

    private static readonly MethodInfo EnsureTooltipMethod =
        AccessTools.Method(typeof(RoleListHoverComponent), "EnsureTooltip");

    private static readonly MethodInfo UpdateTooltipLinksMethod =
        AccessTools.Method(typeof(RoleListHoverComponent), "UpdateTooltipLinks");

    private static readonly MethodInfo GetLineUnderMouseMethod =
        AccessTools.Method(typeof(RoleListHoverComponent), "GetLineUnderMouse");

    private static readonly MethodInfo RestoreRoleListTextMethod =
        AccessTools.Method(typeof(RoleListHoverComponent), "RestoreRoleListText");

    private static void HideTooltip(RoleListHoverComponent instance) => HideTooltipMethod.Invoke(instance, null);
    private static void EnsureTooltip(RoleListHoverComponent instance) => EnsureTooltipMethod.Invoke(instance, null);
    private static void UpdateTooltipLinks(RoleListHoverComponent instance) => UpdateTooltipLinksMethod.Invoke(instance, null);
    private static int GetLineUnderMouse(RoleListHoverComponent instance)
    {
        return (int)GetLineUnderMouseMethod.Invoke(instance, null);
    }

    private static void RestoreRoleListText(RoleListHoverComponent instance) => RestoreRoleListTextMethod.Invoke(instance, null);

    [HarmonyPrefix]
    public static bool UpdatePrefix(RoleListHoverComponent __instance)
    {
        if (!DraftSystem.IsEnabled)
        {
            return true;
        }

        if (__instance.TextTarget == null || !__instance.TextTarget.gameObject.activeSelf)
        {
            HideTooltip(__instance);
            return false;
        }

        EnsureTooltip(__instance);

        var tooltipGo = TooltipGoRef(__instance);
        if (tooltipGo != null && tooltipGo.activeSelf)
        {
            UpdateTooltipLinks(__instance);
        }

        var line = GetLineUnderMouse(__instance);

        if (line == LastLineRef(__instance))
        {
            HideDelayRef(__instance) = 0f;
            return false;
        }

        if (line < 0)
        {
            HideDelayRef(__instance) += Time.deltaTime;
            if (HideDelayRef(__instance) < 0.3f) return false;
            HideDelayRef(__instance) = 0f;
        }
        else
        {
            HideDelayRef(__instance) = 0f;
        }

        LastLineRef(__instance) = line;
        RestoreRoleListText(__instance);

        if (line < 0)
        {
            HideTooltip(__instance);
            return false;
        }

        var options = OptionGroupSingleton<DraftModeOptions>.Instance;
        if (options == null)
        {
            HideTooltip(__instance);
            return false;
        }

        var poolMode = options.PoolMode.Value;
        RoleListOption bucket = (RoleListOption)(-1);

        if (poolMode == DraftPoolMode.RoleList)
        {
            var slotIndex = line - 2;
            if (slotIndex < 0)
            {
                HideTooltip(__instance);
                return false;
            }

            var draftOptions = OptionGroupSingleton<DraftRoleListSettingsOptions>.Instance;
            if (draftOptions == null)
            {
                HideTooltip(__instance);
                return false;
            }

            var draftSlots = new DraftRoleListOption[]
            {
                draftOptions.Slot1.Value, draftOptions.Slot2.Value, draftOptions.Slot3.Value, draftOptions.Slot4.Value, draftOptions.Slot5.Value,
                draftOptions.Slot6.Value, draftOptions.Slot7.Value, draftOptions.Slot8.Value, draftOptions.Slot9.Value, draftOptions.Slot10.Value,
                draftOptions.Slot11.Value, draftOptions.Slot12.Value, draftOptions.Slot13.Value, draftOptions.Slot14.Value, draftOptions.Slot15.Value,
                draftOptions.Slot16.Value, draftOptions.Slot17.Value, draftOptions.Slot18.Value, draftOptions.Slot19.Value, draftOptions.Slot20.Value,
                draftOptions.Slot21.Value, draftOptions.Slot22.Value, draftOptions.Slot23.Value, draftOptions.Slot24.Value, draftOptions.Slot25.Value,
                draftOptions.Slot26.Value, draftOptions.Slot27.Value, draftOptions.Slot28.Value, draftOptions.Slot29.Value, draftOptions.Slot30.Value,
                draftOptions.Slot31.Value, draftOptions.Slot32.Value, draftOptions.Slot33.Value, draftOptions.Slot34.Value, draftOptions.Slot35.Value
            };

            if (slotIndex >= draftSlots.Length)
            {
                HideTooltip(__instance);
                return false;
            }

            var draftSlot = DraftSystem.RoleListSlotOrder.Count > slotIndex
                ? DraftSystem.GetRoleListBucketForPickIndex(slotIndex)
                : draftSlots[slotIndex];

            bucket = ToTownOfUsRoleListOption(draftSlot);
        }
        else if (poolMode == DraftPoolMode.MinMax)
        {
            switch (line)
            {
                case 2: bucket = RoleListOption.CrewRandom; break;
                case 3: bucket = RoleListOption.CrewInvest; break;
                case 4: bucket = RoleListOption.CrewKilling; break;
                case 5: bucket = RoleListOption.CrewPower; break;
                case 6: bucket = RoleListOption.CrewProtective; break;
                case 7: bucket = RoleListOption.CrewSupport; break;
                case 8: bucket = RoleListOption.ImpRandom; break;
                case 9: bucket = RoleListOption.ImpConceal; break;
                case 10: bucket = RoleListOption.ImpKilling; break;
                case 11: bucket = RoleListOption.ImpPower; break;
                case 12: bucket = RoleListOption.ImpSupport; break;
                case 13: bucket = RoleListOption.NeutRandom; break;
                case 14: bucket = RoleListOption.NeutBenign; break;
                case 15: bucket = RoleListOption.NeutEvil; break;
                case 16: bucket = RoleListOption.NeutKilling; break;
                case 17: bucket = RoleListOption.NeutOutlier; break;
            }
        }
        else if (poolMode == DraftPoolMode.OldDraft)
        {
            switch (line)
            {
                case 2: bucket = RoleListOption.NeutWildcard; break;
                case 3: bucket = RoleListOption.NeutKilling; break;
            }
        }

        if ((int)bucket < 0)
        {
            HideTooltip(__instance);
            return false;
        }

        ShowTooltipForBucket(__instance, bucket, line);
        return false;
    }

    private static void ShowTooltipForBucket(RoleListHoverComponent instance, RoleListOption bucket, int hoveredLine)
    {
        BucketTooltipData.TooltipInfo info;
        if (bucket == (RoleListOption)99)
        {
            BucketTooltipData.RoleEntry[] crewRoles = [];
            BucketTooltipData.RoleEntry[] neutRoles = [];

            if (BucketTooltipData.TryGet(RoleListOption.CrewRandom, out var crewInfo))
            {
                crewRoles = GetRoles(crewInfo);
            }
            if (BucketTooltipData.TryGet(RoleListOption.NeutWildcard, out var neutInfo))
            {
                neutRoles = GetRoles(neutInfo);
            }

            if (crewRoles.Length == 0 && neutRoles.Length == 0)
            {
                return;
            }

            var combined = new BucketTooltipData.RoleEntry[crewRoles.Length + neutRoles.Length];
            Array.Copy(crewRoles, 0, combined, 0, crewRoles.Length);
            Array.Copy(neutRoles, 0, combined, crewRoles.Length, neutRoles.Length);
            info = new BucketTooltipData.TooltipInfo(combined);
        }
        else
        {
            if (!BucketTooltipData.TryGet(bucket, out info)) return;
        }

        var textTarget = instance.TextTarget;
        if (textTarget == null) return;

        var originalText = OriginalTextRef(instance);
        if (string.IsNullOrEmpty(originalText))
        {
            originalText = textTarget.text;
            OriginalTextRef(instance) = originalText;
        }

        var lines = originalText.Split('\n');
        if (hoveredLine < lines.Length)
        {
            var copy = (string[])lines.Clone();
            copy[hoveredLine] = $"<i>{copy[hoveredLine]}</i>";
            textTarget.text = string.Join("\n", copy);
        }

        var lineInfo = textTarget.textInfo.lineInfo[hoveredLine];
        var lineWorldY = textTarget.transform.TransformPoint(new Vector3(0, lineInfo.ascender, 0)).y;
        var lineWorldX = textTarget.transform.TransformPoint(new Vector3(textTarget.bounds.max.x, 0, 0)).x;

        var cam = Camera.main;
        if (cam == null) return;

        var camTop = cam.transform.position.y + cam.orthographicSize;
        var camLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect;

        var yOffset = camTop - lineWorldY;
        var xOffset = lineWorldX - camLeft + 0.4f;

        var tooltipAp = TooltipApRef(instance);
        tooltipAp.DistanceFromEdge = new Vector3(xOffset, yOffset, 1f);
        tooltipAp.AdjustPosition();

        var tooltipBaseText = BucketTooltipData.BuildTooltipText(in info);
        TooltipBaseTextRef(instance) = tooltipBaseText;

        var tooltipTmp = TooltipTmpRef(instance);
        tooltipTmp.text = tooltipBaseText;
        tooltipTmp.ForceMeshUpdate();

        var tooltipGo = TooltipGoRef(instance);
        tooltipGo.SetActive(true);

        HudManagerPatches.IsHoveringRoleList = true;
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
            DraftRoleListOption.CrewNeu => (RoleListOption)99,
            _ => RoleListOption.NonImp
        };
    }
}
