using System;
using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using TownOfUs.Extensions;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class FamineStarveButton : TownOfUsRoleButton<FamineRole, PlayerControl>, IDiseaseableButton
{
    public override string Name => TouLocale.Get("ExtensionRoleFamineStarve", "Starve");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Famine;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<BakerOptions>.Instance.StarveCooldown + MapCooldown, 0f, 120f);
    public override float EffectDuration => 0f;
    public override bool ZeroIsInfinite => true;
    public override LoadableAsset<Sprite> Sprite => TownOfUs.Assets.TouNeutAssets.ReapSprite;

    public void SetDiseasedTimer(float multiplier)
    {
        SetTimer(Cooldown * multiplier);
    }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
    }

    public override float Distance => GameManager.Instance.LogicOptions.GetKillDistance();

    public override PlayerControl? GetTarget()
    {
        if (PlayerControl.LocalPlayer == null)
        {
            return null;
        }

        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance, predicate: IsTargetValid);
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (target == null || PlayerControl.LocalPlayer == null || target == PlayerControl.LocalPlayer)
        {
            return false;
        }

        if (target.HasModifier<FamineStarvedModifier>())
        {
            return false;
        }

        if (ApocalypseUtils.AreAllied(PlayerControl.LocalPlayer, target))
        {
            return false;
        }

        if (!Role.CanStarveAnyone && !target.HasModifier<BakerBreadModifier>())
        {
            return false;
        }

        var distance = Vector2.Distance(PlayerControl.LocalPlayer.GetTruePosition(), target.GetTruePosition());
        return distance <= Distance && !target.HasDied();
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || Target == null)
        {
            return;
        }

        if (PoisonSystem.CheckAndTriggerShields(player, Target))
        {
            Timer = Cooldown;
            return;
        }

        FamineRole.RpcStarvePlayer(player, Target);
        Timer = Cooldown;
    }
}
