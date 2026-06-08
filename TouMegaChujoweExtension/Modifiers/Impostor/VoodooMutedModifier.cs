using System;
using System.Linq;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public sealed class VoodooMutedModifier : BaseModifier
{
    private bool shookAlready = true;
    private PlayerVoteArea? voteArea;
    private SpriteRenderer? bmIcon;
    private SpriteRenderer? bmOverlay;

    public override string ModifierName => "Voodoo Muted";
    public override bool HideOnUi => true;
    public int MeetingsRemaining { get; set; } = 1;

    public VoodooMutedModifier()
    {
    }

    public VoodooMutedModifier(int meetingsRemaining)
    {
        MeetingsRemaining = Math.Max(1, meetingsRemaining);
    }

    public override void OnActivate()
    {
        base.OnActivate();
        CreateLocalBlackmailVisuals();
    }

    public override void OnMeetingStart()
    {
        base.OnMeetingStart();
        DestroyLocalBlackmailVisuals();
        CreateLocalBlackmailVisuals();
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        var meetingInstance = MeetingHud.Instance;
        if (meetingInstance == null)
        {
            return;
        }

        if (voteArea == null || bmOverlay == null)
        {
            CreateLocalBlackmailVisuals();
        }

        if (voteArea == null || bmOverlay == null)
        {
            return;
        }

        if (meetingInstance.state != MeetingHud.VoteStates.Animating && !shookAlready)
        {
            shookAlready = true;
            meetingInstance.StartCoroutine(Effects.SwayX(bmOverlay.transform));
        }
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();
        DestroyLocalBlackmailVisuals();
    }

    private void DestroyLocalBlackmailVisuals()
    {
        if (voteArea != null && voteArea.ColorBlindName != null)
        {
            voteArea.ColorBlindName.gameObject.SetActive(true);
        }

        if (bmIcon != null)
        {
            Object.Destroy(bmIcon.gameObject);
            bmIcon = null;
        }

        if (bmOverlay != null)
        {
            Object.Destroy(bmOverlay.gameObject);
            bmOverlay = null;
        }

        voteArea = null;
    }

    private void CreateLocalBlackmailVisuals()
    {
        var localPlayer = PlayerControl.LocalPlayer;
        var canSeeMuted = Player.AmOwner ||
                          (OptionGroupSingleton<VoodooMasterOptions>.Instance.ImpostorsSeeMuted &&
                           localPlayer != null &&
                           localPlayer.IsImpostorAligned());

        if (!canSeeMuted || bmOverlay != null)
        {
            return;
        }

        var meetingInstance = MeetingHud.Instance;
        if (meetingInstance == null)
        {
            return;
        }

        voteArea = meetingInstance.playerStates.FirstOrDefault(x => x.TargetPlayerId == Player.PlayerId);
        if (voteArea == null || voteArea.XMark == null)
        {
            return;
        }

        shookAlready = false;

        bmIcon = Object.Instantiate(voteArea.XMark, voteArea.XMark.transform.parent);
        bmIcon.transform.localPosition = new Vector3(-0.804f, -0.212f, -2f);
        bmIcon.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
        bmIcon.sprite = TouAssets.BlackmailLetterSprite.LoadAsset();
        bmIcon.gameObject.SetActive(true);

        bmOverlay = Object.Instantiate(voteArea.XMark, voteArea.XMark.transform.parent);
        bmOverlay.transform.localPosition = new Vector3(0f, 0f, -2f);
        bmOverlay.transform.localScale = new Vector3(0.769f, 1f, 1f);
        bmOverlay.sprite = TouAssets.BlackmailOverlaySprite.LoadAsset();
        bmOverlay.gameObject.SetActive(true);

        voteArea.ColorBlindName.gameObject.SetActive(false);
    }
}
