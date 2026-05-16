using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Crewmate;

public sealed class PortalmakerPlaceButton : TownOfUsRoleButton<PortalmakerRole>
{
    public override string Name => TouLocale.Get("ExtensionRolePortalmakerPlace", "Place Portal");
    public override LoadableAsset<Sprite> Sprite => TouExtensionCrewAssets.PortalSprite;
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override float Cooldown => OptionGroupSingleton<PortalmakerOptions>.Instance.Cooldown;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        if (Button != null)
        {
            Button.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        }
    }

    private float _placementTimer;
    private Vector2 _placementPos;

    public override bool CanUse()
    {
        if (!base.CanUse() || Role == null) return false;
        if (_placementTimer > 0) return false;

        // Wall check
        if (Modules.PortalmakerSystem.IsNearWall(PlayerControl.LocalPlayer.GetTruePosition()))
            return false;

        return true;
    }

    protected override void OnClick()
    {
        if (Role == null) return;

        var delay = OptionGroupSingleton<PortalmakerOptions>.Instance.PlacementDelay;
        if (delay <= 0f)
        {
            Role.PlacePortal(PlayerControl.LocalPlayer.GetTruePosition());
        }
        else
        {
            _placementTimer = delay;
            _placementPos = PlayerControl.LocalPlayer.GetTruePosition();
            Timer = _placementTimer;
        }
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (_placementTimer > 0)
        {
            _placementTimer -= Time.fixedDeltaTime;
            Timer = _placementTimer;

            if (_placementTimer <= 0)
            {
                _placementTimer = 0;
                Role.PlacePortal(_placementPos);
                Timer = Cooldown;
            }
        }
    }
}
