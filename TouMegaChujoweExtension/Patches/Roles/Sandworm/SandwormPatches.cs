using HarmonyLib;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Modules;
using MiraAPI.GameOptions;
using MiraAPI.Events.Vanilla.Usables;
using TownOfUs;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using UnityEngine;
using TownOfUs.Modifiers.Crewmate;

namespace TouMegaChujoweExtension.Patches.Impostor;

[HarmonyPatch]
public static class SandwormPatches
{
    [HarmonyPatch(typeof(TownOfUs.Events.TownOfUsEventHandlers), nameof(TownOfUs.Events.TownOfUsEventHandlers.PlayerCanUseEventHandler))]
    [HarmonyPrefix]
    public static bool PlayerCanUseEventHandlerPrefix(PlayerCanUseEvent @event)
    {
        if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.IsRole<SandwormRole>())
        {
            if (@event.IsVent)
            {
                if ((HudManager.Instance != null && HudManager.Instance.Chat.IsOpenOrOpening) || MeetingHud.Instance)
                {
                    @event.Cancel();
                }
                return false; // Skip TownOfUs's PlayerCanUseEventHandler to bypass vent disable in 1v1
            }
        }
        return true;
    }
    [HarmonyPatch(typeof(LogicOptions), nameof(LogicOptions.GetPlayerSpeedMod))]
    [HarmonyPostfix]
    public static void GetPlayerSpeedModPostfix(PlayerControl pc, ref float __result)
    {
        if (pc != null && pc.IsRole<SandwormRole>())
        {
            var role = pc.GetRole<SandwormRole>();
            if (role != null && role.IsUnderground)
            {
                __result *= OptionGroupSingleton<SandwormOptions>.Instance.UndergroundSpeed;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
    [HarmonyPrefix]
    public static bool PlayerPhysicsFixedUpdatePrefix(PlayerPhysics __instance)
    {
        if (__instance == null || __instance.myPlayer == null) return true;
        
        var player = __instance.myPlayer;
        if (player.IsRole<SandwormRole>())
        {
            var role = player.GetRole<SandwormRole>();
            if (role != null && role.IsUnderground)
            {


                if (player.AmOwner)
                {
                    // Manually read keyboard input and move the player underground, bypassing vanilla locks!
                    var dir = AdvancedMovementUtilities.GetRegularDirection();
                    AdvancedMovementUtilities.ApplyControlledMovement(__instance, dir, stopIfZero: true);
                }

                return false; // Skip the vanilla FixedUpdate to bypass vent movement lock completely!
            }
        }

        return true;
    }



    [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
    [HarmonyPostfix]
    public static void VentCanUsePostfix(Vent __instance, NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse, ref float __result)
    {
        if (pc != null && pc.Object != null && pc.Object.IsRole<SandwormRole>())
        {
            var role = pc.Object.GetRole<SandwormRole>();
            if (role != null && role.IsUnderground)
            {
                canUse = false;
                __result = float.MaxValue;
            }
        }
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.OnEnable))]
    [HarmonyPrefix]
    public static void ShipStatusOnEnablePrefix()
    {
        SandwormSystem.Reset();
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPrefix]
    public static void MeetingHudStartPrefix()
    {
        // Ensure all sandworms are visible again, collision is restored, and digging is cancelled before resetting system
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc != null && pc.IsRole<SandwormRole>())
            {
                var role = pc.GetRole<SandwormRole>();
                if (role != null)
                {
                    role.IsUnderground = false;
                    role.IsDigging = false;
                }
                pc.Visible = true;
                

            }
        }

        SandwormSystem.Reset();
    }

    [HarmonyPatch(typeof(FootstepsModifier), nameof(FootstepsModifier.FixedUpdate))]
    [HarmonyPrefix]
    public static bool FootstepsModifierFixedUpdatePrefix(FootstepsModifier __instance)
    {
        if (__instance == null) return true;
        var player = __instance.Player;
        if (player != null && player.IsRole<SandwormRole>())
        {
            var role = player.GetRole<SandwormRole>();
            if (role != null && role.IsUnderground)
            {
                return false; // Prevent footprints!
            }
        }
        return true;
    }

    /// <summary>
    /// When Sandworm is underground, reduce vision to 0.1.
    /// When emerged, normal impostor vision is used.
    /// </summary>
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
    [HarmonyPostfix]
    public static void CalculateLightRadiusPostfix(ShipStatus __instance, ref float __result)
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data.IsDead) return;
        
        var role = PlayerControl.LocalPlayer.GetRole<SandwormRole>();
        if (role != null)
        {
            var isUnderground = role.IsUnderground;
            var isEmergeVisionRestricted = !isUnderground && (Time.time - role.EmergeTime < OptionGroupSingleton<SandwormOptions>.Instance.EmergeVisionDuration);

            if (isUnderground || isEmergeVisionRestricted)
            {
                __result = __instance.MinLightRadius * 0.5f;
            }
        }
    }
}
