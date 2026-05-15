using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using MiraAPI.GameOptions;
using TownOfUs.Assets;
using TownOfUs.Utilities;
using TownOfUs.Extensions;
using HarmonyLib;

namespace TouMegaChujoweExtension.Modules;

public static class SandwormSystem
{
    private static readonly Dictionary<byte, List<ActiveHole>> PlayerHoles = new();

    public class ActiveHole
    {
        public Vector2 Position;
        public GameObject? Visual;
    }

    public static void PlaceHole(byte ownerId, Vector2 position)
    {
        if (!PlayerHoles.ContainsKey(ownerId))
            PlayerHoles[ownerId] = new List<ActiveHole>();

        var holes = PlayerHoles[ownerId];

        var newHole = new ActiveHole
        {
            Position = position,
            Visual = CreateHoleVisual(position)
        };
        holes.Add(newHole);
        
        // If we have more than 2, maybe keep them all or just the last pair?
        // User said "ten vent będzie prowadził do miejsca gdzie Sandworm wyskoczył", 
        // suggesting a pair (entrance -> exit).
    }

    private static GameObject CreateHoleVisual(Vector2 position)
    {
        var go = new GameObject("SandwormHole");
        go.transform.position = new Vector3(position.x, position.y, position.y / 1000f + 0.01f);
        
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = TouRoleIcons.Miner.LoadAsset(); // Using Miner icon as placeholder for vent
        renderer.color = new Color(0.4f, 0.3f, 0.1f, 0.8f); // Sand color
        go.transform.localScale = Vector3.one * 0.7f;
        
        return go;
    }

    public static void Reset()
    {
        foreach (var list in PlayerHoles.Values)
        {
            foreach (var h in list)
            {
                if (h.Visual != null) UnityEngine.Object.Destroy(h.Visual);
            }
        }
        PlayerHoles.Clear();
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class HudManagerUpdatePatch
    {
        public static void Postfix()
        {
            if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started) return;
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data.IsDead) return;

            var player = PlayerControl.LocalPlayer;
            float radius = 0.5f;

            // Tunnel logic for holes
            foreach (var holes in PlayerHoles.Values)
            {
                if (holes.Count < 2) continue;

                // Simple pair-wise tunnel (1-2, 3-4, etc)
                for (int i = 0; i < holes.Count; i++)
                {
                    if (Vector2.Distance(player.GetTruePosition(), holes[i].Position) <= radius)
                    {
                        // Teleport to the paired hole if possible
                        int pairIndex = (i % 2 == 0) ? i + 1 : i - 1;
                        if (pairIndex >= 0 && pairIndex < holes.Count)
                        {
                            // Teleport if player interacts? Or just walk over? 
                            // User said "ten vent będzie prowadził", so probably standard vent behavior.
                            // For simplicity, I'll make it a teleport on walk-over with a small cooldown
                        }
                    }
                }
            }
        }
    }
}
