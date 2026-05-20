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
    public override LoadableAsset<Sprite> Sprite => TouExtensionCrewAssets.PortalSprite;
    public override BaseKeybind Keybind => Keybinds.TertiaryAction;
    public override float Cooldown => 0f;

    public override Color TextOutlineColor => TouExtensionColors.Portalmaker;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        if (Button != null)
        {
            Button.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        }
    }

    public override bool Enabled(RoleBehaviour? role)
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null || PlayerControl.LocalPlayer.Data.IsDead) return false;
        if (MeetingHud.Instance != null) return false;

        var opts = OptionGroupSingleton<PortalmakerOptions>.Instance;
        return opts.Mode == TeleportMode.Interaction;
    }

    public override bool CanUse()
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null || PlayerControl.LocalPlayer.Data.IsDead) return false;
        if (MeetingHud.Instance != null) return false;

        var opts = OptionGroupSingleton<PortalmakerOptions>.Instance;
        if (opts.Mode != TeleportMode.Interaction) return false;

        // Check if teleport cooldown is active
        if (PortalmakerSystem.GetTeleportCooldownRemaining(PlayerControl.LocalPlayer.PlayerId) > 0f) return false;

        return PortalmakerSystem.IsNearPortalPair(PlayerControl.LocalPlayer);
    }

    protected override void OnClick()
    {
        if (MeetingHud.Instance != null) return;
        PortalmakerSystem.TriggerTeleport(PlayerControl.LocalPlayer);
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);
        if (playerControl.AmOwner)
        {
            var opts = OptionGroupSingleton<PortalmakerOptions>.Instance;
            bool isNear = PortalmakerSystem.IsNearPortalPair(playerControl);
            Button?.gameObject.SetActive(opts.Mode == TeleportMode.Interaction && isNear && MeetingHud.Instance == null);

            if (Button != null && Button.gameObject.activeSelf)
            {
                float cooldownRemaining = PortalmakerSystem.GetTeleportCooldownRemaining(playerControl.PlayerId);
                if (cooldownRemaining > 0f)
                {
                    Timer = cooldownRemaining;
                }
                else
                {
                    Timer = 0f;
                }
            }
        }
    }
}
