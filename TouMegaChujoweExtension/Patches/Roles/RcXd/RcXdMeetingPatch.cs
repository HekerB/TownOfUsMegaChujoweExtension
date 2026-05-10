using HarmonyLib;

namespace TouMegaChujoweExtension.Patches.Roles.RcXd;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
public static class RcXdMeetingPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.Data?.Role is RcXdRole role && role.ActiveCar != null)
            {
                role.ActiveCar.DoDestroy();
                role.ActiveCar = null;
            }
        }
    }
}















