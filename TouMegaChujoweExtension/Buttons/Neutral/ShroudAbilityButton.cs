using System.Collections;
using System.Linq;
using MiraAPI.GameOptions;
using TownOfUs.Events;
using TownOfUs.Options;
using MiraAPI.Events;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Utilities;
using TownOfUs.Modules;
using TownOfUs.Modifiers;
using TouMegaChujoweExtension.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class ShroudAbilityButton : TownOfUsRoleButton<ShroudRole, PlayerControl>
{
    public override string Name => "Shroud";
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Shroud;
    public override float Cooldown => OptionGroupSingleton<ShroudOptions>.Instance.ShroudCooldown;
    public override LoadableAsset<Sprite> Sprite => TouExtensionNeuAssets.ShroudAbilitySprite;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        Reactor.Utilities.Coroutines.Start(CoMoveWithDelay());
    }

    private IEnumerator CoMoveWithDelay()
    {
        yield return null;
        yield return MiscUtils.CoMoveButtonIndex(this, false);
    }

    public override PlayerControl? GetTarget()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null) return null;
        return player.GetClosestLivingPlayer(true, Distance);
    }

    public override bool CanUse()
    {
        if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance) return false;
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities)) return false;

        return GetTarget() != null;
    }

    public override bool CanClick()
    {
        return base.CanClick() && Timer <= 0;
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (target == null || target.HasDied() || target == PlayerControl.LocalPlayer) return false;

        // Block targeting ONLY for Child
        if (target.GetShieldType() == ShieldType.Child) return false;

        if (target.TryGetModifier<ShroudedModifier>(out var mod) && mod.ShroudOwnerId == PlayerControl.LocalPlayer.PlayerId)
            return false;

        return base.IsTargetValid(target);
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || Target == null) return;

        // Check for shields (excluding Child which is blocked in targeting)
        var shieldType = Target.GetShieldType();
        if (shieldType != ShieldType.None)
        {
            // Blocked by shield. Trigger flash only if it's NOT DeadlyQuota.
            if (shieldType != ShieldType.DeadlyQuota)
            {
                ShieldUtils.TriggerShieldFlash(player, shieldType);
            }
            
            Timer = (shieldType == ShieldType.FirstDead) ? 0.1f : OptionGroupSingleton<GeneralOptions>.Instance.TempSaveCdReset;
            return;
        }

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
