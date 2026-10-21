using System;
using MiraAPI.Hud;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Patches.Draft;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Draft;

public sealed class DraftCancelButton : TownOfUsButton
{
    public static void Show()
    {
        try
        {
            var instance = CustomButtonSingleton<DraftCancelButton>.Instance;
            if (instance != null)
            {
                instance.Disabled = false;
                instance.Timer = 0f;
                instance.Button?.SetEnabled();
            }
        }
        catch {}
    }

    public static void Hide()
    {
        try
        {
            var instance = CustomButtonSingleton<DraftCancelButton>.Instance;
            if (instance != null) instance.Disabled = true;
        }
        catch {}
    }

    public override string Name => "Cancel Draft";
    public override float InitialCooldown => 0.001f;
    public override float Cooldown => 0.001f;

    public override bool ZeroIsInfinite { get; set; } = true;
    public override ButtonLocation Location => ButtonLocation.BottomRight;

    public override Color TextOutlineColor => new Color32(198, 22, 22, 255);

    public override LoadableAsset<Sprite> Sprite => TouExtensionIcons.DraftQuitButton;

    public override bool Disabled { get; set; } = true;

    public override bool Enabled(RoleBehaviour? role)
    {
        return AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && !Disabled;
    }

    // Override CanUse to bypass lobby restrictions (CanMove, UseButton checks etc.)
    public override bool CanUse()
    {
        if (!PlayerControl.LocalPlayer) return false;
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return false;
        return (DraftLobbyPatch.DraftInProgress || DraftLobbyPatch.DraftCompletedWaitingForStart) && !Disabled;
    }

    public override bool CanClick()
    {
        return CanUse();
    }

    public override void SetActive(bool visible, RoleBehaviour role)
    {
        if ((DraftLobbyPatch.DraftInProgress || DraftLobbyPatch.DraftCompletedWaitingForStart) && !Disabled &&
            AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            Button?.ToggleVisible(true);
            Button?.SetEnabled();
            Timer = 0f;
            return;
        }

        base.SetActive(visible, role);
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        if (Button == null) return;

        if ((DraftLobbyPatch.DraftInProgress || DraftLobbyPatch.DraftCompletedWaitingForStart) && !Disabled &&
            AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            Timer = 0f;
            Button.transform.parent?.gameObject.SetActive(true);
            Button.gameObject.SetActive(true);
            Button.SetEnabled();
            Button.cooldownTimerText?.gameObject.SetActive(false);
            foreach (var renderer in Button.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
                renderer.sortingOrder = 100;
            }
            var pos = Button.transform.localPosition;
            Button.transform.localPosition = new Vector3(pos.x, pos.y, -520f);
        }
        else
        {
            Button.gameObject.SetActive(false);
        }
    }

    protected override void OnClick()
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
        if (!DraftLobbyPatch.DraftInProgress && !DraftLobbyPatch.DraftCompletedWaitingForStart) return;

        try
        {
            DraftNetworking.SendDraftCancel();
        }
        catch (Exception ex)
        {
            try { Reactor.Utilities.Logger<TouMegaChujoweExtensionPlugin>.Error($"[DraftCancelButton] Cancel clicked error: {ex.Message}"); } catch {}
        }
        Hide();
    }
}
