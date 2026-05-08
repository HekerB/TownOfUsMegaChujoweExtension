using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using MiraAPI.Modifiers;
using TownOfUs.Modifiers;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using TownOfUs.Extensions;
using UnityEngine;
using System.Linq;

namespace TouMegaChujoweExtension.Buttons.Crewmate;

public sealed class TrapperTrapButton : TownOfUsRoleButton<TrapperRole, Vent>
{
    private static readonly ContactFilter2D Filter = Helpers.CreateFilter(Constants.Usables);

    public override string Name => TouLocale.GetParsed("ExtensionRoleTrapperTrap", "Trap");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Trapper;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<TrapperOptions>.Instance.TrapCooldown + MapCooldown, 5f, 120f);
    public override int MaxUses => (int)OptionGroupSingleton<TrapperOptions>.Instance.MaxTraps;
    public override LoadableAsset<Sprite> Sprite => TouCrewAssets.TrapSprite;

    public override bool IsTargetValid(Vent? target)
    {
        return base.IsTargetValid(target) && target != null && !VentTrapSystem.IsTrapped(target.Id);
    }

    public override Vent? GetTarget()
    {
        var vent = PlayerControl.LocalPlayer.GetNearestObjectOfType<Vent>(Distance / 4, Filter) ??
                   PlayerControl.LocalPlayer.GetNearestObjectOfType<Vent>(Distance / 3, Filter) ??
                   PlayerControl.LocalPlayer.GetNearestObjectOfType<Vent>(Distance / 2, Filter) ??
                   PlayerControl.LocalPlayer.GetNearestObjectOfType<Vent>(Distance, Filter);

        if (vent != null && PlayerControl.LocalPlayer.CanUseVent(vent))
        {
            return vent;
        }

        return null;
    }

    public override void ClickHandler()
    {
        if (!CanClick()) return;
        OnClick();
        Timer = Cooldown;
    }

    public override bool CanUse()
    {
        if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance) return false;
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities)) return false;

        var newTarget = GetTarget();
        if (newTarget != Target)
        {
            Target?.SetOutline(false, false);
        }

        Target = IsTargetValid(newTarget) ? newTarget : null;
        SetOutline(true);

        return Target != null && (UsesLeft > 0 || !LimitedUses);
    }

    public override bool CanClick()
    {
        return Timer <= 0 && Target != null && (UsesLeft > 0 || !LimitedUses);
    }

    protected override void OnClick()
    {
        if (Target == null)
        {
            return;
        }

        TrapperRole.RpcTrapperPlaceTrap(PlayerControl.LocalPlayer, Target.Id);
    }
}
