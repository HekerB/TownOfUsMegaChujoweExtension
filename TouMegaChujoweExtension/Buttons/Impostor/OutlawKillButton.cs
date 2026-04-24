using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Networking;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modifiers;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Impostor;

public sealed class OutlawKillButton : TownOfUsKillRoleButton<OutlawRole, PlayerControl>, IDiseaseableButton, IKillButton
{
    private string _killName = "Kill";
    private string _bonusKill = "Bonus Kill";

    public override string Name => _killName;
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Outlaw;
    public override float Cooldown => OptionGroupSingleton<OutlawOptions>.Instance.KillCooldown;
    public override float EffectDuration => 0f;
    public override bool HasEffect => false;
    public override LoadableAsset<Sprite> Sprite => TouAssets.KillSprite;
    public override bool ZeroIsInfinite { get; set; } = true;

    private int _bonusKillsRemaining;
    private bool _inDoubleKillWindow;
    private float _windowTimer;

    private int MaxBonusKills => (int)OptionGroupSingleton<OutlawOptions>.Instance.BonusKills;
    private float WindowDuration => OptionGroupSingleton<OutlawOptions>.Instance.DoubleKillWindow;

    public void SetDiseasedTimer(float multiplier) => SetTimer(Cooldown * multiplier);

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        _killName = TranslationController.Instance.GetStringWithDefault(StringNames.KillLabel, "Kill");
        _bonusKill = "Bonus Kill";
        OverrideName(_killName);
    }

    public override PlayerControl? GetTarget() => PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance);

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (!base.IsTargetValid(target) || target == null) return false;

        var player = PlayerControl.LocalPlayer;
        if (player == null || (player.IsImpostorAligned() && target.IsImpostorAligned())) return false;

        var hasNonBodyguardShield = target.GetModifiers<BaseShieldModifier>().Any(x => x is not BodyguardShieldModifier);
        if (hasNonBodyguardShield || target.HasModifier<FirstDeadShield>()) return false;

        return true;
    }

    public override bool CanUse()
    {
        if (MeetingHud.Instance || HudManager.Instance.Chat.IsOpenOrOpening) return false;
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied() || !player.CanMove) return false;

        if (_inDoubleKillWindow) return Target != null;

        return base.CanUse();
    }

    public override bool CanClick() => CanUse() && (_inDoubleKillWindow ? Target != null : Timer <= 0f && Target != null);

    protected override void OnClick() { }

    public override void ClickHandler()
    {
        if (!CanClick() || Target == null) return;
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        if (LimitedUses && !_inDoubleKillWindow)
        {
            UsesLeft--;
            Button?.SetUsesRemaining(UsesLeft);
        }

        player.RpcCustomMurder(Target);
        
        if (!_inDoubleKillWindow) 
        {
            Timer = Cooldown;
        }
        else
        {
            // Set a tiny cooldown to prevent double clicking the same target or instant spam
            // but keep it small enough that it doesn't break the "instant" feel.
            Timer = 0.1f; 
        }
    }

    public void HandleSuccessfulKill()
    {
        if (_inDoubleKillWindow)
        {
            _bonusKillsRemaining--;
            if (_bonusKillsRemaining <= 0)
            {
                ResetState();
                Timer = Cooldown;
            }
        }
        else if (MaxBonusKills > 0)
        {
            _inDoubleKillWindow = true;
            _bonusKillsRemaining = MaxBonusKills;
            _windowTimer = WindowDuration;
            Timer = 0f;
            PlayerControl.LocalPlayer.SetKillTimer(0f);
        }
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        if (MeetingHud.Instance) return;

        Button?.gameObject.SetActive(HudManager.Instance.UseButton.isActiveAndEnabled || HudManager.Instance.PetButton.isActiveAndEnabled);

        if (_inDoubleKillWindow)
        {
            _windowTimer -= Time.fixedDeltaTime;
            
            if (_windowTimer <= 0f)
            {
                ResetState();
                Timer = Cooldown;
            }
            else
            {
                Timer = Mathf.Min(Timer, 0f);
                playerControl.SetKillTimer(0f);

                var fill = Mathf.Clamp(_windowTimer / WindowDuration, 0f, 1f);
                Button?.SetCooldownFill(fill);

                if (Button != null)
                {
                    Button.cooldownTimerText.text = Mathf.CeilToInt(_windowTimer).ToString();
                    Button.cooldownTimerText.gameObject.SetActive(true);
                }

                Button?.usesRemainingText.gameObject.SetActive(true);
                Button?.usesRemainingSprite.gameObject.SetActive(true);
                Button!.usesRemainingText.text = _bonusKillsRemaining + "x";

                OverrideName(_bonusKill);
            }
        }
        else
        {
            if (MaxBonusKills > 0)
            {
                Button?.usesRemainingText.gameObject.SetActive(true);
                Button?.usesRemainingSprite.gameObject.SetActive(true);
                Button!.usesRemainingText.text = (1 + MaxBonusKills) + "x";
            }
            OverrideName(_killName);
        }

        base.FixedUpdate(playerControl);
    }

    public void ResetState()
    {
        _inDoubleKillWindow = false;
        _bonusKillsRemaining = 0;
        _windowTimer = 0f;
        Button?.usesRemainingText.gameObject.SetActive(false);
        Button?.usesRemainingSprite.gameObject.SetActive(false);
        OverrideName(_killName);
    }

    public override void ResetCooldownAndOrEffect()
    {
        ResetState();
        base.ResetCooldownAndOrEffect();
    }
}