using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Networking;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Utilities;
using TownOfUs.Events;
using TownOfUs.Options;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class ShroudKillButton : TownOfUsKillRoleButton<ShroudRole, PlayerControl>, IKillButton
{
    public override string Name => TranslationController.Instance.GetStringWithDefault(StringNames.KillLabel, "Kill");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Shroud;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<ShroudOptions>.Instance.KillCooldown + MapCooldown, 5f, 120f);
    public override LoadableAsset<Sprite> Sprite => TouExtensionNeuAssets.ShroudKillButtonSprite;

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

    public static void SetOwnCooldown()
    {
        var instance = CustomButtonSingleton<ShroudKillButton>.Instance;
        if (instance != null)
            instance.Timer = instance.Cooldown;
    }
}
