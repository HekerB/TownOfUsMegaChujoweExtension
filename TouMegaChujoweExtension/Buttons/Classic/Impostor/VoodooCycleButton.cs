using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class VoodooCycleButton : TownOfUsRoleButton<VoodooMasterRole>
{
    public override string Name => TouLocale.Get("ExtensionRoleVoodooMasterCycle", "Change");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => Palette.ImpostorRed;
    public override float Cooldown => 0.5f;
    public override LoadableAsset<Sprite> Sprite => TouImpAssets.TraitorSelect;

    private bool _isProcessingClick;

    public override bool CanUse()
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data.IsDead)
        {
            return false;
        }

        if (Minigame.Instance || MeetingHud.Instance || HudManager.Instance.Chat.IsOpenOrOpening)
        {
            return false;
        }

        return Timer <= 0f;
    }

    public override void ClickHandler()
    {
        if (_isProcessingClick)
        {
            return;
        }

        _isProcessingClick = true;

        try
        {
            if (CanUse())
            {
                OnClick();
            }
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
        if (Role == null)
        {
            return;
        }

        Role.SelectedEffect = (VoodooEffect)(((int)Role.SelectedEffect + 1) % 3);
        OverrideName(Name);
        OverrideSprite(Sprite.LoadAsset());
        CustomButtonSingleton<VoodooDollButton>.Instance?.UpdateUsesDisplay();
        Timer = Cooldown;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        var shouldShow = Role != null && !playerControl.HasDied() && !MeetingHud.Instance;

        if (Button != null && Button.gameObject.activeSelf != shouldShow)
        {
            Button.gameObject.SetActive(shouldShow);
        }

        if (shouldShow)
        {
            base.FixedUpdate(playerControl);
            OverrideName(Name);
            OverrideSprite(Sprite.LoadAsset());
        }
    }
}
