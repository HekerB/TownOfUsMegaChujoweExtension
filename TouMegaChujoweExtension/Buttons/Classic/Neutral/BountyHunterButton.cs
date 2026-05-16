using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using System;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Events;
using TownOfUs.Modules.Localization;
using TownOfUs.Options;
using TownOfUs.Utilities;
using TownOfUs.Modifiers;
using TownOfUs;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class BountyHunterKillButton : TownOfUsRoleButton<BountyHunterRole, PlayerControl>
{
    public override string Name => TouLocale.Get("ExtensionRoleBountyHunterKill", "Hunt");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override float Distance => GameOptionsManager.Instance.currentNormalGameOptions.KillDistance;

    public override float Cooldown
    {
        get
        {
            var opts = OptionGroupSingleton<BountyHunterOptions>.Instance;
            float cd = opts.KillCooldown.Value;
            return Math.Clamp(cd + MapCooldown, 5f, 120f);
        }
    }

    public override float EffectDuration => 0f;
    public override LoadableAsset<Sprite> Sprite => new LoadableBundleAsset<Sprite>("OfficerShootButton", TouAssets.MainBundle);
    public override Color TextOutlineColor => TouExtensionColors.BountyHunter;

    public override int MaxUses =>
        Mathf.Max(1, (int)OptionGroupSingleton<BountyHunterOptions>.Instance.TargetsToKill.Value);

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);

        if (Button != null)
        {
            Button.usesRemainingSprite.sprite = TouAssets.AbilityCounterPlayerSprite.LoadAsset();
            Button.usesRemainingText.gameObject.SetActive(true);
            Button.usesRemainingSprite.gameObject.SetActive(true);
        }

        RefreshKillCounter();
    }

    private void RefreshKillCounter()
    {
        if (PlayerControl.LocalPlayer?.Data?.Role is BountyHunterRole role)
        {
            int remaining = Mathf.Clamp(MaxUses - role.KillsDone, 0, MaxUses);
            SetUses(remaining);
        }
    }

    public override bool Enabled(RoleBehaviour? role)
    {
        var opts = OptionGroupSingleton<BountyHunterOptions>.Instance;
        if (!opts.CanKillInRoundOne && TownOfUs.Events.DeathEventHandlers.CurrentRound <= 1)
            return false;

        return base.Enabled(role) && !PlayerControl.LocalPlayer.Data.IsDead;
    }

    public override PlayerControl? GetTarget()
    {
        if (PlayerControl.LocalPlayer?.Data?.Role is not BountyHunterRole role)
            return null;

        if (role.CurrentTarget == null)
            return null;

        if (role.CurrentTarget.Data == null ||
            role.CurrentTarget.Data.IsDead ||
            role.CurrentTarget.Data.Disconnected)
            return null;

        var closest = PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance);

        if (closest == null)
            return null;

        if (closest.TryGetModifier<ChildModifier>(out var child) && !child.IsAdult)
            return null;

        return closest;

        return closest;
    }

    protected override void FixedUpdate(PlayerControl rolePlayer)
    {
        base.FixedUpdate(rolePlayer);
        RefreshKillCounter();
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
    }

    protected override void OnClick()
    {
        if (Target == null || PlayerControl.LocalPlayer == null)
            return;

        if (PlayerControl.LocalPlayer?.Data?.Role is not BountyHunterRole role)
            return;

        bool isTarget = role.CurrentTarget != null && Target.PlayerId == role.CurrentTarget.PlayerId;

        // Sync for event handlers if needed
        role.LastTargetPlayerId = isTarget ? Target.PlayerId : (byte)255;

        // Perform murder
        PlayerControl.LocalPlayer.RpcCustomMurder(Target);

        if (isTarget)
        {
            role.OnTargetKilled();
        }
        else
        {
            // Wrong person - suicide
            BountyHunterRole.RpcShowBountyHunterMisKillText(Target, PlayerControl.LocalPlayer);

            // Suicide for the Bounty Hunter
            PlayerControl.LocalPlayer.RpcCustomMurder(PlayerControl.LocalPlayer, showKillAnim: false);
        }

        RefreshKillCounter();
        ResetCooldownAndOrEffect();
    }
}
















