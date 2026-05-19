using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Assets;
using TownOfUs.Utilities;
using UnityEngine;
using System;

namespace TouMegaChujoweExtension.Buttons.Crewmate;

public sealed class DoctorInjectButton : TownOfUsRoleButton<DoctorRole, PlayerControl>
{
    private bool _isInjecting;
    private float _injectTimer;
    private float _injectDuration;

    public override string Name => TouLocale.Get("ExtensionRoleDoctorInject", "Inject");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Doctor;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<DoctorOptions>.Instance.InjectCooldown + MapCooldown, 5f, 120f);
    public override LoadableAsset<Sprite> Sprite => TouExtensionCrewAssets.DoctorInjectButtonSprite;
    public override int MaxUses => (int)OptionGroupSingleton<DoctorOptions>.Instance.InitialUses;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        if (Button != null)
        {
            Button.usesRemainingSprite.sprite = TouAssets.AbilityCounterBasicSprite.LoadAsset();
            Button.usesRemainingSprite.gameObject.SetActive(MaxUses > 0);
            if (Button.usesRemainingText != null)
            {
                Button.usesRemainingText.gameObject.SetActive(MaxUses > 0);
            }
        }
    }

    public override bool ZeroIsInfinite { get; set; } = true;

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (_isInjecting) return false;

        if (!base.IsTargetValid(target) || target == null)
        {
            return false;
        }

        return target != PlayerControl.LocalPlayer;
    }

    public override PlayerControl? GetTarget()
    {
        if (_isInjecting) return null;
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance);
    }

    public override bool CanUse()
    {
        if (_isInjecting) return true;
        return base.CanUse();
    }

    public override bool CanClick()
    {
        if (_isInjecting) return false;
        return base.CanClick();
    }

    public override void ClickHandler()
    {
        if (!CanClick()) return;

        var player = PlayerControl.LocalPlayer;
        if (player == null || Target == null) return;

        var delay = OptionGroupSingleton<DoctorOptions>.Instance.EffectDelay;
        if (delay > 0f)
        {
            OnClick();
            _injectDuration = delay;
            _injectTimer = delay;
            _isInjecting = true;

            if (LimitedUses && !(ZeroIsInfinite && MaxUses == 0))
            {
                UsesLeft--;
                SetUses(UsesLeft);
            }

            OverrideName(TouLocale.GetParsed("ExtensionRoleDoctorInjecting", "Injecting..."));
            Button?.SetCooldownFill(1f);
        }
        else
        {
            base.ClickHandler();
        }
    }

    protected override void OnClick()
    {
        if (Target == null)
        {
            Error("Doctor Inject: Target is null");
            return;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            Error("Doctor Inject: LocalPlayer is null");
            return;
        }

        int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        DoctorRole.RpcDoctorInject(player, Target, seed);
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        if (MeetingHud.Instance)
        {
            if (_isInjecting) EndInjectWindow();
            base.FixedUpdate(playerControl);
            UpdateUsesDisplay();
            return;
        }

        if (playerControl == null || !playerControl.IsRole<DoctorRole>())
        {
            if (_isInjecting) EndInjectWindow();
            if (playerControl != null)
            {
                base.FixedUpdate(playerControl);
                UpdateUsesDisplay();
            }
            return;
        }

        if (_isInjecting)
        {
            _injectTimer -= Time.fixedDeltaTime;

            if (_injectTimer <= 0f)
            {
                EndInjectWindow();
                Timer = Cooldown;
                OverrideName(TouLocale.Get("ExtensionRoleDoctorInject", "Inject"));
            }
            else
            {
                Timer = -1f;

                if (Button != null)
                {
                    Button.SetEnabled();
                    Button.SetFillUp(_injectTimer, _injectDuration);
                    Button.cooldownTimerText.text = Mathf.CeilToInt(_injectTimer).ToString();
                    Button.cooldownTimerText.gameObject.SetActive(true);
                }
            }
            UpdateUsesDisplay();
            return;
        }

        base.FixedUpdate(playerControl);
        UpdateUsesDisplay();
    }

    public override void SetUses(int amount)
    {
        base.SetUses(amount);
        UpdateUsesDisplay();
    }

    private void UpdateUsesDisplay()
    {
        if (Button != null)
        {
            if (ZeroIsInfinite && MaxUses == 0)
            {
                Button.usesRemainingSprite.gameObject.SetActive(false);
                if (Button.usesRemainingText != null)
                {
                    Button.usesRemainingText.gameObject.SetActive(false);
                }
            }
            else
            {
                Button.usesRemainingSprite.gameObject.SetActive(MaxUses > 0);
                if (Button.usesRemainingText != null)
                {
                    Button.usesRemainingText.gameObject.SetActive(MaxUses > 0);
                }
            }
        }
    }

    private void EndInjectWindow()
    {
        _isInjecting = false;
        _injectTimer = 0f;

        Button?.SetCooldownFill(0f);
        if (Button != null)
            Button.cooldownTimerText.gameObject.SetActive(false);
    }

    public override void ResetCooldownAndOrEffect()
    {
        EndInjectWindow();
        OverrideName(TouLocale.Get("ExtensionRoleDoctorInject", "Inject"));
        base.ResetCooldownAndOrEffect();
    }
}
