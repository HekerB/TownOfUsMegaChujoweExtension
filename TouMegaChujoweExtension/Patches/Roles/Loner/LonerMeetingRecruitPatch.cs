using System.Linq;
using HarmonyLib;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using TMPro;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Utilities;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace TouMegaChujoweExtension.Patches.Roles.Loner;

[HarmonyPatch]
public static class LonerMeetingRecruitPatch
{
    private const float RecruitIconScale = 0.66f;
    private const float CancelIconScale = 0.66f;
    private const float HoverScaleMultiplier = 1.08f;
    private const float RecruitLabelWorldScale = 0.78f;
    private static readonly Color RecruitTextColor = new(0.45f, 0.72f, 1f, 1f);
    private static readonly Color RecruitHoverColor = Palette.ImpostorRed;
    private static readonly List<GameObject> RecruitButtons = [];
    private static readonly Dictionary<byte, SpriteRenderer> RecruitRenderers = [];
    private static readonly Dictionary<byte, TextMeshPro> RecruitLabels = [];
    private static byte? SelectedTargetId;
    private static byte? ExiledPlayerId;

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    public static void MeetingStartPostfix(MeetingHud __instance)
    {
        ClearButtons();
        SelectedTargetId = null;
        ExiledPlayerId = null;
        CreateRecruitButtons(__instance);
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
    [HarmonyPrefix]
    public static void VotingCompletePrefix(NetworkedPlayerInfo? exiled)
    {
        ExiledPlayerId = exiled?.PlayerId;
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    [HarmonyPostfix]
    public static void MeetingUpdatePostfix(MeetingHud __instance)
    {
        if (RecruitButtons.Count == 0)
        {
            CreateRecruitButtons(__instance);
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
    [HarmonyPostfix]
    public static void MeetingClosePostfix()
    {
        var selectedTargetId = SelectedTargetId;
        var exiledPlayerId = ExiledPlayerId;
        ClearButtons();
        SelectedTargetId = null;
        ExiledPlayerId = null;
        Coroutines.Start(CoResolveAfterMeeting(selectedTargetId, exiledPlayerId));
    }

    private static void CreateRecruitButtons(MeetingHud meetingHud)
    {
        var local = PlayerControl.LocalPlayer;
        if (meetingHud == null || local == null || local.Data?.Role is not LonerRole || LonerRole.HasRecruited(local) || local.HasDied())
        {
            return;
        }

        foreach (var voteArea in meetingHud.playerStates)
        {
            if (voteArea == null || voteArea.AmDead || voteArea.TargetPlayerId == local.PlayerId)
            {
                continue;
            }

            var target = MiscUtils.PlayerById(voteArea.TargetPlayerId);
            if (target == null || target.HasDied() || target.IsImpostor())
            {
                continue;
            }

            var cancelButton = voteArea.Buttons?.transform.Find("CancelButton");
            if (cancelButton == null)
            {
                continue;
            }

            var recruitButton = UObject.Instantiate(cancelButton.gameObject, voteArea.transform);
            recruitButton.name = "LonerRecruitButton";
            recruitButton.transform.localPosition = new Vector3(-0.40f, 0.05f, -3f);
            recruitButton.transform.localScale = new Vector3(RecruitIconScale, RecruitIconScale, 1f);

            var renderer = recruitButton.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = Color.white;
                SetButtonVisual(renderer, null, selected: false);
            }

            var button = recruitButton.GetComponent<PassiveButton>();
            if (button != null)
            {
                var targetId = voteArea.TargetPlayerId;
                button.OverrideOnClickListeners(() => OnRecruitClicked(targetId));
                button.OverrideOnMouseOverListeners(() =>
                {
                    RecruitLabels.TryGetValue(targetId, out var label);
                    SetButtonVisual(renderer, label, SelectedTargetId == targetId, hovered: true);
                    if (label != null)
                    {
                        SetLabel(label, SelectedTargetId == targetId ? "Cancel" : "Recruit", RecruitHoverColor);
                    }
                });
                button.OverrideOnMouseOutListeners(() =>
                {
                    if (RecruitLabels.TryGetValue(targetId, out var label) && label != null)
                    {
                        var selected = SelectedTargetId == targetId;
                        SetButtonVisual(renderer, label, selected);
                        SetLabel(label, selected ? "Cancel" : "Recruit", selected ? RecruitHoverColor : RecruitTextColor);
                    }
                });
            }

            var collider = recruitButton.GetComponent<BoxCollider2D>();
            if (collider != null && renderer?.sprite != null)
            {
                collider.size = renderer.sprite.bounds.size;
                collider.offset = Vector2.zero;
            }

            if (recruitButton.transform.childCount > 0)
            {
                recruitButton.transform.GetChild(0).gameObject.Destroy();
            }

            var label = CreateRecruitLabel(meetingHud, recruitButton.transform);
            SetButtonVisual(renderer, label, selected: false);

            recruitButton.SetActive(true);
            RecruitButtons.Add(recruitButton);
            RecruitRenderers[voteArea.TargetPlayerId] = renderer!;
            RecruitLabels[voteArea.TargetPlayerId] = label;
        }
    }

    private static TextMeshPro CreateRecruitLabel(MeetingHud meetingHud, Transform parent)
    {
        var template = meetingHud.MeetingAbilityButton.buttonLabelText.gameObject;
        var labelObj = UObject.Instantiate(template, parent);
        labelObj.name = "LonerRecruitLabel";
        labelObj.transform.localPosition = new Vector3(0f, -0.34f, 0f);
        SetLabelScale(labelObj.transform, RecruitIconScale);

        labelObj.GetComponent<TextTranslatorTMP>()?.Destroy();

        var label = labelObj.GetComponent<TextMeshPro>();
        label.richText = true;
        SetLabel(label, "Recruit", RecruitTextColor);
        label.fontSize = 3.2f;
        label.fontSizeMax = 3.2f;
        label.fontSizeMin = 3.2f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = RecruitTextColor;
        label.m_enableWordWrapping = false;

        return label;
    }

    private static void OnRecruitClicked(byte targetId)
    {
        var local = PlayerControl.LocalPlayer;
        var target = MiscUtils.PlayerById(targetId);
        if (local == null || target == null || local.Data?.Role is not LonerRole || target.HasDied() || target.IsImpostor())
        {
            return;
        }

        SelectedTargetId = SelectedTargetId == targetId ? null : targetId;
        UpdateRecruitSelection();
    }

    private static void UpdateRecruitSelection()
    {
        foreach (var (targetId, renderer) in RecruitRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            var selected = SelectedTargetId == targetId;
            RecruitLabels.TryGetValue(targetId, out var label);
            SetButtonVisual(renderer, label, selected);
        }

        foreach (var (targetId, label) in RecruitLabels)
        {
            if (label == null)
            {
                continue;
            }

            var selected = SelectedTargetId == targetId;
            SetLabel(label, selected ? "Cancel" : "Recruit", selected ? RecruitHoverColor : RecruitTextColor);
        }
    }

    private static void SetLabel(TextMeshPro label, string text, Color color)
    {
        label.color = color;
        label.text = $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
    }

    private static void SetButtonVisual(SpriteRenderer? renderer, TextMeshPro? label, bool selected, bool hovered = false)
    {
        if (renderer != null)
        {
            var scale = selected ? CancelIconScale : RecruitIconScale;
            var effectiveScale = hovered ? scale * HoverScaleMultiplier : scale;
            renderer.sprite = selected ? TouExtensionAssets.LonerImpostorSprite.LoadAsset() : TouExtensionAssets.LonerCrewSprite.LoadAsset();
            renderer.color = hovered && !selected ? new Color(1f, 0.75f, 0.75f, 1f) : Color.white;
            renderer.transform.localScale = new Vector3(effectiveScale, effectiveScale, 0.75f);
            SetLabelScale(label?.transform, effectiveScale);
        }
    }

    private static void SetLabelScale(Transform? labelTransform, float parentScale)
    {
        if (labelTransform == null)
        {
            return;
        }

        var labelScale = RecruitLabelWorldScale / parentScale;
        labelTransform.localScale = new Vector3(labelScale, labelScale, 1f);
    }

    private static System.Collections.IEnumerator CoResolveAfterMeeting(byte? selectedTargetId, byte? exiledPlayerId)
    {
        yield return new WaitForSeconds(0.35f);

        if (selectedTargetId.HasValue)
        {
            CommitSelectedRecruit(selectedTargetId.Value, exiledPlayerId);
        }

        var local = PlayerControl.LocalPlayer;
        if (local != null && !local.HasDied() && exiledPlayerId != local.PlayerId)
        {
            LonerRole.TriggerPendingMutationLocal();
        }
    }

    private static void CommitSelectedRecruit(byte selectedTargetId, byte? exiledPlayerId)
    {
        var local = PlayerControl.LocalPlayer;
        var target = MiscUtils.PlayerById(selectedTargetId);
        if (local == null || target == null || local.Data?.Role is not LonerRole)
        {
            return;
        }

        if (local.HasDied() || exiledPlayerId == local.PlayerId)
        {
            return;
        }

        if (target.HasDied() || exiledPlayerId == target.PlayerId || target.IsImpostor())
        {
            return;
        }

        LonerRole.RpcRecruit(local, target);
    }

    private static void ClearButtons()
    {
        foreach (var button in RecruitButtons.Where(button => button is not null))
        {
            button.Destroy();
        }

        RecruitButtons.Clear();
        RecruitRenderers.Clear();
        RecruitLabels.Clear();
    }
}
