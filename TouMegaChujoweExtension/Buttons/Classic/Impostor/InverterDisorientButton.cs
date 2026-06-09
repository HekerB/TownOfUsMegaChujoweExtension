using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TownOfUs.Assets;
using TownOfUs.Modules;
using MiraAPI.Hud;
using MiraAPI.Utilities;
using MiraAPI.Modifiers;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using UnityEngine;
using System.Linq;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class InverterDisorientButton : TownOfUsRoleButton<InverterRole>
{
    public override string Name => TouLocale.Get("ExtensionRoleInverterDisorient", "Disorient");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => Palette.ImpostorRed;
    public override float Cooldown => OptionGroupSingleton<InverterOptions>.Instance.DisorientCooldown;
    public override LoadableAsset<Sprite> Sprite => TouRoleIcons.Hypnotist;

    private bool _isProcessingClick;

    public override bool CanUse()
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data.IsDead) return false;
        if (Minigame.Instance) return false;
        if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance) return false;

        return Timer <= 0f;
    }

    public override void ClickHandler()
    {
        if (_isProcessingClick) return;
        _isProcessingClick = true;

        try
        {
            if (!CanUse()) return;
            if (PlayerControl.LocalPlayer.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities)) return;
            OnClick();
        }
        finally
        {
            Reactor.Utilities.Coroutines.Start(ResetProcessingFlag());
        }
    }

    private System.Collections.IEnumerator ResetProcessingFlag()
    {
        yield return new WaitForSeconds(0.2f);
        _isProcessingClick = false;
    }

    protected override void OnClick()
    {
        var playerMenu = CustomPlayerMenu.Create();
        playerMenu.Begin(
            plr => plr != null && !plr.HasDied() && plr.PlayerId != PlayerControl.LocalPlayer.PlayerId
                   && !plr.IsImpostorAligned(),
            plr =>
            {
                playerMenu.ForceClose();
                if (plr == null)
                {
                    Timer = 0.01f;
                    return;
                }

                InverterRole.RpcDisorient(PlayerControl.LocalPlayer, plr);
                Timer = Cooldown;
            });
    }
}
