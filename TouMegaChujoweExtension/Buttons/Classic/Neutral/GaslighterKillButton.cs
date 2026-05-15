using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Roles.Neutral;
using MiraAPI.Roles;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TownOfUs.Networking;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class GaslighterKillButton : TownOfUsKillRoleButton<GaslighterRole, PlayerControl>, IKillButton
{
    public override string Name => "Kill";
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Sandworm; // Placeholder until I update colors
    public override float Cooldown => OptionGroupSingleton<GaslighterOptions>.Instance.KillCooldown;
    public override LoadableAsset<Sprite> Sprite => TownOfUs.Assets.TouAssets.KillSprite;

    public override PlayerControl? GetTarget()
    {
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, GameManager.Instance.LogicOptions.GetKillDistance());
    }

    protected override void OnClick()
    {
        if (Target == null) return;
        
        PlayerControl.LocalPlayer.RpcMurderPlayer(Target, true);
        Timer = Cooldown;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        var gaslighter = Role;
        bool isCorrectAbility = gaslighter != null && gaslighter.CurrentCycleAbility == GaslighterAbility.Kill;
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
