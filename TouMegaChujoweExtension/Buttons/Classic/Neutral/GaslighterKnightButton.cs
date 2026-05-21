using System;
using System.Linq;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Buttons;
using TownOfUs;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Assets;
using TownOfUs.Roles.Neutral;
using MiraAPI.Roles;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Neutral;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class GaslighterKnightButton : TownOfUsRoleButton<GaslighterRole, PlayerControl>
{
    public override string Name => "Knight";
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Gaslighter;
    public override float Cooldown => OptionGroupSingleton<GaslighterOptions>.Instance.KnightCooldown + MapCooldown;
    public override float EffectDuration => 3f;
    public override int MaxUses => (int)OptionGroupSingleton<GaslighterOptions>.Instance.MaxKnights;
    public override LoadableAsset<Sprite> Sprite => TownOfUs.Assets.TouRoleIcons.Monarch;
    public PlayerControl? _knightedTarget;
    private bool _isProcessingClick;

    public override bool CanUse()
    {
        var gaslighter = Role;
        if (gaslighter == null || gaslighter.CurrentCycleAbility != GaslighterAbility.Knight) return false;

        if (PlayerControl.LocalPlayer.HasDied())
        {
            return false;
        }

        if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance)
        {
            return false;
        }

        if (!PlayerControl.LocalPlayer.CanMove ||
            PlayerControl.LocalPlayer.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities))
        {
            return false;
        }

        var newTarget = GetTarget();
        if (newTarget != Target)
        {
            SetOutline(false);
        }

        Target = IsTargetValid(newTarget) ? newTarget : null;
        SetOutline(true);

        return PlayerControl.LocalPlayer.moveable &&
               (EffectActive || (!EffectActive && Target != null && (!LimitedUses || UsesLeft > 0) && Timer <= 0));
    }

    public override bool CanClick()
    {
        return CanUse();
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
            if (!CanClick() || PlayerControl.LocalPlayer.HasModifier<GlitchHackedModifier>() ||
                PlayerControl.LocalPlayer.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities))
            {
                return;
            }

            OnClick();
        }
        finally
        {
            Coroutines.Start(ResetProcessingFlag());
        }
    }

    private System.Collections.IEnumerator ResetProcessingFlag()
    {
        yield return new WaitForSeconds(0.2f);
        _isProcessingClick = false;
    }

    public override PlayerControl? GetTarget()
    {
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(false, GameManager.Instance.LogicOptions.GetKillDistance(),
            predicate: x => !x.HasModifier<GaslighterKnightedModifier>() && !x.HasModifier<TownOfUs.Modifiers.KnightedModifier>());
    }

    protected override void OnClick()
    {
        if (EffectActive)
        {
            var notif2 = Helpers.CreateAndShowNotification(
                "<b>Knighting has been cancelled.</b>",
                Color.white, new Vector3(0f, 1f, -20f), spr: TouRoleIcons.Monarch.LoadAsset());
            notif2.Text.SetOutlineThickness(0.35f);
            _knightedTarget = null;
            ResetCooldownAndOrEffect();
            EffectActive = false;
            Timer = 0.001f;
            return;
        }

        if (Target == null)
        {
            return;
        }

        OverrideName("Knighting");

        _knightedTarget = Target;
        var notif = Helpers.CreateAndShowNotification(
            $"<b>You chose to knight {_knightedTarget.CachedPlayerData.PlayerName}. They will be knighted in 3 second(s)!</b>",
            Color.white, new Vector3(0f, 1f, -20f), spr: TouRoleIcons.Monarch.LoadAsset());
        notif.Text.SetOutlineThickness(0.35f);

        if (HasEffect)
        {
            EffectActive = true;
            Timer = EffectDuration;
        }
        else
        {
            Timer = Cooldown;
        }
    }

    public override void OnEffectEnd()
    {
        OverrideName("Knight");

        if (_knightedTarget == null) return;

        if (LimitedUses)
        {
            UsesLeft--;
            Button?.SetUsesRemaining(UsesLeft);
            TownOfUsColors.UseBasic = false;
            if (TextOutlineColor != Color.clear)
            {
                SetTextOutline(TextOutlineColor);
                if (Button != null)
                {
                    Button.usesRemainingSprite.color = TextOutlineColor;
                }
            }

            TownOfUsColors.UseBasic = LocalSettingsTabSingleton<TownOfUsLocalRoleSettings>.Instance
                .UseCrewmateTeamColorToggle.Value;
        }

        GaslighterRole.RpcGaslighterKnight(PlayerControl.LocalPlayer, _knightedTarget);
        _knightedTarget = null;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        var gaslighter = Role;
        bool isCorrectAbility = gaslighter != null && gaslighter.CurrentCycleAbility == GaslighterAbility.Knight;
        bool shouldShow = isCorrectAbility && !playerControl.HasDied();
        
        if (Button != null && Button.gameObject.activeSelf != shouldShow)
        {
            Button.gameObject.SetActive(shouldShow);
        }
        
        if (shouldShow)
        {
            base.FixedUpdate(playerControl);
        }
    }
}
