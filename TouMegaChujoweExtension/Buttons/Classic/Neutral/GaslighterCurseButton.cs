using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Roles.Neutral;
using MiraAPI.Roles;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class GaslighterCurseButton : TownOfUsRoleButton<GaslighterRole, PlayerControl>
{
    public override string Name => "Curse";
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Sandworm;
    public override float Cooldown => OptionGroupSingleton<GaslighterOptions>.Instance.CurseCooldown;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.SpellButtonSprite;

    public override PlayerControl? GetTarget()
    {
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(false, GameManager.Instance.LogicOptions.GetKillDistance());
    }

    protected override void OnClick()
    {
        if (Target == null) return;
        
        GaslighterRole.RpcGaslighterCurse(PlayerControl.LocalPlayer, Target);
        Timer = Cooldown;
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
            base.FixedUpdate(playerControl);
        }
    }
}
