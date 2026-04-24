using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class ShroudAbilityButton : TownOfUsRoleButton<ShroudRole, PlayerControl>
{
    public override string Name => "Shroud";
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Shroud;
    public override float Cooldown => OptionGroupSingleton<ShroudOptions>.Instance.ShroudCooldown;
    public override LoadableAsset<Sprite> Sprite => TouExtensionNeuAssets.ShroudAbilitySprite;

    public override PlayerControl? GetTarget()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null) return null;
        return player.GetClosestLivingPlayer(true, Distance);
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (target == null) return false;
        if (target.HasDied()) return false;
        if (target == PlayerControl.LocalPlayer) return false;

        if (target.TryGetModifier<ShroudedModifier>(out var mod) && mod.ShroudOwnerId == PlayerControl.LocalPlayer.PlayerId)
            return false;

        return base.IsTargetValid(target);
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || Target == null) return;

        if (Target.TryGetModifier<ShroudedModifier>(out var existingMod) && existingMod.ShroudOwnerId == player.PlayerId)
            return;

        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p != null && p.TryGetModifier<ShroudedModifier>(out var mod) && mod.ShroudOwnerId == player.PlayerId)
            {
                p.RpcRemoveModifier<ShroudedModifier>();
            }
        }

        Target.RpcAddModifier<ShroudedModifier>(player);

        ShroudKillButton.SetOwnCooldown();
        Timer = Cooldown;
    }

    public static void SetOwnCooldown()
    {
        var instance = CustomButtonSingleton<ShroudAbilityButton>.Instance;
        if (instance != null)
            instance.Timer = instance.Cooldown;
    }
}
