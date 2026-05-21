using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using UnityEngine;
using TownOfUs.Modules.Localization;
using MiraAPI.Hud;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class BodySwapperDecoyButton : TownOfUsButton
{
    public override string Name => "Decoy";
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => Palette.ImpostorRed;
    public override float Cooldown => OptionGroupSingleton<BodySwapperOptions>.Instance.Cooldown;
    public override LoadableAsset<Sprite> Sprite => TouExtensionIcons.BodySwapperRoleIcon;

    public override bool Enabled(RoleBehaviour? role)
    {
        return PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data?.Role is BodySwapperRole;
    }

    public override bool CanUse()
    {
        if (MeetingHud.Instance || ExileController.Instance) return false;
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.HasDied()) return false;
        if (PlayerControl.LocalPlayer.inVent) return false;
        if (Minigame.Instance) return false;

        return Timer <= 0f;
    }

    protected override void OnClick()
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null) return;

        var menu = CustomPlayerMenu.Create();
        if (menu == null) return;

        // Custom material setups for the tablet UI, consistent with premium style
        menu.transform.FindChild("PhoneUI").GetChild(0).GetComponent<SpriteRenderer>().material =
            localPlayer.cosmetics.currentBodySprite.BodySprite.material;
        menu.transform.FindChild("PhoneUI").GetChild(1).GetComponent<SpriteRenderer>().material =
            localPlayer.cosmetics.currentBodySprite.BodySprite.material;

        menu.Begin(
            plr => plr != null && !plr.HasDied(),
            plr =>
            {
                menu.ForceClose();

                if (plr == null)
                {
                    Timer = 0.01f;
                    return;
                }

                // Spawn the decoy!
                DecoySystem.RpcSpawnDecoy(localPlayer, plr, localPlayer.transform.position, false);

                Timer = Cooldown;
            }
        );
    }
}
