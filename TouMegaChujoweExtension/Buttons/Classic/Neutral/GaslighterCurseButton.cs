using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Modifiers;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class GaslighterCurseButton : TownOfUsKillRoleButton<GaslighterRole, PlayerControl>, IDiseaseableButton, IKillButton
{
    private float _spellProgress;
    private PlayerControl? _spellTarget;
    private float _spellStartTime;
    private bool _isProcessingClick;

    public override string Name => "Curse";
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Gaslighter;
    public override float Cooldown => OptionGroupSingleton<GaslighterOptions>.Instance.CurseCooldown + MapCooldown;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.SpellButtonSprite;

    public override bool ZeroIsInfinite { get; set; } = true;

    public override bool CanUse()
    {
        var gaslighter = Role;
        if (gaslighter == null || gaslighter.CurrentCycleAbility != GaslighterAbility.Curse) return false;
        return base.CanUse();
    }

    public void SetDiseasedTimer(float multiplier)
    {
        SetTimer(Cooldown * multiplier);
    }

    public override void ClickHandler()
    {
        if (_isProcessingClick) return;
        _isProcessingClick = true;

        try
        {
            if (!CanUse() || PlayerControl.LocalPlayer == null ||
                PlayerControl.LocalPlayer.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities))
            {
                return;
            }
            OnClick();
        }
        finally
        {
            Reactor.Utilities.Coroutines.Start(ResetProcessingFlag());
        }
    }

    private IEnumerator ResetProcessingFlag()
    {
        yield return new WaitForSeconds(0.2f);
        _isProcessingClick = false;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        var gaslighter = Role;
        bool isCorrectAbility = gaslighter != null && gaslighter.CurrentCycleAbility == GaslighterAbility.Curse;
        bool shouldShow = isCorrectAbility && !playerControl.HasDied();

        if (Button != null && Button.gameObject.activeSelf != shouldShow)
        {
            Button.gameObject.SetActive(shouldShow);
        }

        if (shouldShow)
        {
            var options = OptionGroupSingleton<GaslighterOptions>.Instance;
            var player = PlayerControl.LocalPlayer;

            if (_spellTarget != null && !_spellTarget.HasDied() && player != null)
            {
                var distance = Vector2.Distance(player.GetTruePosition(), _spellTarget.GetTruePosition());
                if (distance <= Distance && Timer <= 0)
                {
                    var elapsed = Time.time - _spellStartTime;
                    _spellProgress = Mathf.Clamp01(elapsed / options.CurseCastingDuration);

                    if (_spellProgress >= 1f)
                    {
                        GaslighterRole.RpcGaslighterCurse(player, _spellTarget);
                        _spellTarget = null;
                        _spellProgress = 0f;
                        SetTimer(Cooldown);
                    }
                }
                else
                {
                    _spellTarget = null;
                    _spellProgress = 0f;
                }
            }
            else if (_spellTarget != null && (_spellTarget.HasDied() || _spellTarget == null))
            {
                _spellTarget = null;
                _spellProgress = 0f;
            }

            if (_spellTarget != null && _spellProgress > 0f)
            {
                Button?.SetCooldownFill(1f - _spellProgress);
            }

            base.FixedUpdate(playerControl);
        }
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (!base.IsTargetValid(target) || target == null)
        {
            return false;
        }

        if (target.HasModifier<GaslighterCursedModifier>())
        {
            return false;
        }

        if (target.IsImpostor())
        {
            return false;
        }

        return true;
    }

    public override PlayerControl? GetTarget()
    {
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance);
    }

    protected override void OnClick()
    {
        if (Target == null) return;
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        if (_spellTarget == null || _spellTarget.PlayerId != Target.PlayerId)
        {
            _spellTarget = Target;
            _spellStartTime = Time.time;
            _spellProgress = 0f;
        }
    }
}
