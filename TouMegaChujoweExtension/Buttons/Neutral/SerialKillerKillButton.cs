using System;
using System.Collections;
using System.Linq;
using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modifiers;
using TownOfUs.Modules;
using TownOfUs.Options.Modifiers.Alliance;
using TownOfUs.Roles;
using TownOfUs.Events;
using TownOfUs.Options;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using TownOfUs.Utilities;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class SerialKillerKillButton : TownOfUsKillRoleButton<SerialKillerRole, PlayerControl>, IDiseaseableButton, IKillButton
{
    public override string Name => TranslationController.Instance.GetStringWithDefault(StringNames.KillLabel, "Kill");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.SerialKiller;
    public override float Cooldown
    {
        get
        {
            var player = PlayerControl.LocalPlayer;
            if (player == null) return 0f;

            var options = OptionGroupSingleton<SerialKillerOptions>.Instance;
            float baseCooldown;

            if (player.TryGetModifier<SerialKillerManiacModifier>(out var maniacMod))
            {
                baseCooldown = maniacMod.CooldownDuration;
            }
            else
            {
                baseCooldown = GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.KillCooldown);
            }

            if (player.TryGetModifier<SerialKillerReductionModifier>(out var redMod))
            {
                return Mathf.Max(options.MinimumKillCooldown.Value, baseCooldown - redMod.ReductionAmount);
            }

            return baseCooldown;
        }
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

    public override LoadableAsset<Sprite> Sprite => new LoadableBundleAsset<Sprite>("OfficerShootButton", TouAssets.MainBundle);

    public override bool ZeroIsInfinite { get; set; } = true;

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

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            Error("Serial Killer Kill: LocalPlayer is null");
            return;
        }

        if (Target == null)
        {
            Error("Serial Killer Kill: Target is null");
            return;
        }

        if (!IsTargetValid(Target))
        {
            Error("Serial Killer Kill: Target is invalid");
            return;
        }

        if (player.inVent && Vent.currentVent != null && !player.HasModifier<SerialKillerNoVentModifier>() &&
            SerialKillerVentKillSystem.TryGetVentKillTarget(player.PlayerId, out var ventTarget) &&
            ventTarget != null && ventTarget.PlayerId == Target.PlayerId)
        {
            player.RpcCustomMurder(Target, true, true, true, false, true, true);
            Role.KillCount++;
            Role.UpdateReductionModifier();
            return;
        }

        player.RpcCustomMurder(Target);
        Role.KillCount++;
        Role.UpdateReductionModifier();
    }

    public override PlayerControl? GetTarget()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return null;
        }

        if (player.inVent && Vent.currentVent != null && !player.HasModifier<SerialKillerNoVentModifier>())
        {
            CheckVentKillOpportunity(Vent.currentVent, player);

            if (SerialKillerVentKillSystem.TryGetVentKillTarget(player.PlayerId, out var ventTarget))
            {
                if (ventTarget != null && !ventTarget.HasDied() && ventTarget.inVent)
                {
                    int targetVentId = GetPlayerVentId(ventTarget);
                    if (targetVentId >= 0 && targetVentId == Vent.currentVent.Id)
                    {
                        return ventTarget;
                    }
                }
                
                SerialKillerVentKillSystem.ClearForPlayer(player.PlayerId);
            }
            
            return player.GetClosestLivingPlayer(true, Distance);
        }

        if (!OptionGroupSingleton<LoversOptions>.Instance.LoversKillEachOther && player.IsLover())
        {
            return player.GetClosestLivingPlayer(true, Distance, false, x => !x.IsLover());
        }

        return player.GetClosestLivingPlayer(true, Distance);
    }

    private static void CheckVentKillOpportunity(Vent vent, PlayerControl serialKiller)
    {
        if (serialKiller.HasModifier<SerialKillerNoVentModifier>())
        {
            SerialKillerVentKillSystem.ClearForPlayer(serialKiller.PlayerId);
            return;
        }

        var options = OptionGroupSingleton<SerialKillerOptions>.Instance;

        PlayerControl? target = null;
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.PlayerId == serialKiller.PlayerId || player.HasDied() || !player.inVent)
            {
                continue;
            }

            int playerVentId = GetPlayerVentId(player);
            if (playerVentId >= 0 && playerVentId == vent.Id && IsValidVentKillTarget(player, options.VentKillTargets))
            {
                target = player;
                break;
            }
        }

        if (target != null)
        {
            SerialKillerVentKillSystem.SetVentKillTarget(serialKiller.PlayerId, target);
        }
        else
        {
            SerialKillerVentKillSystem.ClearForPlayer(serialKiller.PlayerId);
        }
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (target == null)
        {
            return false;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return false;
        }

        // Targeting is allowed, shield block handled in ShieldEvents

        if (target.inVent)
        {
            if (player.inVent && Vent.currentVent != null && !player.HasModifier<SerialKillerNoVentModifier>() && SerialKillerVentKillSystem.TryGetVentKillTarget(player.PlayerId, out var ventTarget))
            {
                return ventTarget != null && ventTarget.PlayerId == target.PlayerId && !target.HasDied();
            }
            return false;
        }

        return base.IsTargetValid(target);
    }

    public override void FixedUpdateHandler(PlayerControl playerControl)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || !player.IsRole<SerialKillerRole>())
        {
            return;
        }

        var hasNoVentModifier = player.HasModifier<SerialKillerNoVentModifier>();

        base.FixedUpdateHandler(playerControl);

        if (!hasNoVentModifier)
        {
            var newTarget = GetTarget();
            if (newTarget != Target)
            {
                SetOutline(false);
            }
            Target = IsTargetValid(newTarget) ? newTarget : null;
            SetOutline(true);

            UpdateVentHighlighting();

            if (Button != null && Target != null && player != null)
            {
                var canHighlight = !TimeLordRewindSystem.IsRewinding &&
                                   !(HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance) &&
                                   !(player.HasDied() && !UsableInDeath) &&
                                   (player.CanMove || (player.inVent && Vent.currentVent != null && !player.HasModifier<SerialKillerNoVentModifier>())) &&
                                   !player.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities);

                if (canHighlight)
                {
                    Button.SetEnabled();
                }
            }
        }
        else
        {
            UpdateVentHighlighting();
            if (player.inVent || Vent.currentVent != null)
            {
                Target = null;
            }
        }
    }

    public override bool CanUse()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return false;
        }

        if (TimeLordRewindSystem.IsRewinding)
        {
            return false;
        }

        if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance)
        {
            return false;
        }

        if (player.HasDied() && !UsableInDeath)
        {
            return false;
        }

        if (player.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities))
        {
            return false;
        }

        var inVent = player.inVent && Vent.currentVent != null && !player.HasModifier<SerialKillerNoVentModifier>();
        if (!inVent && !player.CanMove)
        {
            return false;
        }

        return Target != null && Timer <= 0;
    }

    private static void UpdateVentHighlighting()
    {
        var player = PlayerControl.LocalPlayer;
        var options = OptionGroupSingleton<SerialKillerOptions>.Instance;
        var highlightColor = Color.green;
        var ventColor = TouExtensionColors.SerialKiller;
        if (player == null || player.HasModifier<SerialKillerNoVentModifier>() || PlayerControl.AllPlayerControls.ToArray().Count(x => !x.HasDied()) <= 2)
        {
            if (ShipStatus.Instance != null)
            {
                foreach (var vent in ShipStatus.Instance.AllVents.Where(v => v != null))
                {
                    vent.SetOutline(false, false, Color.clear);
                }
            }
            return;
        }

        if (ShipStatus.Instance == null)
        {
            return;
        }

        var playerPos = player.GetTruePosition();

        foreach (var vent in ShipStatus.Instance.AllVents)
        {
            if (vent == null)
            {
                continue;
            }

            var ventPos3D = vent.transform.position;
            var ventPos2D = new Vector2(ventPos3D.x, ventPos3D.y);
            var distance = Vector2.Distance(playerPos, ventPos2D);

            var direction = (ventPos2D - playerPos).normalized;
            var inVision = distance < player.lightSource.viewDistance &&
                          !PhysicsHelpers.AnyNonTriggersBetween(playerPos, direction, distance, Constants.ShipAndObjectsMask);

            var canUseVent = player.CanUseVent(vent);

            var hasValidTarget = false;
            foreach (var otherPlayer in PlayerControl.AllPlayerControls)
            {
                if (otherPlayer == null || otherPlayer.PlayerId == player.PlayerId || otherPlayer.HasDied() || !otherPlayer.inVent)
                {
                    continue;
                }

                int playerVentId = GetPlayerVentId(otherPlayer);
                if (playerVentId >= 0 && playerVentId == vent.Id && IsValidVentKillTarget(otherPlayer, options.VentKillTargets))
                {
                    hasValidTarget = true;
                    break;
                }
            }

            if (hasValidTarget && inVision)
            {
                if (canUseVent)
                {
                    vent.SetOutline(true, true, highlightColor);
                }
                else
                {
                    vent.SetOutline(true, false, highlightColor);
                }
            }
            else if (inVision && canUseVent)
            {
                vent.SetOutline(true, true, ventColor);
            }
            else if (inVision)
            {
                vent.SetOutline(true, false, ventColor);
            }
            else
            {
                vent.SetOutline(false, false, Color.clear);
            }
        }
    }

    private static int GetPlayerVentId(PlayerControl player)
    {
        if (player.AmOwner && Vent.currentVent != null)
        {
            return Vent.currentVent.Id;
        }

        if (!player.inVent)
        {
            return -1;
        }

        foreach (var vent in ShipStatus.Instance.AllVents)
        {
            if (vent == null)
            {
                continue;
            }

            if (VentOccupancySystem.TryGetOccupant(vent.Id, out var occupantId) && occupantId == player.PlayerId)
            {
                return vent.Id;
            }
        }

        var playerPos = player.transform.position;
        foreach (var vent in ShipStatus.Instance.AllVents)
        {
            if (vent == null)
            {
                continue;
            }

            var ventPos = vent.transform.position;
            if (Vector2.Distance(new Vector2(playerPos.x, playerPos.y), new Vector2(ventPos.x, ventPos.y)) < 0.5f)
            {
                return vent.Id;
            }
        }

        return -1;
    }

    private static bool IsValidVentKillTarget(PlayerControl target, VentKillTargets ventKillTargets)
    {
        return ventKillTargets switch
        {
            VentKillTargets.Impostors => target.IsImpostor(),
            VentKillTargets.ImpNK => target.IsImpostor() || target.Is(RoleAlignment.NeutralKilling),
            VentKillTargets.ImpNeutrals => target.IsImpostor() || target.IsNeutral(),
            VentKillTargets.Any => true,
            _ => false
        };
    }
}
