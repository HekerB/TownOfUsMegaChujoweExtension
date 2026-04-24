using HarmonyLib;
using MiraAPI.Hud;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Buttons.Crewmate;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.President;

[HarmonyPatch(typeof(MonarchRole), nameof(MonarchRole.RpcKnight))]
public static class PresidentMonarchInteractionPatch
{
    [HarmonyPrefix]
    public static bool Prefix(PlayerControl player, PlayerControl target)
    {

        if (target == null || target.Data.Role is not PresidentRole)
        {
            return true;
        }

        if (player.AmOwner)
        {

            var notif = Helpers.CreateAndShowNotification(
                $"<b>This player cannot be knighted! Stack refunded.</b>",
                Color.red, new Vector3(0f, 1f, -20f));

            if (notif?.Text != null)
            {
                notif.Text.SetOutlineThickness(0.35f);
            }

            var button = CustomButtonSingleton<MonarchKnightButton>.Instance;
            if (button != null)
            {

                if (button.LimitedUses)
                {
                    button.UsesLeft++;
                    button.Button?.SetUsesRemaining(button.UsesLeft);
                }

                button.Timer = 0.5f;
            }
        }
        return false;
    }
}