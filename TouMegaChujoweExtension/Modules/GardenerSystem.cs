using HarmonyLib;
using InnerNet;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Crewmate;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TownOfUs.Assets;
using TownOfUs.Utilities;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using TownOfUs.Modules.Localization;
using TownOfUs.Options;
using TownOfUs.Extensions;
using MiraAPI.Utilities;

namespace TouMegaChujoweExtension.Modules;

public static class GardenerSystem
{
    private static readonly Dictionary<byte, ActiveGarden> ActiveGardens = [];

    public class ActiveGarden
    {
        public Vector2 Position { get; set; }
        public float Radius { get; set; }
        public float EndTime { get; set; }
        public byte OwnerId { get; set; }
        public GameObject? Visual { get; set; }
    }

    public static bool IsInAnyGarden(PlayerControl? player)
    {
        if (player == null || player.Data == null || player.Data.IsDead) return false;
        
        float currentTime = Time.time;
        foreach (var garden in ActiveGardens.Values)
        {
            if (currentTime > garden.EndTime) continue;

            if (Vector2.Distance(player.GetTruePosition(), garden.Position) <= garden.Radius)
            {
                return true;
            }
        }
        return false;
    }

    public static void SetGarden(byte ownerId, Vector2 position, float radius, float duration)
    {
        if (ActiveGardens.TryGetValue(ownerId, out var oldGarden) && oldGarden.Visual != null)
        {
            UnityEngine.Object.Destroy(oldGarden.Visual);
        }

        ActiveGardens[ownerId] = new ActiveGarden
        {
            Position = position,
            Radius = radius,
            EndTime = Time.time + duration,
            OwnerId = ownerId,
            Visual = CreateGardenVisual(position, radius)
        };
    }

    private static GameObject CreateGardenVisual(Vector2 position, float radius)
    {
        var go = new GameObject("GardenVisual");
        go.transform.position = new Vector3(position.x, position.y, position.y / 1000f + 0.1f);

        var renderer = go.AddComponent<SpriteRenderer>();
        var sprite = TouAssets.AbilityCounterBasicSprite?.LoadAsset();
        if (sprite != null) renderer.sprite = sprite;
        renderer.color = new Color(0.2f, 1f, 0.2f, 0.35f); // More visible semi-transparent green

        go.transform.localScale = Vector3.one * (radius * 2f);

        // Visible to everyone to represent the "Safe Zone"
        return go;
    }

    public static void ClearAll()
    {
        foreach (var visual in ActiveGardens.Values.Select(garden => garden.Visual).Where(visual => visual != null))
        {
            UnityEngine.Object.Destroy(visual);
        }
        ActiveGardens.Clear();
    }

