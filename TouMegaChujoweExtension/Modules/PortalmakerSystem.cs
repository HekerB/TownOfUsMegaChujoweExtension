using HarmonyLib;
using InnerNet;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MiraAPI.GameOptions;
using TownOfUs.Assets;
using TownOfUs.Utilities;
using TownOfUs.Extensions;
using MiraAPI;
using MiraAPI.Roles;
using TownOfUs.Roles;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TownOfUs;
using MiraAPI.Utilities;

namespace TouMegaChujoweExtension.Modules;

public static class PortalmakerSystem
{
    public class ActivePortal
    {
        public Vector2 Position { get; set; }
        public GameObject? Visual { get; set; }
        public float CreationTime { get; set; }
    }

    public class PortalPair
    {
        public ActivePortal? PortalA;
        public ActivePortal? PortalB;
    }

    private static readonly Dictionary<byte, List<PortalPair>> PlayerPortalPairs = [];
    private static readonly Dictionary<byte, float> LastTeleportTime = [];
    private static readonly HashSet<byte> PlayersTeleporting = [];

    public static LobbyNotificationMessage? CooldownNotification;
    private static float LastCooldownNotificationTime = 0f;

    public static void Reset()
    {
        ClearAll();
    }

    public static float GetTeleportCooldownRemaining(byte playerId)
    {
        float tpCooldown = OptionGroupSingleton<PortalmakerOptions>.Instance.TeleportCooldown;
        if (LastTeleportTime.TryGetValue(playerId, out float lastTime))
        {
            float elapsed = Time.time - lastTime;
            if (elapsed < tpCooldown)
            {
                return tpCooldown - elapsed;
            }
        }
        return 0f;
    }

