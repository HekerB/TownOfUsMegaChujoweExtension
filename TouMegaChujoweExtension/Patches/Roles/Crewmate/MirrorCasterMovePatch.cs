using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TownOfUs.Extensions;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Crewmate;

[HarmonyPatch]
public static class MirrorCasterMovePatch
{
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

            __instance.moveable = true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void FixedUpdatePostfix(PlayerControl __instance)
    {
        if (!__instance.AmOwner || MeetingHud.Instance != null || ExileController.Instance != null) return;

        if (__instance.IsRole<MirrorcasterRole>() &&
            OptionGroupSingleton<MirrorCasterExtensionOptions>.Instance.MoveWhileMenu &&
            (Minigame.Instance != null || MapBehaviour.Instance != null))
        {
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