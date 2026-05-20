using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Crewmate;

public sealed class PortalmakerPlaceButton : TownOfUsRoleButton<PortalmakerRole>
{
    public override string Name => TouLocale.Get("ExtensionRolePortalmakerPlace", "Place Portal");
    public override LoadableAsset<Sprite> Sprite => TouExtensionCrewAssets.PortalSprite;
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override float Cooldown => OptionGroupSingleton<PortalmakerOptions>.Instance.Cooldown;

    public override float EffectDuration => OptionGroupSingleton<PortalmakerOptions>.Instance.PlacementDelay;

    public override int MaxUses => (int)OptionGroupSingleton<PortalmakerOptions>.Instance.PortalUses;
    public override bool ZeroIsInfinite { get; set; } = true;

    public override Color TextOutlineColor => TouExtensionColors.Portalmaker;

    public Vector3? SavedPos { get; set; }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        if (Button != null)
        {
            Button.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        }
    }

    public override bool CanUse()
    {
        if (MeetingHud.Instance != null) return false;
        if (!base.CanUse() || Role == null) return false;
        if (LimitedUses && UsesLeft <= 0) return false;

        // Wall check
        if (Modules.PortalmakerSystem.IsNearWall(PlayerControl.LocalPlayer.GetTruePosition()))
            return false;

        return true;
    }

    protected override void OnClick()
    {
        if (Role == null || MeetingHud.Instance != null) return;

        var delay = OptionGroupSingleton<PortalmakerOptions>.Instance.PlacementDelay;
        if (delay <= 0f)
        {
            Role.PlacePortal(PlayerControl.LocalPlayer.GetTruePosition());
            Timer = Cooldown;
        }
        else
        {
            SavedPos = PlayerControl.LocalPlayer.GetTruePosition();
            Reactor.Utilities.Coroutines.Start(ShowNotificationCoroutine(delay));
        }
    }

    public override void OnEffectEnd()
    {
        base.OnEffectEnd();

        if (Role != null && SavedPos != null && MeetingHud.Instance == null && !PlayerControl.LocalPlayer.HasDied())
        {
            Role.PlacePortal(SavedPos.Value);
            Timer = Cooldown;
        }

        SavedPos = null;
    }

    private System.Collections.IEnumerator ShowNotificationCoroutine(float delay)
    {
        var player = PlayerControl.LocalPlayer;
        var notif = Helpers.CreateAndShowNotification(
            "<b>Placing portal...</b>",
            TouExtensionColors.Portalmaker,
            new Vector3(0f, 1.2f, -20f),
            spr: TouExtensionCrewAssets.PortalSprite.LoadAsset());
        notif.AdjustNotification();

        float start = Time.time;
        while (Time.time - start < delay)
        {
            if (MeetingHud.Instance != null || player.HasDied())
            {
                if (notif != null) UnityEngine.Object.Destroy(notif.gameObject);
                yield break;
            }

            float remaining = delay - (Time.time - start);
            if (notif != null && notif.Text != null)
            {
                notif.Text.text = $"<b>Placing portal in {remaining:F1}s...</b>";
            }

            yield return null;
        }

        if (notif != null) UnityEngine.Object.Destroy(notif.gameObject);
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);
    }
}
