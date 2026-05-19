using HarmonyLib;
using InnerNet;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
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
using Reactor.Utilities.Extensions;
using MiraAPI.Utilities;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using TownOfUs.Buttons;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Modifiers.Types;
using TouMegaChujoweExtension.Assets;

namespace TouMegaChujoweExtension.Modules;

public static class GardenerSystem
{
    private static readonly Dictionary<byte, ActiveGarden> ActiveGardens = [];
    private static float _lastCleanupTime;

    private static readonly List<AttackLog> PendingAttackLogs = [];

    public class AttackLog
    {
        public byte OwnerId { get; set; }
        public byte AttackerId { get; set; }
        public byte TargetId { get; set; }
        public bool Killed { get; set; }
    }

    public static void RemoveGarden(byte ownerId)
    {
        if (ActiveGardens.TryGetValue(ownerId, out var garden))
        {
            if (garden.Visual != null) UnityEngine.Object.Destroy(garden.Visual);
            ActiveGardens.Remove(ownerId);
        }
    }

    public class ActiveGarden
    {
        public Vector2 Position { get; set; }
        public float Radius { get; set; }
        public float RemainingTime { get; set; }
        public byte OwnerId { get; set; }
        public GameObject? Visual { get; set; }
    }

    public static bool IsInAnyGarden(PlayerControl? player)
    {
        if (player == null || player.Data == null || player.Data.IsDead) return false;
        if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return false;

        foreach (var garden in ActiveGardens.Values)
        {
            if (garden == null) continue;
            
            var pos = player.GetTruePosition();
            if (Vector2.Distance(pos, garden.Position) <= garden.Radius)
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

        GameObject? visual = null;
        if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == ownerId)
        {
            visual = CreateGardenVisual(position, radius);
        }

        ActiveGardens[ownerId] = new ActiveGarden
        {
            Position = position,
            Radius = radius,
            RemainingTime = duration,
            OwnerId = ownerId,
            Visual = visual
        };
    }

    private static GameObject CreateGardenVisual(Vector2 position, float radius)
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Garden Visual";

        // Set position with a slight Z and Y sorting offset
        sphere.transform.position = new Vector3(position.x, position.y, position.y / 1000f + 0.005f);

        // Scale perfectly matching the actual radius (diameter = radius * 2)
        sphere.transform.localScale = new Vector3(radius * 2f, radius * 2f, radius * 2f);

        // Remove the collider so players don't bump into it
        var collider = sphere.GetComponent<SphereCollider>();
        if (collider != null)
        {
            UnityEngine.Object.Destroy(collider);
        }

