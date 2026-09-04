using HarmonyLib;
using TownOfUs.Patches;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Pirate
{
    [HarmonyPatch(typeof(TownOfUs.Modules.Components.HudManagerHelper), nameof(TownOfUs.Modules.Components.HudManagerHelper.UpdateRoleNameText))]
    public static class PirateDuelTargetColorPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            var localPlayer = PlayerControl.LocalPlayer;
            if (localPlayer == null || localPlayer.Data?.Role is not PirateRole pirateRole)
            {
                return;
            }

            if (pirateRole.DuelTargetId == byte.MaxValue)
            {
                return;
            }

            // During meeting, PirateDuelMeetingPatch handles the meeting name color and text,
            // but during gameplay we need to color the target's nickname yellow.
            if (MeetingHud.Instance == null)
            {
                var target = MiscUtils.PlayerById(pirateRole.DuelTargetId);
                if (target != null && target.cosmetics?.nameText != null)
                {
                    target.cosmetics.nameText.color = Color.yellow;
                }
            }
        }
    }
}
