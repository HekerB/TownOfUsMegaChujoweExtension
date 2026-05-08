using HarmonyLib;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Patches.Assassin;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
public static class ClassicAssassinRefreshPatch
{
    public static void Postfix()
    {
        if (!MeetingHud.Instance) return;

        ClassicAssassinSystem.RefreshExemptions();
    }
}
