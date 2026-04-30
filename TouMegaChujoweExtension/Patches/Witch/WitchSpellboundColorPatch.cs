using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches;

/// <summary>
/// Patch to make spellbound players' names purple for everyone after first meeting if they have meetings left.
/// </summary>
[HarmonyPatch(typeof(PlayerRoleTextExtensions), nameof(PlayerRoleTextExtensions.UpdateTargetColor), typeof(Color), typeof(PlayerControl), typeof(bool))]
public static class WitchSpellboundColorPatch
{
    [HarmonyPostfix]
    public static void UpdateTargetColorPostfix(ref Color __result, PlayerControl player, bool hidden = false)
    {
        if (player == null || !player.HasModifier<WitchSpellboundModifier>())
        {
            return;
        }

        var localPlayer = PlayerControl.LocalPlayer;
        var modifier = player.GetModifier<WitchSpellboundModifier>();
        if (modifier == null) return;

        var options = OptionGroupSingleton<WitchOptions>.Instance;
        var meetingsUntilDeath = options.MeetingsUntilDeath;
        var currentMeetingCount = Events.Impostor.WitchEvents.GetCurrentMeetingCount();
        var meetingsSinceSpell = (currentMeetingCount - 1) - modifier.SpellCastMeeting;
        var meetingsRemaining = meetingsUntilDeath - meetingsSinceSpell;

        if (MeetingHud.Instance != null)
        {
            // W trakcie meetingu kolorujemy nick wszystkim, gdy widać Hexa
            if (meetingsSinceSpell >= 0 && meetingsRemaining >= 0)
            {
                __result = TouExtensionColors.Witch;
            }
            return;
        }

        // W zwykłej rozgrywce kolor widzi TYLKO Wiedźma i inni Impostorzy
        if (localPlayer != null && localPlayer.Data != null && localPlayer.Data.Role != null)
        {
            if (localPlayer.IsRole<WitchRole>() || localPlayer.Data.Role.IsImpostor)
            {
                __result = TouExtensionColors.Witch;
            }
        }
    }
}
