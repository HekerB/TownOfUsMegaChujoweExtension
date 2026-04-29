using HarmonyLib;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Extensions;
using MiraAPI.Roles;
using MiraAPI.Modifiers;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.SchrodingersCat;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
public static class SchrodingersCatDisconnectPatches
{
    public static void Postfix(AmongUsClient __instance, InnerNet.ClientData client, DisconnectReasons reason)
    {
        if (client == null || client.Character == null) return;
        var disconnectedPlayer = client.Character;

        // Cleanup modifiers if the cat or the partner leaves
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null) continue;

            // If a Schrodinger's Cat leaves, remove the PartnerRevealModifier from their owner
            if (disconnectedPlayer.IsRole<SchrodingersCatRole>())
            {
                var catRole = disconnectedPlayer.GetRole<SchrodingersCatRole>();
                if (catRole.IsAdopted && player.PlayerId == catRole.TeammateId)
                {
                    if (player.HasModifier<PartnerRevealModifier>())
                        player.RemoveModifier<PartnerRevealModifier>();
                }
            }

            // If the owner leaves, remove the CatRevealModifier from the cat, and un-adopt them?
            // The prompt says "cleanup", we'll just remove the modifier and maybe reset adoption so they can be adopted again.
            if (player.IsRole<SchrodingersCatRole>())
            {
                var catRole = player.GetRole<SchrodingersCatRole>();
                if (catRole.IsAdopted && catRole.TeammateId == disconnectedPlayer.PlayerId)
                {
                    if (player.HasModifier<CatRevealModifier>())
                        player.RemoveModifier<CatRevealModifier>();
                    
                    // Un-adopt the cat
                    catRole.TeammateId = byte.MaxValue;
                }
            }
        }
    }
}
