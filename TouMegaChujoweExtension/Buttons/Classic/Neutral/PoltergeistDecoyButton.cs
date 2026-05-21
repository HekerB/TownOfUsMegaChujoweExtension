using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using UnityEngine;
using TownOfUs.Modules.Localization;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Neutral;
using MiraAPI.Hud;
using MiraAPI.Modifiers;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class PoltergeistDecoyButton : TownOfUsButton
{
    public override string Name => "Decoy";
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Poltergeist;
    public override float Cooldown => OptionGroupSingleton<PoltergeistOptions>.Instance.DecoyCooldown;
    public override LoadableAsset<Sprite> Sprite => TouExtensionIcons.PoltergeistRoleIcon;
    public override bool UsableInDeath => true;

    public override bool Enabled(RoleBehaviour? role)
    {
        return PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data?.Role is PoltergeistRole;
    }

    public override bool CanUse()
    {
        if (MeetingHud.Instance || ExileController.Instance) return false;
        if (PlayerControl.LocalPlayer == null) return false;
        if (Minigame.Instance) return false;

        return Timer <= 0f;
    }

    protected override void OnClick()
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null) return;

        var menu = CustomPlayerMenu.Create();
        if (menu == null) return;

        menu.transform.FindChild("PhoneUI").GetChild(0).GetComponent<SpriteRenderer>().material =
            localPlayer.cosmetics.currentBodySprite.BodySprite.material;
        menu.transform.FindChild("PhoneUI").GetChild(1).GetComponent<SpriteRenderer>().material =
            localPlayer.cosmetics.currentBodySprite.BodySprite.material;

        menu.Begin(
            plr => plr != null && !plr.HasDied() && plr.PlayerId != localPlayer.PlayerId,
            plr =>
            {
                menu.ForceClose();

                if (plr == null)
                {
                    Timer = 0.01f;
                    return;
                }

                // Spawn the Poltergeist decoy (isPoltergeist = true)!
                DecoySystem.RpcSpawnDecoy(localPlayer, plr, localPlayer.transform.position, true);

                Timer = Cooldown;
            }
        );
    }
}
