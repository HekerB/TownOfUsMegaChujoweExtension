using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TownOfUs.Assets;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class VoodooCycleButton : TownOfUsRoleButton<VoodooMasterRole>
{
    public override string Name => "Cycle Curse";
    public override BaseKeybind Keybind => Keybinds.TertiaryAction;
    public override Color TextOutlineColor => Palette.ImpostorRed;
    public override float Cooldown => 0.5f;
    public override LoadableAsset<Sprite> Sprite => TownOfUs.Assets.TouRoleIcons.Herbalist;

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
        var voodooRole = Role;
        if (voodooRole == null) return;

        voodooRole.SelectedEffect = (VoodooEffect)(((int)voodooRole.SelectedEffect + 1) % 3);
        
        var localizedEffect = TouLocale.Get("ExtensionVoodooEffect" + voodooRole.SelectedEffect, voodooRole.SelectedEffect.ToString());
        OverrideName(localizedEffect);
        
        Timer = Cooldown;
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
            var localizedEffect = TouLocale.Get("ExtensionVoodooEffect" + voodooRole.SelectedEffect, voodooRole.SelectedEffect.ToString());
            OverrideName(localizedEffect);
        }
    }
}
