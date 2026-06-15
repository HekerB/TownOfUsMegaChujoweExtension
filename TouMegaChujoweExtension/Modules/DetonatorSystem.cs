using BepInEx.Logging;
using Reactor.Networking.Rpc;
using System.Collections.Generic;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TouMegaChujoweExtension.Modifiers.Crewmate;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using UnityEngine;
using System.Linq;
using MiraAPI.Modifiers;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using MiraAPI.GameOptions;

namespace TouMegaChujoweExtension.Modules;

public static class DetonatorSystem
{


    private sealed class ActiveBomb
    {
        public byte DetonatorId;
        public byte TargetId;
        public float TimeElapsed;
        public bool Detonated;
        public float LastBeepTime;
        public float CreationTime; 
    }

    private static readonly List<ActiveBomb> _activeBombs = [];
    private static readonly HashSet<byte> _bombTargets = [];
    private static float _timeSinceRoundStart;

    public static bool IsAtRoundStart => _timeSinceRoundStart < 30f;
    
    public static float GetDetonateCooldown()
    {
        var options = OptionGroupSingleton<DetonatorOptions>.Instance;
        var baseKc = GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown;
        var multiplier = PlayerControl.LocalPlayer != null && baseKc > 0 
            ? PlayerControl.LocalPlayer.GetKillCooldown() / baseKc 
            : 1f;
        return options.ManualDetonateDelay * multiplier;
    }

    public static float GetAttachRemainingTime(byte playerId)
    {
        var player = MiscUtils.PlayerById(playerId);
        return player != null ? player.killTimer : 0f;
    }

    public static void ResetAttachCooldown(byte playerId)
    {
        var player = MiscUtils.PlayerById(playerId);
        player?.SetKillTimer(player.GetKillCooldown());
    }

    public static void ResetDetonateCooldown(byte playerId)
    {
        var bomb = _activeBombs.FirstOrDefault(b => b.DetonatorId == playerId && !b.Detonated);
        if (bomb != null)
        {
            bomb.CreationTime = Time.time;
            bomb.TimeElapsed = 0f;
        }
    }

    public static float GetManualDetonateRemainingTime(byte detonatorId)
    {
        var bomb = _activeBombs.FirstOrDefault(b => b.DetonatorId == detonatorId && !b.Detonated);
        if (bomb == null) return 0f;

        float elapsed = Time.time - bomb.CreationTime;
        return Mathf.Max(0, GetDetonateCooldown() - elapsed);
    }

    public static void AttachBomb(byte detonatorId, byte targetId)
    {
        var target = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == targetId);
        var detonator = MiscUtils.PlayerById(detonatorId);
        
        if (target != null && detonator != null)
        {
            target.AddModifier(new DetonatorBombModifier(detonator));
        }

