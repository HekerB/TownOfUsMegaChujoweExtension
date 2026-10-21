using MiraAPI.GameOptions;
using MiraAPI.Modifiers.Types;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modifiers;

/// <summary>
/// Caught in the Icenberg's blizzard: slowed for the duration. The slow is a multiplier applied by
/// <c>IcenbergBlizzardSpeedPatch</c> on <c>LogicOptions.GetPlayerSpeedMod</c>, so it stacks with every other
/// speed effect instead of overwriting <c>MyPhysics.Speed</c> and restoring a stale value afterwards.
/// </summary>
public sealed class IcenbergBlizzardModifier(float duration) : TimedModifier
{
    public override string ModifierName => TouLocale.Get("ExtensionModifierIcenbergBlizzard", "Blizzard");
    public override bool HideOnUi => false;
    public override LoadableAsset<Sprite>? ModifierIcon => TouExtensionNeuAssets.BlizzardButtonSprite;
    public override float Duration => duration;

    public static float SpeedMultiplier =>
        Mathf.Clamp01(OptionGroupSingleton<IcenbergOptions>.Instance.BlizzardSpeed / 100f);

    public override void OnActivate()
    {
        base.OnActivate();
        if (Player == null || !Player.AmOwner)
        {
            return;
        }

        var notification = Helpers.CreateAndShowNotification(
            TouLocale.Get("ExtensionRoleIcenbergBlizzardNotification", "A blizzard has started!"),
            TouExtensionColors.Icenberg,
            new Vector3(0f, 1f, -20f),
            spr: TouExtensionNeuAssets.BlizzardButtonSprite.LoadAsset());
        notification.AdjustNotification();
        IcenbergOverlay.SetBlizzard(true);
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();
        if (Player != null && Player.AmOwner)
        {
            IcenbergOverlay.SetBlizzard(false);
        }
    }

    public override void OnMeetingStart()
    {
        base.OnMeetingStart();
        ModifierComponent?.RemoveModifier(this);
    }

    public override void OnDeath(DeathReason reason)
    {
        base.OnDeath(reason);
        ModifierComponent?.RemoveModifier(this);
    }
}
