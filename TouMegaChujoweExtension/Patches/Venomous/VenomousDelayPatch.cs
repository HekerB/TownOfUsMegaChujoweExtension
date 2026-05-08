using HarmonyLib;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Options.Modifiers;
using TownOfUs.Modules.Components;

namespace TouMegaChujoweExtension.Patches.Neutral;

[HarmonyPatch(typeof(ViperDeadBody), nameof(ViperDeadBody.SetupViperInfo))]
public static class VenomousDelayPatch
{
    public static bool Active { get; set; }

    public static void Prefix(ref float maxTime)
    {
        if (!Active)
            return;

        maxTime = OptionGroupSingleton<VenomousModifierOptions>.Instance.VenomousRotDelay.Value;
        Active = false;
    }
}
