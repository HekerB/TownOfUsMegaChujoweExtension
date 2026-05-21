using Reactor.Networking.Attributes;
using System.Collections.Generic;
using System.Linq;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using MiraAPI.Modifiers;
using TownOfUs;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Impostor;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public static class DumperSystem
{
    private static readonly Dictionary<byte, byte> DraggingBodies = new(); // DumperPlayerId -> BodyId (ParentId)
    private static readonly Dictionary<byte, float> AutoDumpTimes = new(); // DumperPlayerId -> AutoDumpTime
    private static readonly Dictionary<byte, DeadBody> DraggingBodyObjects = new(); // DumperPlayerId -> DeadBody object

    public static byte? GetDraggedBodyId(byte playerId)
    {
        return DraggingBodies.TryGetValue(playerId, out var bodyId) ? bodyId : null;
    }

    public static float? GetAutoDumpTime(byte playerId)
    {
        return AutoDumpTimes.TryGetValue(playerId, out var time) ? time : null;
    }

    public static void Update()
    {
        if (ShipStatus.Instance == null) return;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null) continue;

            if (player.Data.IsDead)
            {
                if (IsDragging(player.PlayerId))
                {
                    DropBody(player);
                }
                continue;
            }

            if (!player.IsRole<DumperRole>()) continue;

            var draggedBodyId = GetDraggedBodyId(player.PlayerId);
            var autoDumpTime = GetAutoDumpTime(player.PlayerId);

            if (draggedBodyId.HasValue && autoDumpTime.HasValue)
            {
                if (player.TryGetModifier<DragModifier>(out var dragMod))
                {
                    dragMod.SpeedFactor = 1.0f;
                }

                if (Time.time >= autoDumpTime.Value)
                {
                    if (player.AmOwner)
                    {
                        Info($"[Dumper] Auto-dumping body for {player.Data.PlayerName} after duration elapsed!");
                        TouMegaChujoweExtension.Roles.Impostor.DumperRole.RpcDropBody(player);
                    }
                }
            }
        }
    }

    public static void Reset()
    {
        foreach (var kvp in DraggingBodyObjects)
        {
            var body = kvp.Value;
            if (body != null && body.gameObject != null)
            {
                foreach (var r in body.GetComponentsInChildren<Renderer>(true))
                {
                    r.enabled = true;
                }
                foreach (var c in body.GetComponentsInChildren<Collider2D>(true))
                {
                    c.enabled = true;
                }
            }
        }
        DraggingBodyObjects.Clear();
        DraggingBodies.Clear();
        AutoDumpTimes.Clear();
    }

    public static bool IsDragging(byte playerId) 
    {
        return DraggingBodies.ContainsKey(playerId);
    }

    public static void PickupBody(PlayerControl player, byte bodyId)
    {
        if (!player.IsRole<DumperRole>()) return;

        // Delegate carrying, speed reduction, and networking synchronization to base mod's DragModifier
        // MUST do this first so that the modifier can successfully find and cache the DeadBody before we disable its components!
        var dragMod = new DragModifier(bodyId);
        dragMod.SpeedFactor = 1.0f; // No carry speed slowdown!
        player.GetModifierComponent()?.AddModifier(dragMod);

        // Find the DeadBody game object and make it invisible and unreportable by disabling renderers and colliders
        var body = Helpers.GetBodyById(bodyId);
        if (body != null)
        {
            foreach (var r in body.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = false;
            }
            foreach (var c in body.GetComponentsInChildren<Collider2D>(true))
            {
                c.enabled = false;
            }
            DraggingBodyObjects[player.PlayerId] = body;
        }

        DraggingBodies[player.PlayerId] = bodyId;
        var duration = OptionGroupSingleton<DumperOptions>.Instance.MaxDragDuration;
        AutoDumpTimes[player.PlayerId] = Time.time + duration;

        if (player.Data.Role is DumperRole role)
        {
            role.DraggingBodyId = bodyId;
            role.AutoDumpTime = Time.time + duration;
        }

        Info($"[Dumper] {player.Data.PlayerName} picked up body of {bodyId}");
    }

    public static void DropBody(PlayerControl player)
    {
        if (!player.IsRole<DumperRole>()) return;

        // Cleanly remove the DragModifier
        if (player.HasModifier<DragModifier>())
        {
            player.GetModifierComponent()?.RemoveModifier<DragModifier>();
        }

        DraggingBodies.Remove(player.PlayerId);
        AutoDumpTimes.Remove(player.PlayerId);

        if (player.Data.Role is DumperRole role)
        {
            role.DraggingBodyId = null;
            role.AutoDumpTime = null;
        }

        // Reactivate body and drop it at player's location
        if (DraggingBodyObjects.TryGetValue(player.PlayerId, out var body))
        {
            if (body != null && body.gameObject != null)
            {
                var dropPos = player.transform.position;
                dropPos.z = dropPos.y / 1000f;
                body.transform.position = dropPos;
                
                // Re-enable renderers and colliders
                foreach (var r in body.GetComponentsInChildren<Renderer>(true))
                {
                    r.enabled = true;
                }
                foreach (var c in body.GetComponentsInChildren<Collider2D>(true))
                {
                    c.enabled = true;
                }
            }
            DraggingBodyObjects.Remove(player.PlayerId);
        }

        if (player.AmOwner)
        {
            var instance = MiraAPI.Hud.CustomButtonSingleton<DumperDragButton>.Instance;
            if (instance != null)
            {
                instance.Timer = instance.Cooldown;
            }
        }

        Info($"[Dumper] {player.Data.PlayerName} dropped body");
    }
}
