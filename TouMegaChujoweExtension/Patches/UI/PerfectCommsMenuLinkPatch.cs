using HarmonyLib;
using TownOfUs.Assets;
using TownOfUs.Patches.AprilFools;
using UnityEngine;
using UnityEngine.UI;

namespace TouMegaChujoweExtension.Patches.UI;

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public static class PerfectCommsMenuLinkPatch
{
    private const string PerfectCommsUrl = "https://github.com/marzecoo/Chujowe-Perfect-Comms";

    public static void Postfix(MainMenuManager __instance)
    {
        if (__instance.newsButton == null || GameObject.Find("TouMcePerfectCommsButton") != null)
            return;

        var commsButton = __instance.newsButton.CloneMenuItem(
            "TouMcePerfectCommsButton",
            new Vector2(0.815f, 0.86f),
            TouAssets.SourceCode.LoadAsset(),
            "ExtensionMenuPerfectComms",
            "Perfect Comms");

        var passive = commsButton.GetComponent<PassiveButton>();
        passive.OnClick = new Button.ButtonClickedEvent();
        passive.OnClick.AddListener((Action)(() => Constants.OpenURL(PerfectCommsUrl)));

        var uiList = new Il2CppSystem.Collections.Generic.List<PassiveButton>();
        foreach (var button in __instance.mainButtons)
        {
            uiList.Add(button);
        }

        uiList.Add(passive);
        __instance.mainButtons = uiList;
        __instance.SetUpControllerNav();
    }
}
