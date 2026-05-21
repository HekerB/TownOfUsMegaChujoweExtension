using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using UnityEngine;
using TownOfUs.Modules.Localization;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class SniperButton : TownOfUsRoleButton<SniperRole>
{
    public static SniperButton? Instance { get; private set; }

    public SniperButton()
    {
        Instance = this;
    }

    public override string Name => TouLocale.Get("ExtensionRoleSniperSnipe", "Scope");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Detonator;
    public override float Cooldown
    {
        get
        {
            var cooldown = OptionGroupSingleton<SniperOptions>.Instance?.Cooldown ?? 30f;
            return cooldown <= 0f ? 30f : cooldown;
        }
    }
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.SniperButtonSprite;
    public override int MaxUses => 0;

    public override bool CanUse()
    {
        if (MeetingHud.Instance || ExileController.Instance) return false;
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.HasDied()) return false;
        
        var role = PlayerControl.LocalPlayer.GetRole<SniperRole>();
        if (role == null) return false;

        // Can click if not in vent and either the cooldown is done OR scope is already active (to allow manual toggle off)
        return !PlayerControl.LocalPlayer.inVent && (Timer <= 0f || role.IsScopeActive);
    }

    public override bool CanClick()
    {
        return CanUse();
    }

    private bool _isProcessingClick;

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
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        var role = player.GetRole<SniperRole>();
        if (role == null) return;

        if (role.IsScopeActive)
        {
            role.DeactivateScope();
            Timer = Cooldown;
        }
        else
        {
            role.ActivateScope();
        }
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        if (MeetingHud.Instance || ExileController.Instance)
        {
            base.FixedUpdate(playerControl);
            return;
        }

        var role = playerControl.GetRole<SniperRole>();
        if (role != null && role.IsScopeActive)
        {
            float remaining = Mathf.Max(0f, role.ScopeTimer);
            Timer = -1f;

            if (Button != null)
            {
                var shootWindow = OptionGroupSingleton<SniperOptions>.Instance?.ShootWindow ?? 5f;
                if (shootWindow <= 0f) shootWindow = 5f;

                Button.SetEnabled();
                Button.SetFillUp(remaining, shootWindow);
                Button.cooldownTimerText.text = Mathf.CeilToInt(remaining).ToString();
                Button.cooldownTimerText.gameObject.SetActive(true);
            }
            return;
        }

        base.FixedUpdate(playerControl);
    }
}
