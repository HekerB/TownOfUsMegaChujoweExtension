using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using System;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class InjectorInjectButton : TownOfUsKillRoleButton<InjectorRole, PlayerControl>, IDiseaseableButton
{
    private bool _isInjecting;
    private float _injectTimer;
    private float _injectDuration;

    public override string Name => TouLocale.Get("ExtensionRoleInjectorInject", "Inject");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Injector;
    public override float Cooldown
    {
        get
        {
            var baseKc = GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown;
            var multiplier = PlayerControl.LocalPlayer != null && baseKc > 0 
                ? PlayerControl.LocalPlayer.GetKillCooldown() / baseKc 
                : 1f;
            return Math.Clamp((OptionGroupSingleton<InjectorOptions>.Instance.InjectCooldown + MapCooldown) * multiplier, 5f, 120f);
        }
    }

    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.InjectorInjectButtonSprite;
    public override int MaxUses => (int)OptionGroupSingleton<InjectorOptions>.Instance.InitialUses;

    public override bool ZeroIsInfinite { get; set; } = true;

    public void SetDiseasedTimer(float multiplier)
    {
        SetTimer(Cooldown * multiplier);
    }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        if (Button != null)
        {
            Button.usesRemainingSprite.gameObject.SetActive(MaxUses > 0);
            if (Button.usesRemainingText != null)
            {
                Button.usesRemainingText.gameObject.SetActive(MaxUses > 0);
            }
        }
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (_isInjecting) return false;

        if (!base.IsTargetValid(target) || target == null)
        {
            return false;
        }

        return CanInjectTarget(target);
    }

    private static bool CanInjectTarget(PlayerControl? target)
    {
        if (target == null)
        {
            return false;
        }

        if (target.IsImpostor())
        {
            return false;
        }

        if (target.HasModifier<FirstDeadShield>())
        {
            return false;
        }

        return true;
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
        if (Target == null) return;

        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        var beforeMurderEvent = new BeforeMurderEvent(player, Target, MeetingCheck.OutsideMeeting);
        MiraEventManager.InvokeEvent(beforeMurderEvent);
        
        if (beforeMurderEvent.IsCancelled)
        {
            return;
        }

        var delay = OptionGroupSingleton<InjectorOptions>.Instance.EffectDelay;
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

            OverrideName(TouLocale.GetParsed("ExtensionRoleInjectorInjecting", "Injecting"));
            Button?.SetCooldownFill(1f);
        }
        else
        {
            OnClick();
            Timer = Cooldown;
        }
    }

    protected override void OnClick()
    {
        if (Target == null)
        {
            Error("Injector Inject: Target is null");
            return;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            Error("Injector Inject: LocalPlayer is null");
            return;
        }

        var seed = UnityEngine.Random.RandomRange(int.MinValue, int.MaxValue);
        InjectorRole.RpcInjectorInject(player, Target, seed);
        
        if (OptionGroupSingleton<InjectorOptions>.Instance.SharedCooldown)
        {
            player.SetKillTimer(player.GetKillCooldown());
        }
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        UpdateUsesDisplay();

        if (MeetingHud.Instance)
        {
            if (_isInjecting) EndInjectWindow();
            base.FixedUpdate(playerControl);
            return;
        }

        if (playerControl == null || !playerControl.IsRole<InjectorRole>())
        {
            if (_isInjecting) EndInjectWindow();
            if (playerControl != null) base.FixedUpdate(playerControl);
            return;
        }

        if (_isInjecting)
        {
            _injectTimer -= Time.fixedDeltaTime;

            if (_injectTimer <= 0f)
            {
                EndInjectWindow();
                Timer = Cooldown;
                OverrideName(TouLocale.Get("ExtensionRoleInjectorInject", "Inject"));
            }
            else
            {
                Timer = -1f;

                if (Button != null)
                {
                    Button.SetEnabled();
                    Button.SetFillUp(_injectTimer, _injectDuration);
                    var format = _injectTimer <= 10f && MiraAPI.LocalSettings.LocalSettingsTabSingleton<TownOfUs.TownOfUsLocalSettings>.Instance.PreciseCooldownsToggle.Value
                        ? "0.0"
                        : "0";
                    Button.cooldownTimerText.text = _injectTimer.ToString(format, System.Globalization.NumberFormatInfo.InvariantInfo);
                    Button.cooldownTimerText.gameObject.SetActive(true);
                }
            }
            return;
        }

        base.FixedUpdate(playerControl);
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
        OverrideName(TouLocale.Get("ExtensionRoleInjectorInject", "Inject"));
        base.ResetCooldownAndOrEffect();
    }
}
