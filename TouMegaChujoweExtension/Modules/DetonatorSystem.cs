using BepInEx.Logging;
using Reactor.Networking.Rpc;
using System.Collections.Generic;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
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
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("DetonatorSystem");

    private class ActiveBomb
    {
        public byte DetonatorId;
        public byte TargetId;
        public float TimeElapsed;
        public bool Detonated;
        public float LastBeepTime;
    }

    private static readonly List<ActiveBomb> _activeBombs = new();
    private static readonly HashSet<byte> _bombTargets = new();

    public static void AttachBomb(byte detonatorId, byte targetId)
    {
        var options = OptionGroupSingleton<DetonatorOptions>.Instance;
        
        var target = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == targetId);
        if (target != null)
        {
            // We still use the modifier for visual/logic but without a fixed duration if we want it to last forever until detonate
            // Passing 9999f or similar for now, or updating the modifier to not care about duration
            target.AddModifier(new DetonatorBombModifier(MiscUtils.PlayerById(detonatorId), 9999f));
        }

        _activeBombs.Add(new ActiveBomb
        {
            DetonatorId = detonatorId,
            TargetId = targetId,
            TimeElapsed = 0f
        });
        _bombTargets.Add(targetId);
        Logger.LogInfo($"Bomb attached to player {targetId} by {detonatorId}");
    }

    public static bool HasBomb(byte targetId)
    {
        return _bombTargets.Contains(targetId);
    }

    public static bool HasAnyActiveBomb(byte detonatorId)
    {
        return _activeBombs.Any(b => b.DetonatorId == detonatorId && !b.Detonated);
    }

    public static bool CanManualDetonate(byte detonatorId)
    {
        var bomb = _activeBombs.FirstOrDefault(b => b.DetonatorId == detonatorId && !b.Detonated);
        if (bomb == null) return false;

        var options = OptionGroupSingleton<DetonatorOptions>.Instance;
        return bomb.TimeElapsed >= options.ManualDetonateDelay;
    }

    public static float GetManualDetonateRemainingTime(byte detonatorId)
    {
        var bomb = _activeBombs.FirstOrDefault(b => b.DetonatorId == detonatorId && !b.Detonated);
        if (bomb == null) return 0f;

        var options = OptionGroupSingleton<DetonatorOptions>.Instance;
        return Mathf.Max(0, options.ManualDetonateDelay - bomb.TimeElapsed);
    }

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
        if (MeetingHud.Instance != null || ExileController.Instance != null) return;

        UpdateTimers(Time.deltaTime);
    }

    public static void MeetingUpdate()
    {
        // We freeze the timer during meetings as requested
    }

    private static void UpdateTimers(float dt)
    {
        for (int i = _activeBombs.Count - 1; i >= 0; i--)
        {
            var bomb = _activeBombs[i];
            
            // Cleanup dead victims or detonated bombs
            var victim = MiscUtils.PlayerById(bomb.TargetId);
            if (victim == null || victim.HasDied())
            {
                _bombTargets.Remove(bomb.TargetId);
                _activeBombs.RemoveAt(i);
                continue;
            }

            if (bomb.Detonated) continue;

            bomb.TimeElapsed += dt;

            // Beeping logic after delay
            var options = OptionGroupSingleton<DetonatorOptions>.Instance;
            if (bomb.TimeElapsed >= options.ManualDetonateDelay)
            {
                float timeSinceReady = bomb.TimeElapsed - options.ManualDetonateDelay;
                // Beep faster as time goes on (from 1.5s down to 0.3s)
                float beepInterval = Mathf.Clamp(1.5f - (timeSinceReady / 15f), 0.3f, 1.5f);

                if (bomb.TimeElapsed - bomb.LastBeepTime >= beepInterval)
                {
                    bomb.LastBeepTime = bomb.TimeElapsed;
                    
                    // Volume increases (from 0.2 to 1.0)
                    float volume = Mathf.Clamp(0.2f + (timeSinceReady / 25f), 0.2f, 1.0f);
                    DetonatorRole.RpcPlayBeep(victim, bomb.DetonatorId, volume);
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

        var options = OptionGroupSingleton<DetonatorOptions>.Instance;
        var radius = options.DetonateRadius * ShipStatus.Instance.MaxLightRadius;
        var pos = mainTarget.transform.position;
        var detonator = MiscUtils.PlayerById(bomb.DetonatorId);
        var actualKiller = detonator ?? mainTarget;

        var victims = PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p != null
                        && !p.HasDied()
                        && !p.HasModifier<InvulnerabilityModifier>()
                        && Vector2.Distance((Vector2)pos, (Vector2)p.transform.position) <= radius)
            .OrderBy(p => Vector2.Distance((Vector2)pos, (Vector2)p.transform.position))
            .Take((int)options.MaxKills)
            .ToList();

        if (!victims.Contains(mainTarget))
        {
            victims.Add(mainTarget);
        }

        foreach (var victim in victims)
        {
            // Extended shield protection check (ToU Extension + Original ToU)
            bool isShielded = victim.HasModifier<BaseShieldModifier>() ||
                             victim.HasModifier<TownOfUs.Modifiers.Neutral.MercenaryGuardModifier>() ||
                             victim.HasModifier<TownOfUs.Modifiers.Crewmate.MedicShieldModifier>() ||
                             victim.HasModifier<TownOfUs.Modifiers.Crewmate.WardenFortifiedModifier>() ||
                             victim.HasModifier<TownOfUs.Modifiers.Crewmate.MagicMirrorModifier>() ||
                             victim.HasModifier<TownOfUs.Modifiers.FirstDeadShield>() ||
                             victim.HasModifier<TownOfUs.Modifiers.Neutral.GuardianAngelProtectModifier>() ||
                             victim.HasModifier<TownOfUs.Modifiers.Crewmate.ClericBarrierModifier>();

            if (isShielded)
            {
                // Trigger specific alerts for our own shields
                if (victim.TryGetModifier<BodyguardShieldModifier>(out var bgShield))
                {
                    BodyguardRole.RpcBodyguardShieldAttacked(bgShield.Bodyguard, detonator ?? victim, victim);
                }
                else if (victim.TryGetModifier<DoctorShieldModifier>(out var docShield))
                {
                    DoctorRole.RpcDoctorShieldAttacked(docShield.Doctor, victim);
                    victim.RemoveModifier(docShield);
                }
                // For original ToU shields, RpcSpecialMurder with ignoreShield: false will handle them
                else
                {
                    actualKiller.RpcSpecialMurder(
                        victim,
                        ignoreShield: false,
                        createDeadBody: true,
                        teleportMurderer: false,
                        showKillAnim: false,
                        playKillSound: true,
                        causeOfDeath: "Detonated"
                    );
                }
                continue;
            }

            actualKiller.RpcSpecialMurder(
                victim,
                ignoreShield: true, // We already checked, so if we're here, they are dead
                createDeadBody: true,
                teleportMurderer: false,
                showKillAnim: victims.Count == 1,
                playKillSound: true,
                causeOfDeath: "Detonated"
            );
        }

        // Show explosion range effect to Impostors and dead players
        DetonatorRole.RpcShowDetonationEffect(actualKiller, pos, options.DetonateRadius);

        if (detonator != null)
        {
            detonator.SetKillTimer(detonator.GetKillCooldown());
        }

        Logger.LogInfo($"Bomb detonated on player {bomb.TargetId}, victims: {victims.Count}");
    }

    public static bool IsBombTarget(byte targetId) => _bombTargets.Contains(targetId);

    public static void RoundReset()
    {
        var options = OptionGroupSingleton<DetonatorOptions>.Instance;
        for (int i = _activeBombs.Count - 1; i >= 0; i--)
        {
            var b = _activeBombs[i];
            PlayerControl? target = MiscUtils.PlayerById(b.TargetId);

            if (target == null || target.HasDied() || b.Detonated)
            {
                _bombTargets.Remove(b.TargetId);
                _activeBombs.RemoveAt(i);
            }
            else
            {
                // Instant return: keep TimeElapsed as it was (freezed during meeting)
                b.LastBeepTime = b.TimeElapsed; // Reset beep timer to match current time
            }
        }
    }

    public static void FullReset()
    {
        _activeBombs.Clear();
        _bombTargets.Clear();
    }
}