        // Apply Gardener's custom transparent material
        var meshRenderer = sphere.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            try
            {
                meshRenderer.material = TouExtensionAnims.GardenerMaterial;
            }
            catch { /* fallback */ }
        }

        return sphere;
    }

    public static void ClearAll()
    {
        PendingAttackLogs.Clear();
        foreach (var visual in ActiveGardens.Values.Select(garden => garden.Visual).Where(visual => visual != null))
        {
            UnityEngine.Object.Destroy(visual);
        }
        ActiveGardens.Clear();
        _lastCleanupTime = Time.time;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.HasModifier<GardenerProtectedModifier>())
            {
                player.RemoveModifier<GardenerProtectedModifier>();
            }
        }
    }

    public static void RecordAttackLog(byte ownerId, byte attackerId, byte targetId, bool killed)
    {
        PendingAttackLogs.Add(new AttackLog { OwnerId = ownerId, AttackerId = attackerId, TargetId = targetId, Killed = killed });
    }

    public static void HandleAttackNotification(byte ownerId, byte attackerId, byte targetId, bool killed, bool delayed = false)
    {
        if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == ownerId)
        {
            var attacker = MiscUtils.PlayerById(attackerId);
            var target = MiscUtils.PlayerById(targetId);
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
            string targetName = target?.Data?.PlayerName ?? "someone";
            var options = OptionGroupSingleton<GardenerOptions>.Instance;

            string msg;
            if (options.Feedback)
            {
                msg = killed
                   ? TouLocale.GetParsed("ExtensionGardenerAttackKilledFeedback", $"{attackerRole} killed {targetName} in your garden!").Replace("{0}", attackerRole).Replace("{1}", targetName)
                   : TouLocale.GetParsed("ExtensionGardenerAttackBlockedFeedback", $"{attackerRole} tried to attack {targetName} in your garden!").Replace("{0}", attackerRole).Replace("{1}", targetName);
            }
            else
            {
                msg = killed
                   ? TouLocale.Get("ExtensionGardenerAttackKilled", "Someone was killed in your garden!")
                   : TouLocale.Get("ExtensionGardenerAttackBlocked", "An attack was blocked in your garden!");
            }

            if (delayed)
            {
                var title = $"<color=#{TouExtensionColors.Gardener.ToHtmlStringRGBA()}>{TouLocale.Get("ExtensionRoleGardenerFeedbackTitle", "Gardener Feedback")}</color>";
                MiscUtils.AddFakeChat(PlayerControl.LocalPlayer.Data, title, msg, false, true);
            }
            else
            {
                var notif = MiraAPI.Utilities.Helpers.CreateAndShowNotification(
                    $"<b><color=#{(killed ? "FF0000" : "00FF00")}>{msg}</color></b>",
                    Color.white,
                    new Vector3(0f, 1f, -20f),
                    spr: TouExtensionCrewAssets.GardenerButtonSprite.LoadAsset());
                notif.AdjustNotification();

                Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(killed ? Color.red : Color.green));
            }
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

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
    public static class IntroImpostorStartPatch
    {
        public static void Postfix() => ClearAll();
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
    public static class IntroCrewmateStartPatch
    {
        public static void Postfix() => ClearAll();
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
    public static class LobbyStartPatch
    {
        public static void Postfix() => ClearAll();
    }

    [RegisterEvent]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        try
        {
            if (@event.Source == null || @event.Target == null) return;

            bool targetInGarden = IsInAnyGarden(@event.Target);

            if (targetInGarden)
            {
                @event.Cancel();

                if (@event.Source.AmOwner)
                {
                    @event.Source.SetKillTimer(@event.Source.GetKillCooldown());

                    if (HudManager.Instance != null && HudManager.Instance.KillButton != null)
                    {
                        HudManager.Instance.KillButton.SetTarget(null);
                    }

                    foreach (var button in MiraAPI.Hud.CustomButtonManager.Buttons.Where(button => button != null && button.Enabled(@event.Source.Data.Role) && button is IKillButton))
                    {
                        button.Timer = button.Cooldown;
                    }

                    var garden = ActiveGardens.Values.FirstOrDefault(g =>
                        g.RemainingTime > 0 &&
                        Vector2.Distance(@event.Target.GetTruePosition(), g.Position) <= g.Radius);

                    if (garden != null)
                    {
                        var owner = MiscUtils.PlayerById(garden.OwnerId);
                        if (owner != null)
                        {
                            GardenerRole.RpcGardenerAttackNotify(owner, @event.Source.PlayerId, @event.Target.PlayerId, false);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[TOUMCE] Exception in Gardener BeforeMurderEventHandler: {ex}");
        }
    }

    [RegisterEvent]
    public static void OnMeetingStart(MiraAPI.Events.Vanilla.Meeting.StartMeetingEvent _)
    {
        if (PendingAttackLogs.Count == 0) return;

        foreach (var log in PendingAttackLogs.Where(l => l.OwnerId == PlayerControl.LocalPlayer?.PlayerId))
        {
            HandleAttackNotification(log.OwnerId, log.AttackerId, log.TargetId, log.Killed, true);
        }
        PendingAttackLogs.Clear();
    }


    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class HudManagerUpdatePatch
    {
        public static void Postfix()
        {
            if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return;

            // Only run cleanup every 0.1s - light on CPU
            if (Time.time - _lastCleanupTime >= 0.1f)
            {
                var expired = new List<byte>();
                float dt = Time.time - _lastCleanupTime;
                foreach (var kvp in ActiveGardens)
                {
                    kvp.Value.RemainingTime -= dt;
                    if (kvp.Value.RemainingTime <= 0)
                    {
                        expired.Add(kvp.Key);
                    }
                }

                _lastCleanupTime = Time.time;

                foreach (var key in expired)
                {
                    if (ActiveGardens.TryGetValue(key, out var garden))
                    {
                        if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == key)
                        {
                            GardenerRole.RpcClearGarden(PlayerControl.LocalPlayer, key);
                        }

                        if (garden.Visual != null) UnityEngine.Object.Destroy(garden.Visual);
                        ActiveGardens.Remove(key);
                    }
                }

                var aliveGardenerIds = PlayerControl.AllPlayerControls.ToArray()
                    .Where(p => p != null && p.Data != null && !p.Data.IsDead && p.Data.Role is GardenerRole)
                    .Select(p => p.PlayerId)
                    .ToHashSet();

                var ownersToRemove = ActiveGardens.Keys.Where(id => !aliveGardenerIds.Contains(id)).ToList();
                foreach (var id in ownersToRemove)
                {
                    if (ActiveGardens.TryGetValue(id, out var g))
                    {
                        if (g.Visual != null) UnityEngine.Object.Destroy(g.Visual);
                        ActiveGardens.Remove(id);
                    }
                }
            }
        }
    }
}