    public static void HandleAttackNotification(byte ownerId, byte attackerId, bool killed)
    {
        if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == ownerId)
        {
            var attacker = MiscUtils.PlayerById(attackerId);
            var role = attacker?.Data?.Role;
            string attackerRole = "Someone";
            if (role != null)
            {
                attackerRole = role.GetRoleName();
                if (string.IsNullOrWhiteSpace(attackerRole) || attackerRole == "Unknown")
                {
                    attackerRole = role.GetType().Name.Replace("Role", "").Replace("RoleBehaviour", "");
                }
            }
            var options = OptionGroupSingleton<GardenerOptions>.Instance;

            string msg;
            if (options.Feedback)
            {
                msg = killed
                   ? TouLocale.GetParsed("ExtensionGardenerAttackKilledFeedback", $"{attackerRole} killed someone in your garden!").Replace("{0}", attackerRole)
                   : TouLocale.GetParsed("ExtensionGardenerAttackBlockedFeedback", $"An attack by {attackerRole} was blocked in your garden!").Replace("{0}", attackerRole);
            }
            else
            {
                msg = killed
                   ? TouLocale.Get("ExtensionGardenerAttackKilled", "Someone was killed in your garden!")
                   : TouLocale.Get("ExtensionGardenerAttackBlocked", "An attack was blocked in your garden!");
            }


            var notif = MiraAPI.Utilities.Helpers.CreateAndShowNotification(
                $"<b><color=#{(killed ? "FF0000" : "00FF00")}>{msg}</color></b>",
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouRoleIcons.Traitor.LoadAsset());
            notif.AdjustNotification();

            Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(killed ? Color.red : Color.green));
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    public static class GameEndPatch
    {
        public static void Postfix() => ClearAll();
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
    public static class GameStartPatch
    {
        public static void Postfix() => ClearAll();
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
    public static class CheckMurderProtectionPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (target == null) return true;
            if (OptionGroupSingleton<GardenerOptions>.Instance.CanKillInGarden) return true;

            if (IsInAnyGarden(__instance) || IsInAnyGarden(target))
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    public static class MurderPlayerProtectionPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (target == null) return true;

            var options = OptionGroupSingleton<GardenerOptions>.Instance;

            // Protect if either killer or target is in any garden
            bool killerInGarden = IsInAnyGarden(__instance);
            bool targetInGarden = IsInAnyGarden(target);
            
            if (killerInGarden || targetInGarden)
            {
                if (__instance.AmOwner && !options.CanKillInGarden)
                {
                    __instance.SetKillTimer(__instance.GetKillCooldown());
                    
                    // Notify through the garden that blocked it
                    var garden = ActiveGardens.Values.FirstOrDefault(g => 
                        Vector2.Distance(target.GetTruePosition(), g.Position) <= g.Radius || 
                        Vector2.Distance(__instance.GetTruePosition(), g.Position) <= g.Radius);
                    
                    if (garden != null)
                    {
                        GardenerRole.RpcGardenerAttackNotify(garden.OwnerId, __instance.PlayerId, false);
                    }
                }
                return options.CanKillInGarden;
            }
            
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
    public static class DieProtectionPatch
    {
        public static bool Prefix(PlayerControl __instance)
        {
            // Only protect against active gameplay kills, not meetings/exiles
            if (MeetingHud.Instance != null || ExileController.Instance != null) return true;
            if (OptionGroupSingleton<GardenerOptions>.Instance.CanKillInGarden) return true;

            if (IsInAnyGarden(__instance))
            {
                // Last line of defense for skills that bypass MurderPlayer/CheckMurder
                return false;
            }
            return true;
        }
    }


    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class HudManagerUpdatePatch
    {
        public static void Postfix(HudManager __instance)
        {
            if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return;
            
            // Clean up expired gardens
            var expired = ActiveGardens.Where(kvp => Time.time > kvp.Value.EndTime).Select(kvp => kvp.Key).ToList();
            foreach (var key in expired)
            {
                if (ActiveGardens.TryGetValue(key, out var garden) && garden.Visual != null)
                {
                    UnityEngine.Object.Destroy(garden.Visual);
                }
                ActiveGardens.Remove(key);
            }

            // Cleanup gardens of players who left
            var allPlayerIds = PlayerControl.AllPlayerControls.ToArray().Select(p => p.PlayerId).ToHashSet();
            var ownersToRemove = ActiveGardens.Keys.Where(id => !allPlayerIds.Contains(id)).ToList();
            foreach (var id in ownersToRemove)
            {
                if (ActiveGardens.TryGetValue(id, out var g))
                {
                    if (g.Visual != null) UnityEngine.Object.Destroy(g.Visual);
                    ActiveGardens.Remove(id);
                }
            }

            // Kill button feedback
            var killButton = __instance.KillButton;
            if (killButton != null && PlayerControl.LocalPlayer != null && !PlayerControl.LocalPlayer.Data.IsDead)
            {
                bool killerInGarden = IsInAnyGarden(PlayerControl.LocalPlayer);
                bool targetInGarden = killButton.currentTarget != null && IsInAnyGarden(killButton.currentTarget);

                if (killerInGarden || targetInGarden)
                {
                    killButton.SetDisabled();
                    if (killButton.graphic != null) killButton.graphic.color = Color.gray;
                }
                else
                {
                    // Restore color if not in garden
                    if (killButton.graphic != null && killButton.graphic.color == Color.gray)
                    {
                        killButton.graphic.color = Color.white;
                    }
                }
            }
        }
    }
}
