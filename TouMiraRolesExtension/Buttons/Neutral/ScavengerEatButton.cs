using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMiraRolesExtension.Assets;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Options.Roles.Neutral;
using TouMiraRolesExtension.Roles.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMiraRolesExtension.Buttons.Neutral;

public sealed class ScavengerEatButton : TownOfUsRoleButton<ScavengerRole, DeadBody>
{
    public override string Name => TouLocale.GetParsed("ExtensionRoleScavengerEat", "Eat");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Scavenger;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<ScavengerOptions>.Instance.EatCooldown + MapCooldown, 5f, 120f);
    public override LoadableAsset<Sprite> Sprite => TouExtensionAssets.ScavengerEatButtonSprite;
    public override float Distance => 1.5f;

    public override DeadBody? GetTarget()
    {
        if (PlayerControl.LocalPlayer == null)
        {
            return null;
        }

        var player = PlayerControl.LocalPlayer;
        var allBodies = Object.FindObjectsOfType<DeadBody>();
        DeadBody? closest = null;
        var closestDistance = float.MaxValue;

        foreach (var body in allBodies)
        {
            if (ScavengerSystem.IsBodyEaten(body.ParentId))
            {
                continue;
            }

            var distance = Vector2.Distance(player.GetTruePosition(), body.TruePosition);
            if (distance <= Distance && distance < closestDistance)
            {
                closest = body;
                closestDistance = distance;
            }
        }

        return closest;
    }

    public override bool IsTargetValid(DeadBody? target)
    {
        if (target == null || PlayerControl.LocalPlayer == null)
        {
            return false;
        }

        if (ScavengerSystem.IsBodyEaten(target.ParentId))
        {
            return false;
        }

        var player = PlayerControl.LocalPlayer;
        var distance = Vector2.Distance(player.GetTruePosition(), target.TruePosition);
        return distance <= Distance;
    }

    protected override void OnClick()
    {
        if (Target == null || PlayerControl.LocalPlayer == null)
        {
            return;
        }

        ScavengerRole.RpcScavengerEat(PlayerControl.LocalPlayer, Target.ParentId);
        ResetCooldownAndOrEffect();
    }
}