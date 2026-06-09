using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
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

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class VoodooDollButton : TownOfUsRoleButton<VoodooMasterRole>
{
    public override string Name => "Voodoo Doll";
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => Palette.ImpostorRed;
    public override float Cooldown => OptionGroupSingleton<VoodooMasterOptions>.Instance.Cooldown;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.SpellButtonSprite;

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
            plr => plr != null && !plr.HasDied() && plr.PlayerId != PlayerControl.LocalPlayer.PlayerId,
            plr =>
            {
                playerMenu.ForceClose();
                if (plr == null)
                {
                    Timer = 0.01f;
                    return;
                }

                var voodooRole = Role;
                if (voodooRole != null)
                {
                    VoodooMasterRole.RpcVoodooDollCast(PlayerControl.LocalPlayer, plr, voodooRole.SelectedEffect);
                    Timer = Cooldown;
                }
            });
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        var voodooRole = Role;
        bool shouldShow = voodooRole != null && !playerControl.HasDied();

        if (Button != null && Button.gameObject.activeSelf != shouldShow)
        {
            Button.gameObject.SetActive(shouldShow);
        }

        if (shouldShow)
        {
            base.FixedUpdate(playerControl);
        }
    }
}
