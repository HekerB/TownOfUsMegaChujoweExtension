using HarmonyLib;
using MiraAPI.GameOptions;
using TownOfUs.Modules.Components;

namespace TouMegaChujoweExtension.Patches.Roles.Venomous;

[HarmonyPatch(typeof(ViperDeadBody), nameof(ViperDeadBody.SetupViperInfo))]
public static class VenomousDelayPatch
{
    public static bool Active;

    public static void Prefix(ref float maxTime)
    {
        if (!Active)
            return;

        maxTime = OptionGroupSingleton<VenomousModifierOptions>.Instance.VenomousRotDelay.Value;
        Active = false;
    }
}














