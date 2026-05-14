using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Witch;

/// <summary>
/// Patch to make spellbound players' names purple for everyone after first meeting if they have meetings left.
/// </summary>
[HarmonyPatch(typeof(PlayerRoleTextExtensions), nameof(PlayerRoleTextExtensions.UpdateTargetColor), typeof(Color), typeof(PlayerControl), typeof(bool))]
public static class WitchSpellboundColorPatch
{
    [HarmonyPostfix]
    public static void UpdateTargetColorPostfix(ref Color __result, PlayerControl player, bool hidden = false)
    {
        if (player == null) return;
        
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null) return;

        // --- WITCH LOGIC ---
        if (player.HasModifier<WitchSpellboundModifier>())
        {
            var modifier = player.GetModifier<WitchSpellboundModifier>();
            if (modifier != null)
            {
                var options = OptionGroupSingleton<WitchOptions>.Instance;
                var currentMeetingCount = Events.Impostor.WitchEvents.GetCurrentMeetingCount();
                var meetingsSinceSpell = (currentMeetingCount - 1) - modifier.SpellCastMeeting;
                var meetingsRemaining = options.MeetingsUntilDeath - meetingsSinceSpell;

                if (MeetingHud.Instance != null)
                {
                    if (meetingsSinceSpell >= 0 && meetingsRemaining >= 0)
                    {
                        __result = TouExtensionColors.Witch;
                        return;
                    }
                }
                else if (localPlayer.IsRole<WitchRole>() || (localPlayer.Data?.Role != null && localPlayer.Data.Role.IsImpostor))
                {
                    __result = TouExtensionColors.Witch;
                    return;
                }
            }
        }

        // --- POISONER LOGIC ---
        if (Modules.PoisonSystem.IsTargetPoisonedByPoison(player.PlayerId))
        {
            if (localPlayer.IsImpostorAligned())
            {
                __result = new Color32(0, 255, 0, 255); // Green %
            }
        }
    }
}




















