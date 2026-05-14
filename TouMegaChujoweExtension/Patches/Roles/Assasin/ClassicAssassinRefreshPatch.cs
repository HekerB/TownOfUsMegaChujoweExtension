using HarmonyLib;

namespace TouMegaChujoweExtension.Patches.Roles.Assasin;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
public static class ClassicAssassinRefreshPatch
{
    public static void Postfix()
    {
        if (!MeetingHud.Instance) return;

        ClassicAssassinSystem.RefreshExemptions();
    }
}














