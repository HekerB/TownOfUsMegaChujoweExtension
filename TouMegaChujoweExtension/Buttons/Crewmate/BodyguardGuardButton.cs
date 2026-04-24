using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Crewmate;

public sealed class BodyguardGuardButton : TownOfUsRoleButton<BodyguardRole, PlayerControl>
{
    private enum Stage
    {
        Guard,
        Backlash
    }

    private Stage _stage = Stage.Guard;

    private float _backlashEndTime = -1f;
    private bool _prevBacklashReady;
    private float BacklashWindow => OptionGroupSingleton<BodyguardOptions>.Instance.BacklashWindow;
    private bool IsInBacklashWindow => Time.time < _backlashEndTime;

    public override string Name => TouLocale.GetParsed("ExtensionRoleBodyguardGuard", "Guard");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Bodyguard;
    public override float Cooldown => Math.Clamp(MapCooldown, 0.001f, 120f);
	public override float InitialCooldown => 0.001f;

    public override LoadableAsset<Sprite> Sprite => TouExtensionCrewAssets.GuardButtonSprite;

    public override bool Enabled(RoleBehaviour? role)
    {
        if (role is not BodyguardRole bgRole) return false;
        if (bgRole.KillModeActive) return false;
        return base.Enabled(role);
    }

    public override bool CanUse()
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.HasDied()) return false;

        if (_stage == Stage.Backlash)
        {
            return IsInBacklashWindow;
        }

        // Like Warden: can only use when no one is guarded
        return base.CanUse() && Role is { Guarded: null };
    }

    public override bool CanClick()
    {
        if (_stage == Stage.Backlash && IsInBacklashWindow)
        {
            return true;
        }
        return base.CanClick();
    }

    public override void ClickHandler()
    {
        if (_stage == Stage.Backlash && IsInBacklashWindow)
        {
            Info("[BG-Button] ClickHandler: Backlash click detected!");
            OnClick();
            return;
        }
        base.ClickHandler();
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (_stage == Stage.Backlash) return true;
        return base.IsTargetValid(target);
    }

    public override PlayerControl? GetTarget()
    {
        if (_stage == Stage.Backlash)
        {
            return PlayerControl.LocalPlayer;
        }

        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance);
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (Role == null) return;

        var currentBacklashReady = Role.BacklashReady;

        // Detect RISING EDGE of BacklashReady
        if (currentBacklashReady && !_prevBacklashReady)
        {
            Info($"[BG-Button] Backlash TRIGGERED! Starting own timer for {BacklashWindow}s");
            _backlashEndTime = Time.time + BacklashWindow;

            if (_stage != Stage.Backlash)
            {
                _stage = Stage.Backlash;
                OverrideName(TouLocale.GetParsed("ExtensionRoleBodyguardBacklash", "Backlash"));
                if (Button?.graphic != null)
                {
                    Button.graphic.sprite = TouExtensionCrewAssets.BacklashButtonSprite.LoadAsset();
                }
            }
            Timer = -1f;
        }
        _prevBacklashReady = currentBacklashReady;

        // Check backlash expiry using OWN timer
        if (_stage == Stage.Backlash && !IsInBacklashWindow)
        {
            Info("[BG-Button] Backlash window expired (own timer)");
            _stage = Stage.Guard;
            _backlashEndTime = -1f;
            OverrideName(TouLocale.GetParsed("ExtensionRoleBodyguardGuard", "Guard"));
            if (Button?.graphic != null)
            {
                Button.graphic.sprite = TouExtensionCrewAssets.GuardButtonSprite.LoadAsset();
            }
            Timer = Cooldown;
        }

        // Show backlash countdown on button + force enable
        if (_stage == Stage.Backlash && IsInBacklashWindow && Button != null)
        {
            Button.SetEnabled();

            var remaining = _backlashEndTime - Time.time;
            if (remaining > 0f)
            {
                try
                {
                    Button.SetFillUp(remaining, BacklashWindow);
                    Button.cooldownTimerText.text = Mathf.Ceil(remaining)
                        .ToString("F0", System.Globalization.CultureInfo.InvariantCulture);
                    Button.cooldownTimerText.gameObject.SetActive(true);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }

    protected override void OnClick()
    {
        if (Role == null) return;

        // === BACKLASH ===
        if (_stage == Stage.Backlash && IsInBacklashWindow)
        {
            Info("[BG-Button] Backlash CLICKED! Activating kill mode.");
            BodyguardRole.RpcBodyguardBacklash(PlayerControl.LocalPlayer);

            _stage = Stage.Guard;
            _backlashEndTime = -1f;
            OverrideName(TouLocale.GetParsed("ExtensionRoleBodyguardGuard", "Guard"));
            if (Button?.graphic != null)
            {
                Button.graphic.sprite = TouExtensionCrewAssets.GuardButtonSprite.LoadAsset();
            }
            Timer = Cooldown;
            return;
        }

        // === GUARD ===
        if (Target == null || Target.PlayerId == PlayerControl.LocalPlayer.PlayerId)
        {
            return;
        }

        BodyguardRole.RpcBodyguardGuard(PlayerControl.LocalPlayer, Target);
        // No cooldown needed - button will be disabled until guarded dies
    }
}
