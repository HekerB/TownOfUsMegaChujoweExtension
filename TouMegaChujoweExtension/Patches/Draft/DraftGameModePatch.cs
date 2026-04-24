using HarmonyLib;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Options;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Draft;

[HarmonyPatch]
public static class DraftGameModePatch
{
    public static bool IsDraftMode()
    {
        try
        {
            return OptionGroupSingleton<DraftModeOptions>.Instance.EnableDraftMode;
        }
        catch
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(MiscUtils), nameof(MiscUtils.CurrentGamemode))]
    [HarmonyPostfix]
    public static void CurrentGamemodePostfix(ref TouGamemode __result)
    {
    }
}