    public static void PlacePortal(byte ownerId, Vector2 position)
    {
        if (!PlayerPortalPairs.ContainsKey(ownerId))
            PlayerPortalPairs[ownerId] = [];

        var pairs = PlayerPortalPairs[ownerId];
        var opts = OptionGroupSingleton<PortalmakerOptions>.Instance;
        var radius = 0.5f;

        // Check if last pair is incomplete
        PortalPair? lastPair = pairs.LastOrDefault();
        if (lastPair != null && lastPair.PortalB == null)
        {
            // Set PortalB
            var newPortal = new ActivePortal
            {
                Position = position,
                CreationTime = Time.time
            };
            newPortal.Visual = CreatePortalVisual(position, radius, true); // Active bright purple
            lastPair.PortalB = newPortal;

            // Upgrade PortalA to active color and make it visible to everyone!
            if (lastPair.PortalA != null && lastPair.PortalA.Visual != null)
            {
                lastPair.PortalA.Visual.SetActive(true); // Show to everyone now that pair is complete
                var sr = lastPair.PortalA.Visual.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = Color.white; // Active original sprite colors
                    var local = PlayerControl.LocalPlayer;
                    bool isPortalmaker = local != null && local.Data != null && 
                        (local.GetRole<PortalmakerRole>() != null || local.Data.Role is PortalmakerRole);
                    sr.enabled = isPortalmaker || CanPlayerUsePortal(local);
                }
            }
        }
        else
        {
            float maxUses = opts.PortalUses;
            if (maxUses > 0)
            {
                int maxPairs = Mathf.CeilToInt(maxUses / 2f);
                if (pairs.Count >= maxPairs)
                {
                    var oldestPair = pairs[0];
                    if (oldestPair.PortalA != null && oldestPair.PortalA.Visual != null) UnityEngine.Object.Destroy(oldestPair.PortalA.Visual);
                    if (oldestPair.PortalB != null && oldestPair.PortalB.Visual != null) UnityEngine.Object.Destroy(oldestPair.PortalB.Visual);
                    pairs.RemoveAt(0);
                }
            }

            var newPortal = new ActivePortal
            {
                Position = position,
                CreationTime = Time.time
            };
            newPortal.Visual = CreatePortalVisual(position, radius, false); 

            bool isLocalOwner = PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == ownerId;
            if (!isLocalOwner && newPortal.Visual != null)
            {
                newPortal.Visual.SetActive(false);
            }
            
            var newPair = new PortalPair
            {
                PortalA = newPortal
            };
            pairs.Add(newPair);
        }
    }

    private static GameObject CreatePortalVisual(Vector2 position, float radius, bool isActive)
    {
        var go = new GameObject("PortalVisual");
        go.transform.position = new Vector3(position.x, position.y, position.y / 1000f + 0.05f);
        
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = TouExtensionCrewAssets.PortalSprite.LoadAsset();
        
        if (isActive)
        {
            renderer.color = Color.white;
        }
        else
        {
            renderer.color = new Color(1f, 1f, 1f, 0.4f);
        }
        
        go.transform.localScale = Vector3.one * (radius * 2.0f);
        
        var local = PlayerControl.LocalPlayer;
        bool isPortalmaker = local != null && local.Data != null && 
            (local.GetRole<PortalmakerRole>() != null || local.Data.Role is PortalmakerRole);
        renderer.enabled = isPortalmaker || (isActive && CanPlayerUsePortal(local));
        
        return go;
    }

    public static bool IsNearWall(Vector2 pos)
    {
        var cols = Physics2D.OverlapCircleAll(pos, 0.25f, Constants.ShipAndAllObjectsMask);
        foreach (var c in cols)
        {
            if (c != null && !c.isTrigger) return true;
        }
        return false;
    }

    private static void ShowCooldownNotification(PlayerControl player, float remaining)
    {
        if (!player.AmOwner) return;

        if (CooldownNotification == null)
        {
            CooldownNotification = Helpers.CreateAndShowNotification(
                $"<b>Portal Cooldown: {remaining:F1}s</b>",
                Color.red,
                new Vector3(0f, 1.2f, -20f),
                spr: TouExtensionCrewAssets.PortalSprite.LoadAsset());
            CooldownNotification.AdjustNotification();
        }
        else
        {
            CooldownNotification.Text.text = $"<b>Portal Cooldown: {remaining:F1}s</b>";
        }
        LastCooldownNotificationTime = Time.time;
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class HudManagerUpdatePatch
    {
        public static void Postfix()
        {
            if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return;
            if (PlayerControl.LocalPlayer == null || MeetingHud.Instance != null) return;

            var player = PlayerControl.LocalPlayer;
            if (player == null || player.Data == null || player.Data.IsDead) return;

            bool isNear = IsNearPortalPair(player);
            float cooldownRemaining = GetTeleportCooldownRemaining(player.PlayerId);

            var opts = OptionGroupSingleton<PortalmakerOptions>.Instance;
            if (opts.Mode == TeleportMode.Automatic)
            {
                if (isNear && cooldownRemaining > 0f)
                {
                    ShowCooldownNotification(player, cooldownRemaining);
                    return;
                }
                else
                {
                    if (CooldownNotification != null && Time.time - LastCooldownNotificationTime > 0.05f)
                    {
                        UnityEngine.Object.Destroy(CooldownNotification.gameObject);
                        CooldownNotification = null;
                    }
                }
            }
            else
            {
                if (CooldownNotification != null)
                {
                    UnityEngine.Object.Destroy(CooldownNotification.gameObject);
                    CooldownNotification = null;
                }
            }

            if (PlayersTeleporting.Contains(player.PlayerId)) return;

            if (cooldownRemaining > 0f) return;

            if (opts.Mode != TeleportMode.Automatic) return;

            if (!CanPlayerUsePortal(player)) return;

            var allPairs = PlayerPortalPairs.Values.SelectMany(x => x).ToList();
            foreach (var pair in allPairs)
            {
                if (pair.PortalA == null || pair.PortalB == null) continue;

                if (Vector2.Distance(player.GetTruePosition(), pair.PortalA.Position) <= radiusCheck())
                {
                    Reactor.Utilities.Coroutines.Start(CoTeleport(player, pair.PortalB.Position));
                    return;
                }

                if (Vector2.Distance(player.GetTruePosition(), pair.PortalB.Position) <= radiusCheck())
                {
                    Reactor.Utilities.Coroutines.Start(CoTeleport(player, pair.PortalA.Position));
                    return;
                }
            }
        }

        private static float radiusCheck() => 0.5f;
    }

    public static bool IsNearPortalPair(PlayerControl player)
    {
        if (player == null || !CanPlayerUsePortal(player)) return false;
        float radius = 0.5f;
        var allPairs = PlayerPortalPairs.Values.SelectMany(x => x).ToList();
        foreach (var pair in allPairs)
        {
            if (pair.PortalA == null || pair.PortalB == null) continue;
            if (Vector2.Distance(player.GetTruePosition(), pair.PortalA.Position) <= radius) return true;
            if (Vector2.Distance(player.GetTruePosition(), pair.PortalB.Position) <= radius) return true;
        }
        return false;
    }

    public static void TriggerTeleport(PlayerControl player)
    {
        if (player == null || MeetingHud.Instance != null || !CanPlayerUsePortal(player)) return;
        if (PlayersTeleporting.Contains(player.PlayerId)) return;

        float radius = 0.5f;
        float tpCooldown = OptionGroupSingleton<PortalmakerOptions>.Instance.TeleportCooldown;

        if (LastTeleportTime.TryGetValue(player.PlayerId, out float lastTime) && Time.time < lastTime + tpCooldown) return;

        var allPairs = PlayerPortalPairs.Values.SelectMany(x => x).ToList();
        foreach (var pair in allPairs)
        {
            if (pair.PortalA == null || pair.PortalB == null) continue;

            if (Vector2.Distance(player.GetTruePosition(), pair.PortalA.Position) <= radius)
            {
                Reactor.Utilities.Coroutines.Start(CoTeleport(player, pair.PortalB.Position));
                return;
            }

            if (Vector2.Distance(player.GetTruePosition(), pair.PortalB.Position) <= radius)
            {
                Reactor.Utilities.Coroutines.Start(CoTeleport(player, pair.PortalA.Position));
                return;
            }
        }
    }

    private static System.Collections.IEnumerator CoTeleport(PlayerControl player, Vector2 target)
    {
        byte playerId = player.PlayerId;
        PlayersTeleporting.Add(playerId);

        if (player.AmOwner)
        {
            SoundManager.Instance.PlaySound(TouAudio.ScientistIntroSound.LoadAsset(), false, 1f);
        }
        var notif = Helpers.CreateAndShowNotification(
            "<b>Teleporting...</b>",
            Color.cyan,
            new Vector3(0f, 1.2f, -20f),
            spr: TouExtensionCrewAssets.PortalSprite.LoadAsset());
        notif.AdjustNotification();

        float elapsed = 0f;
        float duration = 1.5f;
        while (elapsed < duration)
        {
            if (MeetingHud.Instance != null || player.HasDied())
            {
                PlayersTeleporting.Remove(playerId);
                yield break;
            }

            elapsed += Time.deltaTime;
            float remaining = Mathf.Max(0f, duration - elapsed);
            if (notif != null && notif.Text != null)
            {
                notif.Text.text = $"<b>Teleporting in {remaining:F1}s...</b>";
            }

            yield return null;
        }

        if (MeetingHud.Instance == null && !player.HasDied())
        {
            player.NetTransform.RpcSnapTo(target);
            player.transform.position = new Vector3(target.x, target.y, player.transform.position.z);
            LastTeleportTime[playerId] = Time.time;
            PirateDuelSystem.FlashScreen(new Color(TouExtensionColors.Portalmaker.r, TouExtensionColors.Portalmaker.g, TouExtensionColors.Portalmaker.b, 0.4f), 0.2f, 0.1f);
        }

        PlayersTeleporting.Remove(playerId);
    }

    public static void ClearPortals(byte ownerId)
    {
        if (PlayerPortalPairs.TryGetValue(ownerId, out var pairs))
        {
            foreach (var pair in pairs)
            {
                if (pair.PortalA != null && pair.PortalA.Visual != null) UnityEngine.Object.Destroy(pair.PortalA.Visual);
                if (pair.PortalB != null && pair.PortalB.Visual != null) UnityEngine.Object.Destroy(pair.PortalB.Visual);
            }
            PlayerPortalPairs.Remove(ownerId);
        }
        LastTeleportTime.Remove(ownerId);
        PlayersTeleporting.Remove(ownerId);
    }

    public static void ClearAll()
    {
        foreach (var pairs in PlayerPortalPairs.Values)
        {
            foreach (var pair in pairs)
            {
                if (pair.PortalA != null && pair.PortalA.Visual != null) UnityEngine.Object.Destroy(pair.PortalA.Visual);
                if (pair.PortalB != null && pair.PortalB.Visual != null) UnityEngine.Object.Destroy(pair.PortalB.Visual);
            }
        }
        PlayerPortalPairs.Clear();
        LastTeleportTime.Clear();
        PlayersTeleporting.Clear();

        if (CooldownNotification != null)
        {
            UnityEngine.Object.Destroy(CooldownNotification.gameObject);
            CooldownNotification = null;
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    public static class MeetingHudStartPatch
    {
        public static void Postfix()
        {
            if (CooldownNotification != null)
            {
                UnityEngine.Object.Destroy(CooldownNotification.gameObject);
                CooldownNotification = null;
            }
        }
    }

    public static bool CanPlayerUsePortal(PlayerControl player)
    {
        if (player == null || player.Data == null || player.Data.IsDead) return false;

        var opts = OptionGroupSingleton<PortalmakerOptions>.Instance;
        var permission = opts.WhoCanUse;

        if (permission == PortalUsageType.Everyone) return true;

        var role = player.Data.Role;
        if (role == null) return false;

        ModdedRoleTeams team;
        if (role is ITownOfUsRole touRole)
        {
            team = touRole.Team;
        }
        else
        {
            team = role.IsImpostor ? ModdedRoleTeams.Impostor : ModdedRoleTeams.Crewmate;
        }

        if (team == ModdedRoleTeams.Crewmate) return true;

        if (team == ModdedRoleTeams.Impostor)
        {
            return permission == PortalUsageType.Everyone || permission == PortalUsageType.CrewmateAndImpostor;
        }

        if (team == ModdedRoleTeams.Custom)
        {
            return permission == PortalUsageType.Everyone || permission == PortalUsageType.CrewmateAndNeutral;
        }

        return false;
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    public static class GameEndPatch
    {
        public static void Postfix() => Reset();
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
    public static class GameStartPatch
    {
        public static void Postfix() => Reset();
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
    public static class IntroImpostorStartPatch
    {
        public static void Postfix() => Reset();
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
    public static class IntroCrewmateStartPatch
    {
        public static void Postfix() => Reset();
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
    public static class LobbyStartPatch
    {
        public static void Postfix() => Reset();
    }
}
