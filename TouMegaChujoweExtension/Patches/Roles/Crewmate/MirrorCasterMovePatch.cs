using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TownOfUs.Extensions;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Crewmate;

[HarmonyPatch]
public static class MirrorCasterMovePatch
{

    // We removed PlayerControlCanMovePostfix because forcing CanMove to true 
    // while a menu is open causes HudManager to steal mouse clicks for movement,
    // resulting in the "3 clicks to target" bug. 
    // Manual WASD movement is still handled in FixedUpdate.

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    public static void FixedUpdatePrefix(PlayerControl __instance)
    {
        if (!__instance.AmOwner || MeetingHud.Instance != null || ExileController.Instance != null) return;

        if (__instance.IsRole<MirrorcasterRole>() &&
            OptionGroupSingleton<MirrorCasterExtensionOptions>.Instance.MoveWhileMenu &&
            (Minigame.Instance != null || MapBehaviour.Instance != null))
        {
            // If a menu is open, we force moveable to true so that:
            // 1. Manual movement (velocity) works.
            // 2. The targeting UI buttons (which check moveable) are clickable.
            __instance.moveable = true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void FixedUpdatePostfix(PlayerControl __instance)
    {
        // No more _weSetMoveable reset here. 
        // We let the game's standard menu logic handle resetting moveable when it closes, 
        // or we just keep it true since the player should be able to move anyway.

        if (!__instance.AmOwner || MeetingHud.Instance != null || ExileController.Instance != null) return;

        if (__instance.IsRole<MirrorcasterRole>() &&
            OptionGroupSingleton<MirrorCasterExtensionOptions>.Instance.MoveWhileMenu &&
            (Minigame.Instance != null || MapBehaviour.Instance != null))
        {
            // If we are in a minigame, KeyboardJoystick returns zero.
            // We manually apply movement if keys are pressed.
            var horizontal = Input.GetAxisRaw("Horizontal");
            var vertical = Input.GetAxisRaw("Vertical");
            var move = new Vector2(horizontal, vertical);

            if (move.sqrMagnitude > 0.01f)
            {
                move.Normalize();
                var speed = __instance.MyPhysics.TrueSpeed;
                __instance.MyPhysics.body.velocity = move * speed;
                __instance.MyPhysics.HandleAnimation(__instance.Data.IsDead);
            }
        }
    }
}
