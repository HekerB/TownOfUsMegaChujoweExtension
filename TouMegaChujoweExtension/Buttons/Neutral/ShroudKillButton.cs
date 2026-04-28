using System;
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
using MiraAPI.Networking;
using MiraAPI.Events.Vanilla.Gameplay;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class ShroudKillButton : TownOfUsKillRoleButton<ShroudRole, PlayerControl>, IKillButton
{
    public override string Name => TranslationController.Instance.GetStringWithDefault(StringNames.KillLabel, "Kill");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Shroud;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<ShroudOptions>.Instance.KillCooldown + MapCooldown, 5f, 120f);
    public override LoadableAsset<Sprite> Sprite => TouExtensionNeuAssets.ShroudKillButtonSprite;

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
        var player = PlayerControl.LocalPlayer;
        if (player == null || Target == null) return;

        player.RpcSpecialMurder(Target, causeOfDeath: "Shroud");
        ShroudAbilityButton.SetOwnCooldown();
    }

    public override PlayerControl? GetTarget()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null) return null;
        return player.GetClosestLivingPlayer(true, Distance);
    }

    public override bool CanUse()
    {
        return base.CanUse() && Target != null && Timer <= 0;
    }

    public static void SetOwnCooldown()
    {
        var instance = CustomButtonSingleton<ShroudKillButton>.Instance;
        if (instance != null)
            instance.Timer = instance.Cooldown;
    }
}
