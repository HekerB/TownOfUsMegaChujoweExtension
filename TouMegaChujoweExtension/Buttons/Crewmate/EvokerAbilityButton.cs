using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Crewmate;

public sealed class EvokerAbilityButton : TownOfUsRoleButton<EvokerRole>
{
    private PlayerControl? _verifyTarget;
    private PlayerControl? _lastOutlined;
    private bool _wasVerifyMode;

    public override string Name => TouLocale.Get("ExtensionRoleEvokerBlind", "Blind");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Evoker;
    public override LoadableAsset<Sprite> Sprite => TouExtensionCrewAssets.EvokerBlindButtonSprite;

    public override float Cooldown => EvokerSystem.IsBlindActive
        ? Math.Clamp(OptionGroupSingleton<EvokerOptions>.Instance.VerifyCooldown.Value + MapCooldown, 1f, 60f)
        : Math.Clamp(OptionGroupSingleton<EvokerOptions>.Instance.BlindCooldown.Value + MapCooldown, 5f, 120f);

public override bool CanUse()
{
    if (!base.CanUse()) return false;

    var player = PlayerControl.LocalPlayer;
    if (player == null || player.HasDied()) return false;

    if (EvokerSystem.IsBlindActive)
    {
        if (OptionGroupSingleton<EvokerOptions>.Instance.CantVerify.Value) return false;
        var max = (int)OptionGroupSingleton<EvokerOptions>.Instance.MaxVerifications.Value;
        if (max > 0 && Role.VerifiesUsed >= max) return false;
        return _verifyTarget != null;
    }

    return true;
}

    public override void ClickHandler()
    {
        if (!CanClick()) return;
        OnClick();
        Timer = Cooldown;
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        if (!EvokerSystem.IsBlindActive)
        {
            // === BLIND ===
            var duration = OptionGroupSingleton<EvokerOptions>.Instance.BlindDuration.Value;
            EvokerRole.RpcEvokerBlind(player, duration);
            var max = (int)OptionGroupSingleton<EvokerOptions>.Instance.MaxVerifications.Value;
            if (max > 0)
            {
                Button?.SetUsesRemaining(max - Role.VerifiesUsed);
            }
            Timer = 0f; // ready to verify immediately
        }
        else
        {
            // === VERIFY ===
            if (_verifyTarget == null) return;

            var isKiller = EvokerSystem.IsBlindTarget(_verifyTarget);
            var name = _verifyTarget.Data.PlayerName;

            var color = isKiller ? Palette.ImpostorRed : Palette.CrewmateBlue;
            var text = isKiller
                ? $"<b>{Palette.ImpostorRed.ToTextColor()}{name} is a Killing role!</color></b>"
                : $"<b>{Palette.CrewmateBlue.ToTextColor()}{name} is NOT a Killing role.</color></b>";

            Coroutines.Start(MiscUtils.CoFlash(color));
            var notif = Helpers.CreateAndShowNotification(text, Color.white,
                new Vector3(0f, 1f, -20f), spr: TouExtensionIcons.EvokerRoleIcon.LoadAsset());
            notif.AdjustNotification();

            EvokerSystem.AddVerified(_verifyTarget.PlayerId, isKiller);
            EvokerRole.RpcEvokerVerify(player, _verifyTarget.PlayerId);
            var max = (int)OptionGroupSingleton<EvokerOptions>.Instance.MaxVerifications.Value;
            if (max > 0)
            {
                Button?.SetUsesRemaining(max - Role.VerifiesUsed);
            }
        }
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        if (MeetingHud.Instance)
        {
            ClearOutline();
            _verifyTarget = null;
            return;
        }

        var button = Button;
        if (button == null) return;

        button.gameObject.SetActive(
            HudManager.Instance.UseButton.isActiveAndEnabled ||
            HudManager.Instance.PetButton.isActiveAndEnabled);

        var maxVer = (int)OptionGroupSingleton<EvokerOptions>.Instance.MaxVerifications.Value;
        var isVerify = EvokerSystem.IsBlindActive && (maxVer <= 0 || Role.VerifiesUsed < maxVer);

        // --- mode switch ---
        if (isVerify)
        {
            if (!_wasVerifyMode)
            {
                Timer = 0f;
                _wasVerifyMode = true;
            }
            
            var max = (int)OptionGroupSingleton<EvokerOptions>.Instance.MaxVerifications.Value;
            if (max > 0)
            {
                button.SetUsesRemaining(max - Role.VerifiesUsed);
            }
        }
        else if (!isVerify && _wasVerifyMode)
        {
            Timer = Cooldown;
            _wasVerifyMode = false;
            _verifyTarget = null;
            ClearOutline();
            if (button.usesRemainingText != null)
                button.usesRemainingText.gameObject.SetActive(false);
            if (button.usesRemainingSprite != null)
                button.usesRemainingSprite.gameObject.SetActive(false);
        }

        // --- sprite + name ---
        if (isVerify)
        {
            OverrideName(TouLocale.Get("ExtensionRoleEvokerVerify", "Verify"));
            var spr = TouExtensionCrewAssets.EvokerVerifyButtonSprite.LoadAsset();
            if (spr != null && button.graphic != null && button.graphic.sprite != spr)
                button.graphic.sprite = spr;
        }
        else
        {
            OverrideName(TouLocale.Get("ExtensionRoleEvokerBlind", "Blind"));
            var spr = TouExtensionCrewAssets.EvokerBlindButtonSprite.LoadAsset();
            if (spr != null && button.graphic != null && button.graphic.sprite != spr)
                button.graphic.sprite = spr;
        }

        // --- verify target ---
        if (isVerify && !OptionGroupSingleton<EvokerOptions>.Instance.CantVerify.Value)
		{
			ClearOutline();
			_verifyTarget = playerControl.GetClosestLivingPlayer(true, 1.5f, false,
				p => !EvokerSystem.VerifiedPlayers.ContainsKey(p.PlayerId));

            if (_verifyTarget != null && !_verifyTarget.HasDied())
            {
                _verifyTarget.cosmetics.SetOutline(true,
                    new Il2CppSystem.Nullable<Color>(TouExtensionColors.Evoker));
                _lastOutlined = _verifyTarget;
            }
        }
        else
        {
            _verifyTarget = null;
        }
    }

    private void ClearOutline()
    {
        if (_lastOutlined != null)
        {
            _lastOutlined.cosmetics.SetOutline(false, new Il2CppSystem.Nullable<Color>());
            _lastOutlined = null;
        }
    }
}
