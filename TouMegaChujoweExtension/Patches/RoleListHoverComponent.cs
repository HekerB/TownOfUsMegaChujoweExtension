/*
using System;
using System.Text;
using MiraAPI.GameOptions;
using TMPro;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options;
using TownOfUs.Modules.Components;
using TownOfUs.Options;
using TownOfUs.Patches;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Draft;

public static class DraftRoleListHoverStandalone
{
    private static GameObject? _tooltipGo;
    private static TextMeshPro? _tooltipTmp;
    private static AspectPosition? _tooltipAp;
    private static string _tooltipBaseText = string.Empty;
    private static int _lastLine = -1;

    private const float BaseX = 0.43f;
    private const float BaseY = 0.1f;

    public static void Hide()
    {
        if (_tooltipGo != null)
        {
            _tooltipGo.SetActive(false);
        }

        _tooltipBaseText = string.Empty;
        _lastLine = -1;
        HudManagerPatches.IsHoveringRoleList = false;
    }

    public static void Update(TextMeshPro textTarget)
    {
        if (!DraftSystem.IsEnabled)
        {
            Hide();
            return;
        }

        if (textTarget == null || !textTarget.gameObject.activeSelf)
        {
            Hide();
            return;
        }

        if (!TryGetDraftPoolMode(out var poolMode))
        {
            Hide();
            return;
        }

        if (poolMode != DraftPoolMode.RoleList && poolMode != DraftPoolMode.OldDraft)
        {
            Hide();
            return;
        }

        EnsureTooltip(textTarget);

        if (_tooltipGo != null && _tooltipGo.activeSelf)
        {
            UpdateTooltipLinks();
        }

        textTarget.ForceMeshUpdate();

        var line = GetLineUnderMouse(textTarget);

        if (line < 0)
        {
            Hide();
            return;
        }

        if (!TryGetTooltipForLine(textTarget, poolMode, line, out var tooltipText))
        {
            Hide();
            return;
        }

        _lastLine = line;
        ItalicizeLine(textTarget, line);
        ShowTooltip(textTarget, line, tooltipText);
    }

    private static bool TryGetDraftPoolMode(out DraftPoolMode poolMode)
    {
        try
        {
            poolMode = OptionGroupSingleton<DraftModeOptions>.Instance.PoolMode.Value;
            return true;
        }
        catch
        {
            poolMode = DraftPoolMode.OldDraft;
            return false;
        }
    }

    private static void EnsureTooltip(TextMeshPro textTarget)
    {
        if (_tooltipGo != null)
        {
            return;
        }

        var pingTracker = UnityEngine.Object.FindObjectOfType<PingTracker>(true);

        if (pingTracker == null || HudManager.Instance == null)
        {
            return;
        }

        _tooltipGo = UnityEngine.Object.Instantiate(pingTracker.gameObject, HudManager.Instance.transform);
        _tooltipGo.name = "DraftBucketTooltipText";

        var pt = _tooltipGo.GetComponent<PingTracker>();

        if (pt != null)
        {
            UnityEngine.Object.Destroy(pt);
        }

        _tooltipAp = _tooltipGo.GetComponent<AspectPosition>();
        _tooltipAp.Alignment = AspectPosition.EdgeAlignments.LeftTop;
        _tooltipAp.DistanceFromEdge = new Vector3(BaseX, BaseY, 1f);
        _tooltipAp.AdjustPosition();

        _tooltipTmp = _tooltipGo.GetComponent<TextMeshPro>();
        _tooltipTmp.alignment = TextAlignmentOptions.TopLeft;
        _tooltipTmp.verticalAlignment = VerticalAlignmentOptions.Top;
        _tooltipTmp.fontSize = _tooltipTmp.fontSizeMin = _tooltipTmp.fontSizeMax = textTarget.fontSize;
        _tooltipTmp.enableWordWrapping = false;
        _tooltipTmp.text = "";

        _tooltipGo.SetActive(false);
    }

    private static void UpdateTooltipLinks()
    {
        if (_tooltipTmp == null || _tooltipBaseText == string.Empty)
        {
            return;
        }

        var cam = Camera.main;

        if (cam == null)
        {
            return;
        }

        _tooltipTmp.text = _tooltipBaseText;

        var linkIndex = TMP_TextUtilities.FindIntersectingLink(_tooltipTmp, Input.mousePosition, cam);

        if (linkIndex < 0 || linkIndex >= _tooltipTmp.textInfo.linkCount)
        {
            return;
        }

        var linkInfo = _tooltipTmp.textInfo.linkInfo[linkIndex];
        var linkId = linkInfo.GetLinkID();

        _tooltipTmp.text = _tooltipBaseText.Replace(
            $"<link=\"{linkId}\">",
            $"<link=\"{linkId}\"><i>").Replace(
            "</link>",
            "</i></link>",
            StringComparison.Ordinal);

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (Minigame.Instance)
        {
            return;
        }

        WikiHyperlink.OpenHyperlink(linkInfo);
    }

    private static int GetLineUnderMouse(TextMeshPro textTarget)
    {
        if (textTarget.textInfo == null || textTarget.textInfo.lineCount == 0)
        {
            return -1;
        }

        var cam = Camera.main;

        if (cam == null)
        {
            return -1;
        }

        if (_tooltipGo != null && _tooltipGo.activeSelf && IsMouseOverTooltip())
        {
            return _lastLine;
        }

        var worldPos = cam.ScreenToWorldPoint(new Vector3(
            Input.mousePosition.x,
            Input.mousePosition.y,
            -cam.transform.position.z));

        var localPos = textTarget.transform.InverseTransformPoint(worldPos);
        var bounds = textTarget.bounds;

        if (localPos.x < bounds.min.x || localPos.x > bounds.max.x)
        {
            return -1;
        }

        for (var i = 0; i < textTarget.textInfo.lineCount; i++)
        {
            var li = textTarget.textInfo.lineInfo[i];

            if (li.characterCount == 0)
            {
                continue;
            }

            if (localPos.y <= li.ascender && localPos.y >= li.descender)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsMouseOverTooltip()
    {
        if (_tooltipTmp == null)
        {
            return false;
        }

        var cam = Camera.main;

        if (cam == null)
        {
            return false;
        }

        var worldPos = cam.ScreenToWorldPoint(new Vector3(
            Input.mousePosition.x,
            Input.mousePosition.y,
            -cam.transform.position.z));

        var localPos = _tooltipTmp.transform.InverseTransformPoint(worldPos);
        var bounds = _tooltipTmp.bounds;

        return localPos.x >= bounds.min.x &&
               localPos.x <= bounds.max.x &&
               localPos.y >= bounds.min.y &&
               localPos.y <= bounds.max.y;
    }

    private static void ItalicizeLine(TextMeshPro textTarget, int hoveredLine)
    {
        var lines = textTarget.text.Split('\n');

        if (hoveredLine < 0 || hoveredLine >= lines.Length)
        {
            return;
        }

        if (lines[hoveredLine].StartsWith("<i>", StringComparison.Ordinal))
        {
            return;
        }

        lines[hoveredLine] = $"<i>{lines[hoveredLine]}</i>";
        textTarget.text = string.Join("\n", lines);
        textTarget.ForceMeshUpdate();
    }

    private static void ShowTooltip(TextMeshPro textTarget, int hoveredLine, string tooltipText)
    {
        if (_tooltipGo == null || _tooltipTmp == null || _tooltipAp == null)
        {
            return;
        }

        var lineInfo = textTarget.textInfo.lineInfo[hoveredLine];
        var lineWorldY = textTarget.transform.TransformPoint(new Vector3(0, lineInfo.ascender, 0)).y;
        var lineWorldX = textTarget.transform.TransformPoint(new Vector3(textTarget.bounds.max.x, 0, 0)).x;

        var cam = Camera.main;

        if (cam == null)
        {
            return;
        }

        var camTop = cam.transform.position.y + cam.orthographicSize;
        var camLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect;

        var yOffset = camTop - lineWorldY;
        var xOffset = lineWorldX - camLeft + 0.4f;

        _tooltipAp.DistanceFromEdge = new Vector3(xOffset, yOffset, 1f);
        _tooltipAp.AdjustPosition();

        _tooltipBaseText = tooltipText;
        _tooltipTmp.text = _tooltipBaseText;
        _tooltipTmp.ForceMeshUpdate();
        _tooltipGo.SetActive(true);

        HudManagerPatches.IsHoveringRoleList = true;
    }

    private static bool TryGetTooltipForLine(TextMeshPro textTarget, DraftPoolMode poolMode, int hoveredLine, out string tooltipText)
    {
        tooltipText = string.Empty;

        if (poolMode == DraftPoolMode.RoleList)
        {
            return TryGetDraftRoleListTooltip(textTarget, hoveredLine, out tooltipText);
        }

        if (poolMode == DraftPoolMode.OldDraft)
        {
            return TryGetOldDraftTooltip(textTarget, hoveredLine, out tooltipText);
        }

        return false;
    }

    private static bool TryGetDraftRoleListTooltip(TextMeshPro textTarget, int hoveredLine, out string tooltipText)
    {
        tooltipText = string.Empty;

        var slotIndex = GetBucketSlotIndexFromLine(textTarget, hoveredLine);

        if (slotIndex < 0)
        {
            return false;
        }

        var bucket = DraftSystem.RoleListSlotOrder.Count > slotIndex
            ? DraftSystem.GetRoleListBucketForPickIndex(slotIndex)
            : GetDraftRoleListOptionBySlot(slotIndex);

        if ((int)bucket < 0)
        {
            return false;
        }

        return TryBuildDraftTooltip(bucket, out tooltipText);
    }

    private static bool TryGetOldDraftTooltip(TextMeshPro textTarget, int hoveredLine, out string tooltipText)
    {
        tooltipText = string.Empty;

        var lineText = GetPlainLineText(textTarget, hoveredLine);

        if (IndexOfIgnoreCase(lineText, "Neutral Killing") >= 0)
        {
            return TryBuildTooltip(RoleListOption.NeutKilling, out tooltipText);
        }

        if (IndexOfIgnoreCase(lineText, "Neutral Other") >= 0)
        {
            return TryBuildTooltip(RoleListOption.NeutWildcard, out tooltipText);
        }

        return false;
    }

    private static bool TryBuildDraftTooltip(DraftRoleListOption bucket, out string tooltipText)
    {
        tooltipText = string.Empty;

        if (bucket == DraftRoleListOption.CrewNeu)
        {
            return TryBuildMergedTooltip(out tooltipText, RoleListOption.CrewRandom, RoleListOption.NeutWildcard);
        }

        return TryBuildTooltip(ToTownOfUsRoleListOption(bucket), out tooltipText);
    }

    private static bool TryBuildTooltip(RoleListOption bucket, out string tooltipText)
    {
        tooltipText = string.Empty;

        if (!BucketTooltipData.TryGet(bucket, out var info))
        {
            return false;
        }

        tooltipText = BucketTooltipData.BuildTooltipText(in info);
        return !string.IsNullOrWhiteSpace(tooltipText);
    }

    private static bool TryBuildMergedTooltip(out string tooltipText, params RoleListOption[] buckets)
    {
        var builder = new StringBuilder();

        foreach (var bucket in buckets)
        {
            if (!TryBuildTooltip(bucket, out var part))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(part);
        }

        tooltipText = builder.ToString();
        return !string.IsNullOrWhiteSpace(tooltipText);
    }

    private static DraftRoleListOption GetDraftRoleListOptionBySlot(int slotIndex)
    {
        var roleList = OptionGroupSingleton<DraftRoleListSettingsOptions>.Instance;

        return slotIndex switch
        {
            0 => roleList.Slot1.Value,
            1 => roleList.Slot2.Value,
            2 => roleList.Slot3.Value,
            3 => roleList.Slot4.Value,
            4 => roleList.Slot5.Value,
            5 => roleList.Slot6.Value,
            6 => roleList.Slot7.Value,
            7 => roleList.Slot8.Value,
            8 => roleList.Slot9.Value,
            9 => roleList.Slot10.Value,
            10 => roleList.Slot11.Value,
            11 => roleList.Slot12.Value,
            12 => roleList.Slot13.Value,
            13 => roleList.Slot14.Value,
            14 => roleList.Slot15.Value,
            15 => roleList.Slot16.Value,
            16 => roleList.Slot17.Value,
            17 => roleList.Slot18.Value,
            18 => roleList.Slot19.Value,
            19 => roleList.Slot20.Value,
            20 => roleList.Slot21.Value,
            21 => roleList.Slot22.Value,
            22 => roleList.Slot23.Value,
            23 => roleList.Slot24.Value,
            24 => roleList.Slot25.Value,
            25 => roleList.Slot26.Value,
            26 => roleList.Slot27.Value,
            27 => roleList.Slot28.Value,
            28 => roleList.Slot29.Value,
            29 => roleList.Slot30.Value,
            30 => roleList.Slot31.Value,
            31 => roleList.Slot32.Value,
            32 => roleList.Slot33.Value,
            33 => roleList.Slot34.Value,
            34 => roleList.Slot35.Value,
            _ => (DraftRoleListOption)(-1)
        };
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

    private static int GetBucketSlotIndexFromLine(TextMeshPro textTarget, int hoveredLine)
    {
        var firstBucketLine = GetFirstBucketLine(textTarget);

        if (firstBucketLine < 0)
        {
            return -1;
        }

        return hoveredLine - firstBucketLine;
    }

    private static int GetFirstBucketLine(TextMeshPro textTarget)
    {
        var lines if (firstBucketLine < 0)
        {
            return -1;
        }

        return hoveredLine - firstBucketLine;
    }

    = textTarget.text.Split('\n');

        for (var i = 0; i<lines.Length; i++)
        {
            var line = RemoveRichTextTags(lines[i]).Trim();

            if (line.Length <= 0)
            {
                continue;
            }

            if (IndexOfIgnoreCase(line, "Draft Mode:") >= 0)
            {
                continue;
            }

            if (line.EndsWith(":", StringComparison.Ordinal))
            {
                continue;
            }

            return i;
        }

        return -1;
    }

    private static string GetPlainLineText(TextMeshPro textTarget, int line)
{
    var lines = textTarget.text.Split('\n');

    if (line < 0 || line >= lines.Length)
    {
        return string.Empty;
    }

    return RemoveRichTextTags(lines[line]).Trim();
}

private static string RemoveRichTextTags(string text)
{
    var builder = new StringBuilder(text.Length);
    var inTag = false;

    foreach (var c in text)
    {
        if (c == '<')
        {
            inTag = true;
            continue;
        }

        if (c == '>')
        {
            inTag = false;
            continue;
        }

        if (!inTag)
        {
            builder.Append(c);
        }
    }

    return builder.ToString();
}

private static int IndexOfIgnoreCase(string source, string value)
{
    return source.IndexOf(value, StringComparison.OrdinalIgnoreCase);
}
*/