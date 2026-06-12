using TownOfUs.Utilities;
using UnityEngine;
using MiraAPI.Hud;

namespace TouMegaChujoweExtension.Modules;

public static class SniperSystem
{
    public static bool IsAiming { get; set; }
    public static int StartAimingFrame { get; set; } = -1;

    public static bool IsPlayerFrozen(byte playerId)
    {
        if (IsAiming && PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == playerId)
        {
            return true;
        }
        return false;
    }

    public static void Update()
    {
        var localPlayer = PlayerControl.LocalPlayer;

        if (IsAiming)
        {
            if (localPlayer == null || localPlayer.Data.IsDead || MeetingHud.Instance != null)
            {
                var btn = CustomButtonSingleton<SniperShootButton>.Instance;
                if (btn != null)
                {
                    btn.ResetCooldownAndOrEffect();
                }
                else
                {
                    IsAiming = false;
                }
            }
        }

        if (IsAiming && localPlayer != null && !localPlayer.Data.IsDead)
        {
            if (Time.frameCount > StartAimingFrame && Input.GetMouseButtonDown(0))
            {
                if (Camera.main != null)
                {
                    var mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    PlayerControl? clickedTarget = null;
                    var minClickDist = 0.8f;

                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc == null || pc.Data.IsDead || pc.PlayerId == localPlayer.PlayerId) continue;
                        if (pc.IsImpostorAligned()) continue;
                        if (PelicanSystem.IsSwallowed(pc.PlayerId)) continue;

                        var distToClick = Vector2.Distance(mouseWorldPos, pc.transform.position);
                        if (distToClick < minClickDist)
                        {
                            clickedTarget = pc;
                            break;
                        }
                    }

                    if (clickedTarget != null)
                    {
                        var btn = CustomButtonSingleton<SniperShootButton>.Instance;

                        if (PoisonSystem.CheckAndTriggerShields(localPlayer, clickedTarget))
                        {

                            btn?.EndAiming(true);
                        }
                        else
                        {
                            SniperRole.RpcSniperShoot(localPlayer, clickedTarget.PlayerId);
                            SniperRole.RpcSniperPlaySound(localPlayer, clickedTarget.PlayerId);
                            btn?.EndAiming(true);
                        }
                    }
                }
            }
        }
    }

    public static void RoundReset()
    {
        IsAiming = false;
        StartAimingFrame = -1;
    }
}
