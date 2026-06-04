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

        var closest = localPlayer.GetClosestLivingPlayer(true, Distance);
        if (closest == null)
        {
            return null;
        }

        // Don't highlight the player already challenged this round
        if (pirateRole.DuelTargetId != byte.MaxValue && closest.PlayerId == pirateRole.DuelTargetId)
        {
            return null;
        }

        if (pirateRole.IsBlacklisted(closest.PlayerId))
        {
            return null;
        }

        return closest;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);
        SetButtonState(GetTarget() != null && !playerControl.HasDied());
    }

    private void SetButtonState(bool shouldBeBright)
    {
        if (Button == null) return;

        if (Button.cooldownTimerText != null && Button.cooldownTimerText.gameObject.activeSelf)
        {
            Button.cooldownTimerText.color = Color.white;
        }

        if (Button.buttonLabelText != null)
        {
            Button.buttonLabelText.color = shouldBeBright ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        }

        if (Button.graphic != null)
        {
            Button.graphic.color = new Color(1f, 1f, 1f, shouldBeBright ? 1f : 0.5f);
            if (Button.graphic.material != null)
                Button.graphic.material.SetFloat("_Desat", shouldBeBright ? 0f : 1f);
        }
    }

    protected override void OnClick()
    {
        var localPlayer = PlayerControl.LocalPlayer;
        var pirateRole = localPlayer?.Data?.Role as PirateRole;

        if (Target == null || localPlayer == null || pirateRole == null)
        {
            return;
        }

        // Only block RPC from firing again — button can still highlight before this
        if (pirateRole.DuelTargetId != byte.MaxValue)
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