        _activeBombs.Add(new ActiveBomb
        {
            DetonatorId = detonatorId,
            TargetId = targetId,
            TimeElapsed = 0f,
            CreationTime = Time.time
        });
        _bombTargets.Add(targetId);
    }

    public static void RemoveBomb(byte targetId)
    {
        _bombTargets.Remove(targetId);
        _activeBombs.RemoveAll(b => b.TargetId == targetId);
    }

    public static bool HasBomb(byte targetId) => _bombTargets.Contains(targetId);
    public static bool IsBombTarget(byte targetId) => _bombTargets.Contains(targetId);
    public static bool HasAnyActiveBomb(byte detonatorId) => _activeBombs.Any(b => b.DetonatorId == detonatorId && !b.Detonated);

    public static void ManualDetonate(byte detonatorId)
    {
        var bombs = _activeBombs.Where(b => b.DetonatorId == detonatorId && !b.Detonated).ToList();
        foreach (var bomb in bombs)
        {
            _bombTargets.Remove(bomb.TargetId);
            Detonate(bomb);
        }
    }

    public static void Update()
    {
        _timeSinceRoundStart += Time.deltaTime;
        if (MeetingHud.Instance != null || ExileController.Instance != null) return;
        UpdateTimers(Time.deltaTime);
    }

    public static void OnRoundStart()
    {
        _timeSinceRoundStart = 0f;
        // Don't clear bombs here - they should persist through meetings!
    }

    public static void OnMeetingEnd()
    {
        var impostors = PlayerControl.AllPlayerControls.ToArray().Where(p => p.Data.Role.IsImpostor);
        foreach (var imp in impostors)
        {
            var bomb = _activeBombs.FirstOrDefault(b => b.DetonatorId == imp.PlayerId && !b.Detonated);
            if (bomb != null) bomb.CreationTime = Time.time;
        }
    }

    public static void MeetingUpdate() 
    {
        // Handled by other systems
    }

    private static void UpdateTimers(float dt)
    {
        if (_activeBombs.Count == 0) return;

        float detonateCooldown = GetDetonateCooldown();
        float timeNow = Time.time;
        var options = OptionGroupSingleton<DetonatorOptions>.Instance;

        for (int i = _activeBombs.Count - 1; i >= 0; i--)
        {
            var bomb = _activeBombs[i];
            var victim = MiscUtils.PlayerById(bomb.TargetId);
            var detonator = MiscUtils.PlayerById(bomb.DetonatorId);

            if (victim == null || victim.HasDied() || detonator == null || detonator.HasDied())
            {
                _bombTargets.Remove(bomb.TargetId);
                _activeBombs.RemoveAt(i);
                continue;
            }

            if (bomb.Detonated) continue;
            bomb.TimeElapsed += dt;

            float elapsed = timeNow - bomb.CreationTime;
            float detonateRemaining = Mathf.Max(0, detonateCooldown - elapsed);

            if (detonateRemaining <= 0)
            {
                float timeSinceReady = bomb.TimeElapsed - options.ManualDetonateDelay;
                float beepInterval = Mathf.Clamp(1.5f - (timeSinceReady / 15f), 0.3f, 1.5f);
                if (bomb.TimeElapsed - bomb.LastBeepTime >= beepInterval)
                {
                    bomb.LastBeepTime = bomb.TimeElapsed;
                    float volume = Mathf.Clamp(0.2f + (timeSinceReady / 25f), 0.2f, 1.0f);
                    DetonatorRole.PlayBeep(victim, bomb.DetonatorId, volume);
                }
            }
        }
    }

    private static void Detonate(ActiveBomb bomb)
    {
        bomb.Detonated = true;
        if (!AmongUsClient.Instance.AmHost) return;
        PlayerControl? mainTarget = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == bomb.TargetId);
        if (mainTarget == null || mainTarget.HasDied()) return;
        if (PelicanSystem.IsSwallowed(mainTarget.PlayerId)) return;
        var options = OptionGroupSingleton<DetonatorOptions>.Instance;
        var radius = options.DetonateRadius * ShipStatus.Instance.MaxLightRadius;
        var pos = mainTarget.transform.position;
        var detonator = MiscUtils.PlayerById(bomb.DetonatorId);
        var actualKiller = detonator ?? mainTarget;
        JokerCloneSystem.TriggerClonesInRadius(actualKiller, pos, radius);
        var victims = PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && !p.HasDied() && !PelicanSystem.IsSwallowed(p.PlayerId) && Vector2.Distance(pos, p.transform.position) <= radius).OrderBy(p => Vector2.Distance(pos, p.transform.position)).Take((int)options.MaxKills).ToList();
        if (!victims.Contains(mainTarget) && !PelicanSystem.IsSwallowed(mainTarget.PlayerId)) victims.Add(mainTarget);
        foreach (var victim in victims.Where(victim => victim != null && !victim.HasDied()))
        {
                // Check for invulnerability (e.g. Pestilence, Veteran on alert)
                if (victim.TryGetModifier<TownOfUs.Modifiers.InvulnerabilityModifier>(out var invic) &&
                    !actualKiller.HasModifier<TownOfUs.Modifiers.IgnoreInvulnerabilityModifier>())
                {
                    // If target is Pestilence (AttackMurderer), kill the attacker
                    if (invic.AttackMurderer && actualKiller.AmOwner)
                    {
                        victim.RpcCustomMurder(actualKiller);
                    }
                    continue; // Skip killing the invincible target
                }

                actualKiller.RpcSpecialMurder(victim, ignoreShield: false, createDeadBody: true, teleportMurderer: false, showKillAnim: victims.Count == 1, playKillSound: true, causeOfDeath: "Detonated");
        }
        DetonatorRole.RpcShowDetonationEffect(actualKiller, pos, options.DetonateRadius);
        if (detonator is not null)
        {
            DetonatorRole.RpcPlayExplosion(detonator);
            detonator.SetKillTimer(detonator.GetKillCooldown());
        }
    }

    public static void RoundReset() { OnMeetingEnd(); }
    public static void FullReset() { _activeBombs.Clear(); _bombTargets.Clear(); }
}
