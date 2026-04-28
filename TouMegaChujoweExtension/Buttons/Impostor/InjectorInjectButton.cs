using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Buttons;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Impostor;

public sealed class InjectorInjectButton : TownOfUsKillRoleButton<InjectorRole, PlayerControl>
{
    public override string Name => TouLocale.Get("ExtensionRoleInjectorInject", "Inject");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Injector;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<InjectorOptions>.Instance.InjectCooldown + MapCooldown, 5f, 120f);
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.InjectorInjectButtonSprite;
    public override int MaxUses => (int)OptionGroupSingleton<InjectorOptions>.Instance.InitialUses;

    public override bool ZeroIsInfinite { get; set; } = true;

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (!base.IsTargetValid(target) || target == null)
        {
            return false;
        }

        return CanInjectTarget(target);
    }

    private static bool CanInjectTarget(PlayerControl? target)
    {
        if (target == null)
        {
            return false;
        }

        if (target.IsImpostor())
        {
            return false;
        }

        // Targeting allowed, shield handled in OnClick
        if (target.HasModifier<FirstDeadShield>())
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
        if (Target == null)
        {
            Error("Injector Inject: Target is null");
            return;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            Error("Injector Inject: LocalPlayer is null");
            return;
        }

        var shieldType = ShieldUtils.GetShieldType(Target);
        if (shieldType != ShieldType.None)
        {
            ShieldUtils.TriggerShieldFlash(player, shieldType);
            if (OptionGroupSingleton<InjectorOptions>.Instance.SharedCooldown)
            {
                player.SetKillTimer(player.GetKillCooldown());
            }
            Timer = Cooldown;
            return;
        }

        InjectorRole.RpcInjectorInject(player, Target);
        
        if (OptionGroupSingleton<InjectorOptions>.Instance.SharedCooldown)
        {
            player.SetKillTimer(player.GetKillCooldown());
        }
    }
}