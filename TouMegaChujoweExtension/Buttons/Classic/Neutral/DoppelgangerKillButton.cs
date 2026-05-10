using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Networking;
using MiraAPI.Utilities.Assets;
using MiraAPI.Utilities;
using System.Collections;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Events;
using TownOfUs.Modifiers;
using TownOfUs.Options.Modifiers.Alliance;
using TownOfUs.Options;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class DoppelgangerKillButton : TownOfUsKillRoleButton<DoppelgangerRole, PlayerControl>, IDiseaseableButton, IKillButton
{
    public override string Name => TranslationController.Instance.GetStringWithDefault(StringNames.KillLabel, "Kill");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Doppelganger;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<DoppelgangerOptions>.Instance.KillCooldown + MapCooldown, 5f, 120f);
    public override LoadableAsset<Sprite> Sprite => TouExtensionNeuAssets.DoppelgangerKillButtonSprite;
    public override bool ShouldPauseInVent => true;

    public void SetDiseasedTimer(float multiplier)
    {
        SetTimer(Cooldown * multiplier);
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
        
        if (HasEffect)
        {
            EffectActive = true;
            Timer = EffectDuration;
        }
        else
        {
            Timer = Cooldown;
        }
    }
    public override bool IsTargetValid(PlayerControl? target)
    {
        if (!base.IsTargetValid(target) || target == null)
        {
            return false;
        }

        // Targeting is allowed, shield block handled in ShieldEvents

        return true;
    }

    protected override void OnClick()
    {
        if (Target == null)
        {
            Error("Doppelganger Kill: Target is null");
            return;
        }

        PlayerControl.LocalPlayer.RpcCustomMurder(Target);
    }

    public override PlayerControl? GetTarget()
    {
        if (!OptionGroupSingleton<LoversOptions>.Instance.LoversKillEachOther && PlayerControl.LocalPlayer.IsLover())
        {
            return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance, false, x => !x.IsLover());
        }

        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance);
    }

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

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        var player = PlayerControl.LocalPlayer;
        if (player == null || !player.IsRole<DoppelgangerRole>() || Button == null)
        {
            return;
        }

        var role = player.GetRole<DoppelgangerRole>();
        if (role == null)
        {
            return;
        }

        if (role.HasInfiniteSteals)
        {
            Button.usesRemainingText.gameObject.SetActive(false);
            Button.usesRemainingSprite.gameObject.SetActive(false);
        }
        else
        {
            SetUses(role.RemainingIdentityThefts);
        }
    }
}














