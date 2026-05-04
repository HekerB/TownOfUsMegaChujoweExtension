using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Utilities;
using TownOfUs.Events;
using TownOfUs.Options;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Impostor;

public sealed class KamikazeKillButton : TownOfUsKillRoleButton<KamikazeRole, PlayerControl>, IDiseaseableButton, IKillButton
{
    public override string Name => TranslationController.Instance.GetStringWithDefault(StringNames.KillLabel, "Kill");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => Palette.ImpostorRed;
    public override float Cooldown => PlayerControl.LocalPlayer?.GetKillCooldown() ?? 25f;
    public override LoadableAsset<Sprite> Sprite => TouAssets.KillSprite;

    public void SetDiseasedTimer(float multiplier)
    {
        SetTimer(Cooldown * multiplier);
    }

    public override PlayerControl? GetTarget()
    {
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance);
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (!base.IsTargetValid(target) || target == null)
        {
            return false;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return false;
        }

        if (player.IsImpostorAligned() && target.IsImpostorAligned())
        {
            return false;
        }

        return true;
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
            return;
        }

        PlayerControl.LocalPlayer.RpcCustomMurder(Target);

        // Set suicide button cooldown
        CustomButtonSingleton<KamikazeSuicideButton>.Instance.SetTimer(
            OptionGroupSingleton<KamikazeOptions>.Instance.SuicideCooldown);
    }
}
