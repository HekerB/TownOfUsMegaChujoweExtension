using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Modules;

namespace TouMegaChujoweExtension.Patches.Roles.VoodooMaster;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CanMove), MethodType.Getter)]
public static class VoodooMoveWithMenuPatch
{
    public static bool Prefix(PlayerControl __instance, ref bool __result)
    {
        if (PlayerControl.LocalPlayer == null || MeetingHud.Instance != null)
        {
            return true;
        }

        if (PlayerControl.LocalPlayer.Data?.Role is VoodooMasterRole &&
            ActiveInputManager.currentControlType == ActiveInputManager.InputType.Keyboard &&
            OptionGroupSingleton<VoodooMasterOptions>.Instance.MoveWhileMenu &&
            Minigame.Instance is CustomPlayerMenu)
        {
            __result = __instance.moveable;
            return false;
        }

        return true;
    }
}
