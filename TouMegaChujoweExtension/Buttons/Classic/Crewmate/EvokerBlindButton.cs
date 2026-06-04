using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using MiraAPI.Utilities;
using Reactor.Utilities;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using System;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Buttons.Classic.Crewmate;

public sealed class EvokerBlindButton : TownOfUsRoleButton<EvokerRole>
{
    public override string Name => TouLocale.Get("ExtensionRoleEvokerBlind", "Blind");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Evoker;
    public override LoadableAsset<Sprite> Sprite => TouExtensionCrewAssets.EvokerBlindButtonSprite;

    public override float Cooldown => Math.Clamp(OptionGroupSingleton<EvokerOptions>.Instance.BlindCooldown.Value + MapCooldown, 5f, 120f);
    public override float InitialCooldown => OptionGroupSingleton<EvokerOptions>.Instance.BlindCooldown.Value;

    public override float EffectDuration => OptionGroupSingleton<EvokerOptions>.Instance.BlindDuration.Value;

    public override bool CanUse()
    {
        if (!base.CanUse()) return false;

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied()) return false;

        return !EvokerSystem.IsBlindActive;
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        var duration = OptionGroupSingleton<EvokerOptions>.Instance.BlindDuration.Value;
        EvokerRole.RpcEvokerBlind(player, duration);
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (Button == null) return;

        if (EffectActive && !EvokerSystem.IsBlindActive)
        {
            EffectActive = false;
            Timer = Cooldown;
        }
    }
}
