using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TownOfUs.Modifiers.Impostor;
using TownOfUs.Options.Maps;
using TownOfUs.Options.Roles.Impostor;
using TownOfUs.Options;
using TownOfUs.Patches;
using TownOfUs.Utilities.Appearances;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.BugFixes;

[HarmonyPatch]
public static class ParasiteBugFixPatches
{
    // Fix 1: Prevent name color leak in RawSetAppearance
    [HarmonyPatch(typeof(AppearanceExtensions), nameof(AppearanceExtensions.RawSetAppearance), typeof(PlayerControl), typeof(VisualAppearance))]
    [HarmonyPrefix]
    public static void RawSetAppearancePrefix(PlayerControl player, VisualAppearance appearance)
    {
        // If the appearance doesn't specify a color and the player is impostor-aligned,
        // the original method would set it to red for EVERYONE.
        // We override this by setting a default white color if the local player shouldn't see red.
        if (appearance.NameColor == null && player.IsImpostorAligned())
        {
            if (PlayerControl.LocalPlayer == null || !PlayerControl.LocalPlayer.IsImpostorAligned())
            {
                appearance.NameColor = Color.white;
            }
        }
    }

    // Fix 2: Prevent Parasite reveal during Comms Sabotage
    [HarmonyPatch(typeof(ParasiteInfectedModifier), nameof(ParasiteInfectedModifier.GetVisualAppearance))]
    [HarmonyPostfix]
    public static void GetVisualAppearancePostfix(ParasiteInfectedModifier __instance, ref VisualAppearance? __result)
    {
        // If taking control during comms, the victim should remain camouflaged
        if (HudManagerPatches.CommsSaboActive())
        {
            var player = __instance.Player;
            __result = new VisualAppearance(player.GetDefaultAppearance(), TownOfUsAppearances.Camouflage)
            {
                ColorId = player.Data.DefaultOutfit.ColorId,
                HatId = string.Empty,
                SkinId = string.Empty,
                VisorId = string.Empty,
                PlayerName = string.Empty,
                PetId = string.Empty,
                NameVisible = false,
                PlayerMaterialColor = Color.grey,
                Size = (OptionGroupSingleton<AdvancedSabotageOptions>.Instance.HidePlayerSizeInCamo) ? new Vector3(0.7f, 0.7f, 1f) : player.GetAppearance().Size
            };
        }
    }
}











