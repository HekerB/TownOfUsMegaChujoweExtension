using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Roles;
using UnityEngine;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Buttons.Classic.Crewmate;

public sealed class PortalmakerTeleportButton : TownOfUsRoleButton<RoleBehaviour>
{
    public override string Name => TouLocale.Get("ExtensionRolePortalmakerTeleport", "Teleport");
    public override LoadableAsset<Sprite> Sprite => TouAssets.VentSprite;
    public override BaseKeybind Keybind => Keybinds.TertiaryAction;
    public override float Cooldown => 0f;

    public override bool Enabled(RoleBehaviour? role)
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null || PlayerControl.LocalPlayer.Data.IsDead) return false;

        var opts = OptionGroupSingleton<PortalmakerOptions>.Instance;
        return opts.Mode == TeleportMode.Interaction;
    }

    public override bool CanUse()
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null || PlayerControl.LocalPlayer.Data.IsDead) return false;

        var opts = OptionGroupSingleton<PortalmakerOptions>.Instance;
        if (opts.Mode != TeleportMode.Interaction) return false;

        return PortalmakerSystem.IsNearPortalPair(PlayerControl.LocalPlayer);
    }

    protected override void OnClick()
    {
        PortalmakerSystem.TriggerTeleport(PlayerControl.LocalPlayer);
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        if (Button != null) VentUtilities.InitializeVentButton(Button);
        base.FixedUpdate(playerControl);
        if (playerControl.AmOwner)
        {
            var opts = OptionGroupSingleton<PortalmakerOptions>.Instance;
            Button?.gameObject.SetActive(opts.Mode == TeleportMode.Interaction && PortalmakerSystem.IsNearPortalPair(playerControl));
        }
    }
}
