using System;
using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Modifiers;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class IcenbergKillButton : TownOfUsKillRoleButton<IcenbergRole, PlayerControl>, IDiseaseableButton, IKillButton
{
    public override string Name => TranslationController.Instance.GetStringWithDefault(StringNames.KillLabel, "Kill");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Icenberg;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<IcenbergOptions>.Instance.KillCooldown + MapCooldown, 5f, 120f);
    public override LoadableAsset<Sprite> Sprite => TouExtensionNeuAssets.IcenbergKillButtonSprite;
    public override float Distance => GameManager.Instance.LogicOptions.GetKillDistance();

    public void SetDiseasedTimer(float multiplier) => SetTimer(Cooldown * multiplier);

    public override PlayerControl? GetTarget() =>
        PlayerControl.LocalPlayer?.GetClosestLivingPlayer(true, Distance, predicate: IsTargetValid);

    public override bool IsTargetValid(PlayerControl? target)
    {
        var local = PlayerControl.LocalPlayer;
        if (target == null || local == null || target == local || target.HasDied())
        {
            return false;
        }

        if (local.IsImpostorAligned() && target.IsImpostorAligned())
        {
            return false;
        }

        return !target.HasModifier<FirstDeadShield>()
               && Vector2.Distance(local.GetTruePosition(), target.GetTruePosition()) <= Distance;
    }

    protected override void OnClick()
    {
        var local = PlayerControl.LocalPlayer;
        if (local != null && Target != null && IsTargetValid(Target))
        {
            CustomTouMurderRpcs.RpcSpecialMurder(local, Target, causeOfDeath: "Frozen");
        }
    }
}
