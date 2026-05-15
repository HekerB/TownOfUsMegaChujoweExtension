using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TownOfUs.Assets;
using TownOfUs.Modules;
using MiraAPI.Hud;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Crewmate;

public sealed class ConfuserConfuseButton : TownOfUsRoleButton<ConfuserRole>
{
    public override string Name => "Confuse";
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Confuser;
    public override float Cooldown => OptionGroupSingleton<ConfuserOptions>.Instance.ConfuseCooldown;
    public override LoadableAsset<Sprite> Sprite => TownOfUs.Assets.TouRoleIcons.Herbalist;

    public PlayerControl? Target { get; private set; }

    public override bool CanUse()
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data.IsDead) return false;
        if (Minigame.Instance) return false;
        if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance) return false;
        
        return Timer <= 0f;
    }

    protected override void OnClick()
    {
        var playerMenu = CustomPlayerMenu.Create();
        playerMenu.Begin(
            plr => plr != null && !plr.HasDied() && plr.PlayerId != PlayerControl.LocalPlayer.PlayerId,
            plr =>
            {
                playerMenu.ForceClose();
                if (plr == null) return;

                ConfuserRole.RpcConfuse(PlayerControl.LocalPlayer, plr);
                Timer = Cooldown;
            });
    }
}
