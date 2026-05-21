using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using System.Linq;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class TomahawkThrowButton : TownOfUsRoleButton<TomahawkRole>
{
    public static TomahawkThrowButton? Instance { get; private set; }

    public TomahawkThrowButton()
    {
        Instance = this;
    }

    public override string Name => TouLocale.Get("ExtensionRoleTomahawkThrow", "Throw");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction; // This is F
    public override float Cooldown => OptionGroupSingleton<TomahawkOptions>.Instance.Cooldown;
    public override LoadableAsset<Sprite> Sprite => TouExtensionIcons.TomahawkRoleIcon;

    public override bool CanUse()
    {
        if (MeetingHud.Instance || ExileController.Instance) return false;
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data.IsDead) return false;
        
        var role = player.GetRole<TomahawkRole>();
        return !player.inVent && (Timer <= 0f || (role != null && role.IsAiming));
    }

    public override bool CanClick()
    {
        return CanUse();
    }

    private bool _isProcessingClick;

    public override void ClickHandler()
    {
        Info($"[Tomahawk] ClickHandler! CanUse: {CanUse()}");
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
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        var role = player.GetRole<TomahawkRole>();
        if (role != null)
        {
            if (role.IsAiming)
            {
                role.DeactivateAim();
                Timer = Cooldown;
            }
            else
            {
                role.ActivateAim();
            }
        }
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        if (MeetingHud.Instance || ExileController.Instance)
        {
            base.FixedUpdate(playerControl);
            return;
        }

        var role = playerControl.GetRole<TomahawkRole>();
        if (role != null && role.IsAiming)
        {
            float remaining = Mathf.Max(0f, role.AimTimer);
            Timer = -1f;

            if (Button != null)
            {
                Button.SetEnabled();
                Button.SetFillUp(remaining, 5f);
                Button.cooldownTimerText.text = Mathf.CeilToInt(remaining).ToString();
                Button.cooldownTimerText.gameObject.SetActive(true);
            }
            return;
        }

        base.FixedUpdate(playerControl);
    }
}
