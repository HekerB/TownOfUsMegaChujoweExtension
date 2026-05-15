using System.Collections.Generic;
using UnityEngine;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Roles.Crewmate;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using MiraAPI.GameOptions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modifiers;
using MiraAPI.Modifiers;
using TownOfUs.Roles;
using TownOfUs.Buttons;
using System.Linq;
using TouMegaChujoweExtension.Buttons.Crewmate;
using MiraAPI.Hud;

namespace TouMegaChujoweExtension.Modules;

public static class SentinelSystem
{
    private class ActivePatrol
    {
        public Vector2 Position;
        public float RemainingDuration;
        public float FlickerTimer;
        public bool RendererEnabled = true;
        public bool CooldownStarted = false;
        public TownOfUsButton? CachedButton;
    }

    private static readonly Dictionary<byte, ActivePatrol> _activePatrols = new();

    public static void SetPatrol(byte playerId, Vector2 position)
    {
        var duration = OptionGroupSingleton<SentinelOptions>.Instance.Duration;
        _activePatrols[playerId] = new ActivePatrol
        {
            Position = position,
            RemainingDuration = duration
        };
    }

    public static Vector2? GetActivePatrolPosition(byte playerId)
    {
        return _activePatrols.TryGetValue(playerId, out var patrol) ? patrol.Position : null;
    }

    public static float GetRemainingDuration(byte playerId)
    {
        return _activePatrols.TryGetValue(playerId, out var patrol) ? patrol.RemainingDuration : 0f;
    }

    public static void HandleKill(PlayerControl killer, PlayerControl victim)
    {
        if (killer == null || victim == null) return;

        foreach (var entry in _activePatrols)
        {
            var sentinelId = entry.Key;
            var patrol = entry.Value;
            var sentinel = MiscUtils.PlayerById(sentinelId);

            if (sentinel == null || sentinel.HasDied() || !sentinel.IsRole<SentinelRole>()) continue;

            float radius = OptionGroupSingleton<SentinelOptions>.Instance.Radius;
            float dist = Vector2.Distance(victim.transform.position, patrol.Position);

            float maxLightRadius = ShipStatus.Instance != null ? ShipStatus.Instance.MaxLightRadius : 1f;
            float effectiveRadius = radius * maxLightRadius;

            if (dist <= effectiveRadius)
            {
                // Killer killed someone inside the patrol area!
                sentinel.RpcSpecialMurder(
                    killer,
                    createDeadBody: true,
                    teleportMurderer: false,
                    showKillAnim: true,
                    playKillSound: true,
                    causeOfDeath: "Patrolled"
                );
                
                // Removed break to allow one Sentinel to process multiple killers or multiple Sentinels to process the same killer
            }
        }
    }

    public static void Update()
    {
        float dt = Time.deltaTime;
        var local = PlayerControl.LocalPlayer;
        
        var expiredKeys = new List<byte>();
        foreach (var entry in _activePatrols)
        {
            var patrol = entry.Value;
            var sentinelId = entry.Key;
            var sentinel = MiscUtils.PlayerById(sentinelId);

            patrol.RemainingDuration -= dt;

            // --- LOCAL VISUALS & COOLDOWN ---
            if (local != null && local.PlayerId == sentinelId && sentinel != null)
            {
                if (sentinel.Data?.Role is SentinelRole sentinelRole)
                {
                    if (patrol.CachedButton == null)
                    {
                        patrol.CachedButton = CustomButtonManager.Buttons.FirstOrDefault(b => b is SentinelPatrolButton) as TownOfUsButton;
                    }

                    var button = patrol.CachedButton;
                    if (button != null)
                    {
                        if (patrol.RemainingDuration > 0f)
                        {
                            button.Timer = patrol.RemainingDuration;
                            
                            // Flickering logic for the button in the last 3 seconds
                            if (patrol.RemainingDuration < 3f)
                            {
                                patrol.FlickerTimer += dt;
                                if (patrol.FlickerTimer >= 0.15f)
                                {
                                    patrol.FlickerTimer = 0f;
                                    patrol.RendererEnabled = !patrol.RendererEnabled;
                                    
                                    if (button.Button != null)
                                    {
                                        var sr = button.Button.GetComponent<SpriteRenderer>();
                                        if (sr != null)
                                        {
                                            sr.color = patrol.RendererEnabled ? Color.white : new Color(1f, 1f, 1f, 0.3f);
                                        }
                                    }
                                }
                            }
                            else if (!patrol.RendererEnabled)
                            {
                                // Reset button if it was flickering and duration was somehow increased or it's over
                                patrol.RendererEnabled = true;
                                if (button.Button != null)
                                {
                                    var sr = button.Button.GetComponent<SpriteRenderer>();
                                    if (sr != null) sr.color = Color.white;
                                }
                            }
                            
                            // Ensure sphere is always visible while active
                            if (sentinelRole.PatrolAreaObject != null && sentinelRole.PatrolAreaObject.TryGetComponent<MeshRenderer>(out var renderer))
                            {
                                if (!renderer.enabled) renderer.enabled = true;
                            }
                        }
                    }
                }
            }

            if (patrol.RemainingDuration <= 0)
            {
                expiredKeys.Add(sentinelId);
            }
        }

        foreach (var key in expiredKeys)
        {
            if (_activePatrols.TryGetValue(key, out var activePatrol))
            {
                var sentinel = MiscUtils.PlayerById(key);
                if (sentinel?.Data?.Role is SentinelRole sentinelRole)
                {
                    sentinelRole.ClearPatrol();
                    
                    if (local != null && local.PlayerId == key)
                    {
                        var button = activePatrol.CachedButton;
                        if (button != null)
                        {
                            button.Timer = OptionGroupSingleton<SentinelOptions>.Instance.Cooldown;
                            if (button.Button != null)
                            {
                                var sr = button.Button.GetComponent<SpriteRenderer>();
                                if (sr != null) sr.color = Color.white;
                            }
                        }
                    }
                }
                _activePatrols.Remove(key);
            }
        }

        if (local == null) return;

        if (OptionGroupSingleton<SentinelOptions>.Instance.NotifyEvil)
        {
            CheckEntryNotifications(local);
        }
    }

    private static readonly Dictionary<byte, bool> _wasInsidePatrol = new();

    private static void CheckEntryNotifications(PlayerControl local)
    {
        if (local.IsImpostorAligned() || (local.Data?.Role is ITownOfUsRole touRole && touRole.RoleAlignment == RoleAlignment.NeutralKilling)) // Basic "evil" check
        {
            bool isInsideAny = false;
            foreach (var patrol in _activePatrols.Values)
            {
                float radius = OptionGroupSingleton<SentinelOptions>.Instance.Radius;
                float maxLightRadius = ShipStatus.Instance != null ? ShipStatus.Instance.MaxLightRadius : 1f;
                float effectiveRadius = radius * maxLightRadius;
                
                if (Vector2.Distance(local.transform.position, patrol.Position) <= effectiveRadius)
                {
                    isInsideAny = true;
                    break;
                }
            }

            bool wasInside = _wasInsidePatrol.GetValueOrDefault(local.PlayerId, false);
            if (isInsideAny && !wasInside)
            {
                // Just entered!
                HudManager.Instance.ShowPopUp(TouLocale.Get("SentinelPatrolWarning", "You have entered a Patrolled area!"));
            }
            _wasInsidePatrol[local.PlayerId] = isInsideAny;
        }
    }

    public static void Reset()
    {
        _activePatrols.Clear();
        _wasInsidePatrol.Clear();
    }
}
