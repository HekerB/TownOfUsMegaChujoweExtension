using HarmonyLib;
using MiraAPI.GameOptions;
using TMPro;
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
        if (IsDraftMode())
        {
            __result = TouGamemode.Normal;
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        var modeValue = __instance.transform.Find("LobbyInfoPane/AspectSize/ModeValue");
        if (modeValue == null)
        {
            return;
        }

        foreach (var text in modeValue.GetComponentsInChildren<TextMeshPro>(true))
        {
            if (!text.name.Contains("GameModeText"))
            {
                continue;
            }

            if (IsDraftMode())
            {
                text.text = "<color=#00FF00>DRAFT MODE</color>";
            }
            else
            {
                text.text = "<color=#FF0000>VANILLA MODE</color>";
            }
        }
    }
}
