using HarmonyLib;
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
            TouExtensionIcons.MicLogo.LoadAsset(),
            "ExtensionMenuPerfectComms",
            "Perfect Comms");
        ResizeIcon(commsButton);

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

    private static void ResizeIcon(PassiveButton button)
    {
        ResizeIcon(button.transform.GetChild(1).GetChild(0));
        ResizeIcon(button.transform.GetChild(2).GetChild(0));
    }

    private static void ResizeIcon(Transform icon)
    {
        icon.localScale = new Vector3(0.62f, 0.62f, 1f);
        icon.localPosition = new Vector3(-0.02f, 0.01f, icon.localPosition.z);
    }
}
