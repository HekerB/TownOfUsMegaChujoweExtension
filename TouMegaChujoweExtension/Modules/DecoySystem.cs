using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

public sealed class DecoyBodyComponent : MonoBehaviour
{
    public DecoyBodyComponent(IntPtr ptr) : base(ptr) {}

    public byte SwapperPlayerId;
    public byte AppearancePlayerId;
    public bool IsPoltergeist;
}

public static class DecoySystem
{
    public static readonly List<DeadBody> ActiveDecoys = [];

    public static void ClearDecoys()
    {
        ActiveDecoys.Clear();
    }

    [MethodRpc((uint)ExtensionRpc.DecoySpawn)]
    public static void RpcSpawnDecoy(PlayerControl creator, PlayerControl targetAppearance, Vector2 position, bool isPoltergeist)
    {
        if (creator == null || targetAppearance == null) return;

        try
        {
            var deadBody = UnityEngine.Object.Instantiate(GameManager.Instance.deadBodyPrefab[0]);
            if (deadBody == null) return;

            deadBody.ParentId = targetAppearance.PlayerId;
            deadBody.transform.position = new Vector3(position.x, position.y, position.y / 1000f);

            // Copy player cosmetics and colors
            foreach (var renderer in deadBody.bodyRenderers)
            {
                targetAppearance.SetPlayerMaterialColors(renderer);
            }
            targetAppearance.SetPlayerMaterialColors(deadBody.bloodSplatter);

            // Attach Decoy Component
            var comp = deadBody.gameObject.AddComponent<DecoyBodyComponent>();
            comp.SwapperPlayerId = creator.PlayerId;
            comp.AppearancePlayerId = targetAppearance.PlayerId;
            comp.IsPoltergeist = isPoltergeist;

            ActiveDecoys.Add(deadBody);
        }
        catch (System.Exception ex)
        {
            // Silent catch
        }
    }

    public static DeadBody? GetClosestDecoy(Vector2 pos, out float distance)
    {
        DeadBody? closest = null;
        distance = float.MaxValue;

        for (int i = ActiveDecoys.Count - 1; i >= 0; i--)
        {
            var body = ActiveDecoys[i];
            if (body == null || body.gameObject == null)
            {
                ActiveDecoys.RemoveAt(i);
                continue;
            }

            float dist = Vector2.Distance(body.transform.position, pos);
            if (dist < distance)
            {
                distance = dist;
                closest = body;
            }
        }

        return closest;
    }

    [MethodRpc((uint)ExtensionRpc.DecoySpring)]
    public static void RpcSpringDecoy(PlayerControl clicker, byte swapperId, bool isPoltergeist, Vector2 clickerPos)
    {
        if (clicker == null) return;

        try
        {
            // Find closest active decoy
            DeadBody? closestDecoy = null;
            float minDist = float.MaxValue;

            for (int i = ActiveDecoys.Count - 1; i >= 0; i--)
            {
                var body = ActiveDecoys[i];
                if (body == null || body.gameObject == null)
                {
                    ActiveDecoys.RemoveAt(i);
                    continue;
                }

                float dist = Vector2.Distance(body.transform.position, clickerPos);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestDecoy = body;
                }
            }

            if (closestDecoy != null)
            {
                ActiveDecoys.Remove(closestDecoy);
                UnityEngine.Object.Destroy(closestDecoy.gameObject);

                if (isPoltergeist)
                {
                    var poltergeist = MiscUtils.PlayerById(swapperId);
                    if (poltergeist?.Data?.Role is PoltergeistRole poltergeistRole)
                    {
                        poltergeistRole.DecoysReported++;
                        poltergeistRole.CheckWinConditions();
                    }

                    if (clicker.AmOwner)
                    {
                        TownOfUs.Assets.TouAudio.PlaySound(TownOfUs.Assets.TouAudio.DiscoveredSound);
                        MiraAPI.Utilities.Helpers.CreateAndShowNotification(
                            "You reported a Poltergeist Decoy Trap!",
                            Color.red,
                            new Vector3(0f, 1f, -20f)
                        )?.AdjustNotification();
                    }
                }
                else
                {
                    var swapper = MiscUtils.PlayerById(swapperId);
                    if (swapper != null && !clicker.HasDied())
                    {
                        swapper.RpcCustomMurder(clicker);

                        var targetPos = clickerPos;
                        swapper.transform.position = targetPos;
                        swapper.MyPhysics.ResetMoveState();
                        swapper.NetTransform.SnapTo(targetPos);
                        if (swapper.AmOwner)
                        {
                            swapper.NetTransform.RpcSnapTo(targetPos);
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            // Silent catch
        }
    }
}
