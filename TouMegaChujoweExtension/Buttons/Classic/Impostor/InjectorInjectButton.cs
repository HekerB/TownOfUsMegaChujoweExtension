using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class InjectorInjectButton : TownOfUsKillRoleButton<InjectorRole, PlayerControl>
{
    public override string Name => TouLocale.Get("ExtensionRoleInjectorInject", "Inject");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Injector;
    public override float Cooldown
    {
        get
        {
            var baseKc = GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown;
            var multiplier = PlayerControl.LocalPlayer != null && baseKc > 0 
                ? PlayerControl.LocalPlayer.GetKillCooldown() / baseKc 
                : 1f;
            return Math.Clamp((OptionGroupSingleton<InjectorOptions>.Instance.InjectCooldown + MapCooldown) * multiplier, 5f, 120f);
        }
    }

    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.InjectorInjectButtonSprite;
    public override int MaxUses => (int)OptionGroupSingleton<InjectorOptions>.Instance.InitialUses;

    public override bool ZeroIsInfinite { get; set; } = true;
    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        if (Button != null)
        {
            Button.usesRemainingSprite.gameObject.SetActive(MaxUses > 0);
        }
    }

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

    public override void ClickHandler()
    {
        if (!CanClick()) return;
        if (Target == null) return;

        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        var beforeMurderEvent = new BeforeMurderEvent(player, Target, MeetingCheck.OutsideMeeting);
        MiraEventManager.InvokeEvent(beforeMurderEvent);
        
        if (beforeMurderEvent.IsCancelled)
        {
            return;
        }

        OnClick();
        Timer = Cooldown;
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

        InjectorRole.RpcInjectorInject(player, Target);
        
        if (OptionGroupSingleton<InjectorOptions>.Instance.SharedCooldown)
        {
            player.SetKillTimer(player.GetKillCooldown());
        }
    }
}
