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
using TownOfUs;

namespace TouMegaChujoweExtension.Modules;

public static class PortalmakerSystem
{
    private static readonly Dictionary<byte, List<ActivePortal>> PlayerPortals = [];
    private static readonly Dictionary<byte, float> LastTeleportTime = [];

    public class ActivePortal
    {
        public Vector2 Position { get; set; }
        public GameObject? Visual { get; set; }
        public float CreationTime { get; set; }
    }

    public static void Reset()
    {
        ClearAll();
    }

    public static void PlacePortal(byte ownerId, Vector2 position)
    {
        if (!PlayerPortals.ContainsKey(ownerId))
            PlayerPortals[ownerId] = [];

        var portals = PlayerPortals[ownerId];
        var radius = OptionGroupSingleton<PortalmakerOptions>.Instance.PortalRadius;

        // Remove oldest if we already have 2
        if (portals.Count >= 2)
        {
            var oldest = portals[0];
            if (oldest.Visual != null) UnityEngine.Object.Destroy(oldest.Visual);
            portals.RemoveAt(0);
        }

        var newPortal = new ActivePortal
        {
            Position = position,
            Visual = CreatePortalVisual(position, radius),
            CreationTime = Time.time
        };
        portals.Add(newPortal);
    }

    private static GameObject CreatePortalVisual(Vector2 position, float radius)
    {
        var go = new GameObject("PortalVisual");
        go.transform.position = new Vector3(position.x, position.y, position.y / 1000f + 0.05f);
        
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = TouExtensionCrewAssets.PortalSprite.LoadAsset();
        renderer.color = new Color(0.6f, 0.2f, 1f, 0.6f); // Bright purple portal
        
        // Scale the visual to match the actual radius (roughly)
        // VentSprite is about 1x1, so scale should be radius * 2
        go.transform.localScale = Vector3.one * (radius * 2.0f);
        
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

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class HudManagerUpdatePatch
    {
        public static void Postfix()
        {
            if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return;
            if (PlayerControl.LocalPlayer == null || MeetingHud.Instance != null) return;

            var player = PlayerControl.LocalPlayer;
            if (player == null || player.Data == null || player.Data.IsDead) return;

            float tpCooldown = OptionGroupSingleton<PortalmakerOptions>.Instance.TeleportCooldown;
            float radius = OptionGroupSingleton<PortalmakerOptions>.Instance.PortalRadius;
            float duration = OptionGroupSingleton<PortalmakerOptions>.Instance.PortalDuration;

            // Handle portal expiration (thread-safe iteration)
            var owners = PlayerPortals.Keys.ToList();
            foreach (var ownerId in owners)
            {
                if (!PlayerPortals.TryGetValue(ownerId, out var portals)) continue;
                
                for (int i = portals.Count - 1; i >= 0; i--)
                {
                    if (duration > 0f && Time.time > portals[i].CreationTime + duration)
                    {
                        if (portals[i].Visual != null) UnityEngine.Object.Destroy(portals[i].Visual);
                        portals.RemoveAt(i);
                    }
                }
            }

            // Check teleport cooldown
            if (LastTeleportTime.TryGetValue(player.PlayerId, out float lastTime) && Time.time < lastTime + tpCooldown)
            {
                return;
            }

            var opts = OptionGroupSingleton<PortalmakerOptions>.Instance;
            var mode = opts.Mode;
            if (mode != TeleportMode.Automatic) return;

            if (!CanPlayerUsePortal(player)) return;

            var allPortals = PlayerPortals.Values.ToList();
            foreach (var portals in allPortals)
            {
                if (portals.Count != 2) continue;

                for (int i = 0; i < 2; i++)
                {
                    if (Vector2.Distance(player.GetTruePosition(), portals[i].Position) <= radius)
                    {
                        // Teleport to the OTHER portal
                        var target = portals[1 - i].Position;
                        
                        // Use RpcSnapTo for better networking sync
                        player.NetTransform.RpcSnapTo(target);
                        player.transform.position = new Vector3(target.x, target.y, player.transform.position.z);
                        LastTeleportTime[player.PlayerId] = Time.time;
                        
                        // Visual feedback
                        PirateDuelSystem.FlashScreen(new Color(0.5f, 0f, 1f), 0.2f, 0.1f);
                        return;
                    }
                }
            }
        }
    }

    public static bool IsNearPortalPair(PlayerControl player)
    {
        if (player == null || !CanPlayerUsePortal(player)) return false;
        float radius = OptionGroupSingleton<PortalmakerOptions>.Instance.PortalRadius;
        var allPortals = PlayerPortals.Values.ToList();
        foreach (var portals in allPortals)
        {
            if (portals.Count != 2) continue;
            if (portals.Any(portal => Vector2.Distance(player.GetTruePosition(), portal.Position) <= radius)) return true;
        }
        return false;
    }

    public static void TriggerTeleport(PlayerControl player)
    {
        if (player == null || MeetingHud.Instance != null || !CanPlayerUsePortal(player)) return;
        float radius = OptionGroupSingleton<PortalmakerOptions>.Instance.PortalRadius;
        float tpCooldown = OptionGroupSingleton<PortalmakerOptions>.Instance.TeleportCooldown;

        if (LastTeleportTime.TryGetValue(player.PlayerId, out float lastTime) && Time.time < lastTime + tpCooldown) return;

        var allPortals = PlayerPortals.Values.ToList();
        foreach (var portals in allPortals)
        {
            if (portals.Count != 2) continue;
            for (int i = 0; i < 2; i++)
            {
                if (Vector2.Distance(player.GetTruePosition(), portals[i].Position) <= radius)
                {
                    var target = portals[1 - i].Position;
                    player.NetTransform.RpcSnapTo(target);
                    player.transform.position = new Vector3(target.x, target.y, player.transform.position.z);
                    LastTeleportTime[player.PlayerId] = Time.time;
                    PirateDuelSystem.FlashScreen(new Color(0.5f, 0f, 1f), 0.2f, 0.1f);
                    return;
                }
            }
        }
    }


    public static void ClearPortals(byte ownerId)
    {
        if (PlayerPortals.TryGetValue(ownerId, out var portals))
        {
            foreach (var visual in portals.Select(p => p.Visual).Where(v => v != null))
            {
                UnityEngine.Object.Destroy(visual);
            }
            PlayerPortals.Remove(ownerId);
        }
        LastTeleportTime.Remove(ownerId);
    }

    public static void ClearAll()
    {
        foreach (var portals in PlayerPortals.Values)
        {
            foreach (var visual in portals.Select(p => p.Visual).Where(v => v != null))
            {
                UnityEngine.Object.Destroy(visual);
            }
        }
        PlayerPortals.Clear();
        LastTeleportTime.Clear();
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    public static class MeetingHudStartPatch
    {
        public static void Postfix()
        {
            if (!OptionGroupSingleton<PortalmakerOptions>.Instance.StayAfterMeeting)
            {
                ClearAll();
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
