using System.Collections.Generic;
using System.Linq;
using TouMegaChujoweExtension.Roles.Impostor;
using TouMegaChujoweExtension.Networking;
using TownOfUs.Utilities;
using TownOfUs.Extensions;
using UnityEngine;
using Reactor.Networking.Attributes;
using MiraAPI.GameOptions;

namespace TouMegaChujoweExtension.Modules;

public static class DumperSystem
{
    private static readonly Dictionary<byte, DeadBody> DraggedBodies = new();
    private static readonly Dictionary<byte, float> DragTimers = new();
    public static readonly HashSet<byte> MyKills = new();

    public static bool IsDragging(byte playerId) => DraggedBodies.ContainsKey(playerId);
    public static float GetDragTimer(byte playerId) => DragTimers.GetValueOrDefault(playerId, 0f);

    public static void Reset()
    {
        foreach (var body in DraggedBodies.Values)
        {
            if (body != null)
            {
                SetBodyVisibility(body, true);
            }
        }
        DraggedBodies.Clear();
        DragTimers.Clear();
        MyKills.Clear();
    }

    [MethodRpc((uint)ExtensionRpc.DumperPickupBody)]
    public static void RpcPickupBody(PlayerControl dumper, byte bodyId)
    {
        var body = UnityEngine.Object.FindObjectsOfType<DeadBody>().FirstOrDefault(b => b.ParentId == bodyId);
        if (body == null) return;

        DraggedBodies[dumper.PlayerId] = body;
        DragTimers[dumper.PlayerId] = OptionGroupSingleton<Options.Roles.Impostor.DumperOptions>.Instance.MaxDragDuration;
        SetBodyVisibility(body, false);
    }

    [MethodRpc((uint)ExtensionRpc.DumperDropBody)]
    public static void RpcDropBody(PlayerControl dumper)
    {
        if (dumper == null) return;
        if (DraggedBodies.TryGetValue(dumper.PlayerId, out var body))
        {
            SetBodyVisibility(body, true);
            body.transform.position = dumper.transform.position;
            DraggedBodies.Remove(dumper.PlayerId);
            DragTimers.Remove(dumper.PlayerId);
        }
    }

    private static void SetBodyVisibility(DeadBody body, bool visible)
    {
        var renderers = body.GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers) r.enabled = visible;
        
        var collider = body.GetComponent<Collider2D>();
        if (collider != null) collider.enabled = visible;
    }

    public static void Update()
    {
        var toDrop = new List<byte>();
        foreach (var kvp in DraggedBodies)
        {
            var dumperId = kvp.Key;
            var body = kvp.Value;
            var dumper = MiscUtils.PlayerById(dumperId);
            
            if (dumper == null || dumper.HasDied())
            {
                toDrop.Add(dumperId);
                continue;
            }

            if (DragTimers.TryGetValue(dumperId, out var timer))
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    toDrop.Add(dumperId);
                }
                else
                {
                    DragTimers[dumperId] = timer;
                }
            }

            if (body != null && dumper != null)
            {
                body.transform.position = dumper.transform.position;
            }
        }

        foreach (var dId in toDrop)
        {
            RpcDropBody(MiscUtils.PlayerById(dId));
        }
    }
}
