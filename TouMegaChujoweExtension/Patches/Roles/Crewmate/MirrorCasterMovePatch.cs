using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TownOfUs.Extensions;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Roles.Crewmate;
using TownOfUs;
using TownOfUs.Buttons;
using TownOfUs.Modules;
using TownOfUs.Utilities;
using MiraAPI.Hud;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
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

        // If chat is open, do not override moveable state
        if (HudManager.Instance != null && HudManager.Instance.Chat != null && HudManager.Instance.Chat.IsOpenOrOpening) return;

        if (CanMoveWithOpenMenu(__instance))
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

        if (CanMoveWithOpenMenu(__instance))
        {
            // If chat is open or opening, immediately stop any movement velocity and return
            if (HudManager.Instance != null && HudManager.Instance.Chat != null && HudManager.Instance.Chat.IsOpenOrOpening)
            {
                if (__instance.MyPhysics != null && __instance.MyPhysics.body != null)
                {
                    __instance.MyPhysics.body.velocity = Vector2.zero;
                }
                return;
            }

            if (Minigame.Instance is CustomPlayerMenu || (MapBehaviour.Instance != null && MapBehaviour.Instance.gameObject.activeSelf))
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
                else
                {
                    // Reset velocity to Vector2.zero when movement keys are released
                    if (__instance.MyPhysics != null && __instance.MyPhysics.body != null)
                    {
                        __instance.MyPhysics.body.velocity = Vector2.zero;
                    }
                }
            }
        }
    }

    private static bool CanMoveWithOpenMenu(PlayerControl player)
    {
        var customPlayerMenuOpen = Minigame.Instance is CustomPlayerMenu;
        var mapOpen = MapBehaviour.Instance != null && MapBehaviour.Instance.gameObject.activeSelf;

        return player.IsRole<MirrorcasterRole>() &&
                   OptionGroupSingleton<MirrorCasterExtensionOptions>.Instance.MoveWhileMenu &&
                   (customPlayerMenuOpen || mapOpen) ||
               player.IsRole<JokerRole>() &&
                   OptionGroupSingleton<JokerOptions>.Instance.MoveWithTablet &&
                   customPlayerMenuOpen;
    }
}
