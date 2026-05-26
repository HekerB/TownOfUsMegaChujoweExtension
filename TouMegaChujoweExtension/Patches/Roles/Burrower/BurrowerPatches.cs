using HarmonyLib;
using MiraAPI.Events.Vanilla.Usables;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Extensions;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Burrower;

[HarmonyPatch]
public static class BurrowerPatches
{
    [HarmonyPatch(typeof(TownOfUs.Events.TownOfUsEventHandlers), nameof(TownOfUs.Events.TownOfUsEventHandlers.PlayerCanUseEventHandler))]
    [HarmonyPrefix]
    public static bool PlayerCanUseEventHandlerPrefix(PlayerCanUseEvent @event)
    {
        if (PlayerControl.LocalPlayer == null || !PlayerControl.LocalPlayer.IsRole<BurrowerRole>())
        {
            return true;
        }

        if (!@event.IsVent)
        {
            return true;
        }

        if ((HudManager.Instance != null && HudManager.Instance.Chat.IsOpenOrOpening) || MeetingHud.Instance)
        {
            @event.Cancel();
        }

        return false;
    }

    [HarmonyPatch(typeof(LogicOptions), nameof(LogicOptions.GetPlayerSpeedMod))]
    [HarmonyPostfix]
    public static void GetPlayerSpeedModPostfix(PlayerControl pc, ref float __result)
    {
        var role = pc?.GetRole<BurrowerRole>();
        if (role != null && role.IsUnderground)
        {
            __result *= role.GetUndergroundSpeedMultiplier();
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
    [HarmonyPrefix]
    public static bool PlayerPhysicsFixedUpdatePrefix(PlayerPhysics __instance)
    {
        var player = __instance?.myPlayer;
        var role = player?.GetRole<BurrowerRole>();
        if (player == null || role == null || (!role.IsPreparingDig && !role.IsUnderground))
        {
            return true;
        }

        if (role.IsPreparingDig)
        {
            if (player.AmOwner)
            {
                AdvancedMovementUtilities.ApplyControlledMovement(__instance, Vector2.zero, stopIfZero: true);
            }

            return false;
        }

        if (player.AmOwner)
        {
            var direction = AdvancedMovementUtilities.GetRegularDirection();
            AdvancedMovementUtilities.ApplyControlledMovement(__instance, direction, stopIfZero: true);
        }

        return false;
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
    [HarmonyPostfix]
    public static void VentCanUsePostfix(NetworkedPlayerInfo pc, ref bool canUse, ref float __result)
    {
        var role = pc?.Object?.GetRole<BurrowerRole>();
        if (role != null && role.IsUnderground)
        {
            canUse = false;
            __result = float.MaxValue;
        }
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.OnEnable))]
    [HarmonyPrefix]
    public static void ShipStatusOnEnablePrefix()
    {
        if (!OptionGroupSingleton<BurrowerOptions>.Instance.VentsStayAfterMeeting)
        {
            BurrowerSystem.Reset();
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPrefix]
    public static void MeetingHudStartPrefix()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            var role = player?.GetRole<BurrowerRole>();
            if (role == null)
            {
                continue;
            }

            role.IsPreparingDig = false;
            role.IsUnderground = false;
            role.IsDigging = false;
            role.PrepareDigEndTime = 0f;
            player!.Visible = true;
            player.RemoveModifier<BurrowerInvisibleModifier>();
            player.RemoveModifier<BurrowerSpeedModifier>();
        }

        if (!OptionGroupSingleton<BurrowerOptions>.Instance.VentsStayAfterMeeting)
        {
            BurrowerSystem.Reset();
        }
    }

    [HarmonyPatch(typeof(FootstepsModifier), nameof(FootstepsModifier.FixedUpdate))]
    [HarmonyPrefix]
    public static bool FootstepsModifierFixedUpdatePrefix(FootstepsModifier __instance)
    {
        var role = __instance?.Player?.GetRole<BurrowerRole>();
        return role == null || !role.IsUnderground;
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
    [HarmonyPostfix]
    public static void CalculateLightRadiusPostfix(ShipStatus __instance, ref float __result)
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data.IsDead)
        {
            return;
        }

        var role = PlayerControl.LocalPlayer.GetRole<BurrowerRole>();
        if (role == null)
        {
            return;
        }

        if (role.IsUnderground)
        {
            var fadeOut = Mathf.Clamp01((Time.time - role.BurrowStartTime) / 0.75f);
            __result *= Mathf.Lerp(1f, 0.08f, fadeOut);
            return;
        }

        var options = OptionGroupSingleton<BurrowerOptions>.Instance;
        if (options.EmergeVisionDuration <= 0f)
        {
            return;
        }

        var emergeElapsed = Time.time - role.EmergeTime;
        if (emergeElapsed >= 0f && emergeElapsed < options.EmergeVisionDuration)
        {
            var fadeIn = Mathf.Clamp01(emergeElapsed / options.EmergeVisionDuration);
            __result *= Mathf.Lerp(0.08f, 1f, fadeIn);
        }
    }
}
