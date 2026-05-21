using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Roles.Neutral;
using MiraAPI.Roles;
using MiraAPI.Modifiers;
using TownOfUs.Modifiers;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class GaslighterShieldButton : TownOfUsRoleButton<GaslighterRole, PlayerControl>
{
    public override string Name => "Shield";
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Gaslighter;
    public override float Cooldown => OptionGroupSingleton<GaslighterOptions>.Instance.ShieldCooldown + MapCooldown;
    public override LoadableAsset<Sprite> Sprite => TownOfUs.Assets.TouRoleIcons.Medic;

    private bool _isProcessingClick;

    public override bool CanUse()
    {
        var gaslighter = Role;
        if (gaslighter == null || gaslighter.CurrentCycleAbility != GaslighterAbility.Shield) return false;
        return base.CanUse();
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
            predicate: x => !x.HasModifier<GaslighterShieldModifier>());
    }

    protected override void OnClick()
    {
        if (Target == null) return;
        
        GaslighterRole.RpcGaslighterShield(PlayerControl.LocalPlayer, Target);
        Timer = Cooldown;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        var gaslighter = Role;
        bool isCorrectAbility = gaslighter != null && gaslighter.CurrentCycleAbility == GaslighterAbility.Shield;
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
