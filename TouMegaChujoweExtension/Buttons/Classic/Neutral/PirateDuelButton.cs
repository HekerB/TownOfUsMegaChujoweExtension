using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Roles;
using MiraAPI.Utilities.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class PirateDuelButton : TownOfUsRoleButton<PirateRole, PlayerControl>
{
    public override string Name => TouLocale.GetParsed("ExtensionRolePirateDuel", "Duel");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<PirateOptions>.Instance.DuelCooldown + MapCooldown, 5f, 120f);
    public override float EffectDuration => 0f;
    public override LoadableAsset<Sprite> Sprite => TouExtensionNeuAssets.PirateDuelButtonSprite;
    public override Color TextOutlineColor => TouExtensionColors.Pirate;

    public override bool Enabled(RoleBehaviour? role)
    {
        return base.Enabled(role)
               && PlayerControl.LocalPlayer != null
               && !PlayerControl.LocalPlayer.Data.IsDead
               && role is PirateRole pirate
               && pirate.CanContinueActing();
    }

    public override PlayerControl? GetTarget()
    {
        var localPlayer = PlayerControl.LocalPlayer;
        var pirateRole = localPlayer?.Data?.Role as PirateRole;

        if (localPlayer == null || pirateRole == null)
        {
            return null;
        }

        if (pirateRole.DuelTargetId != byte.MaxValue)
        {
            return null;
        }

        var closest = localPlayer.GetClosestLivingPlayer(true, Distance);
        if (closest == null)
        {
            return null;
        }

        if (pirateRole.IsBlacklisted(closest.PlayerId))
        {
            return null;
        }

        return closest;
    }

    protected override void OnClick()
    {
        var localPlayer = PlayerControl.LocalPlayer;
        var pirateRole = localPlayer?.Data?.Role as PirateRole;

        if (Target == null || localPlayer == null || pirateRole == null)
        {
            return;
        }

        if (pirateRole.IsBlacklisted(Target.PlayerId))
        {
            return;
        }

        PirateRole.RpcSetDuelTarget(localPlayer, Target.PlayerId);
        ResetCooldownAndOrEffect();
    }
}














