using HarmonyLib;
using TownOfUs.Extensions;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles.RcXd;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
public static class RcXdMeetingPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null && player.IsRole<RcXdRole>())
            {
                var role = player.GetRole<RcXdRole>();
                if (role != null && role.ActiveCar != null)
                {
                    role.ActiveCar.DoDestroy();
                    role.ActiveCar = null;
                }
            }
        }
    }
}















