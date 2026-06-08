using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using MiraAPI.Utilities;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Crewmate;

public sealed class TrapperTrapButton : TownOfUsRoleButton<TrapperRole, Vent>
{
    public override string Name => TouLocale.GetParsed("ExtensionRoleTrapperTrap", "Trap");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Trapper;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<TrapperOptions>.Instance.TrapCooldown + MapCooldown, 5f, 120f);
    public override int MaxUses => (int)OptionGroupSingleton<TrapperOptions>.Instance.MaxTraps;
    public override LoadableAsset<Sprite> Sprite => TouCrewAssets.TrapSprite;
    public override bool ZeroIsInfinite { get; set; } = true;

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);
        if (Button == null) return;

        if (ZeroIsInfinite && MaxUses == 0)
        {
            Button.usesRemainingText.gameObject.SetActive(false);
            Button.usesRemainingSprite.gameObject.SetActive(false);
        }
        else
        {
            Button.usesRemainingText.gameObject.SetActive(true);
            Button.usesRemainingSprite.gameObject.SetActive(true);
        }
    }

    public override bool IsTargetValid(Vent? target)
    {
        return base.IsTargetValid(target) &&
               target != null &&
               !BurrowerSystem.IsBurrowerVent(target) &&
               !VentTrapSystem.IsTrapped(target.Id);
    }

    public override Vent? GetTarget()
    {
        var vent = BurrowerSystem.GetClosestUsableMapVent(
            PlayerControl.LocalPlayer,
            false,
            Distance,
            candidate => !VentTrapSystem.IsTrapped(candidate.Id));

        if (vent != null && HasClearPathToVent(PlayerControl.LocalPlayer, vent))
        {
            return vent;
        }

        return null;
    }

    public override bool CanUse()
    {
        var newTarget = GetTarget();
        if (newTarget != Target)
        {
            Target?.SetOutline(false, false);
        }

        Target = IsTargetValid(newTarget) ? newTarget : null;
        SetOutline(true);

        return base.CanUse() && Timer <= 0 && Target != null && UsesLeft > 0;
    }

    protected override void OnClick()
    {
        if (Target == null)
        {
            return;
        }

        TrapperRole.RpcTrapperPlaceTrap(PlayerControl.LocalPlayer, Target.Id);
    }

    private static bool HasClearPathToVent(PlayerControl player, Vent vent)
    {
        if (player?.Collider == null || vent == null)
        {
            return false;
        }

        var center = player.Collider.bounds.center;
        var position = vent.transform.position;
        return Vector2.Distance(center, position) <= vent.UsableDistance &&
               !PhysicsHelpers.AnythingBetween(player.Collider, center, position, Constants.ShipOnlyMask, false);
    }
}
