using HarmonyLib;
using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Buttons.Classic.Impostor;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles;

[HarmonyPatch]
public static class BootPatches
{
    [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
    [HarmonyPostfix]
    public static void KillButtonDoClickPostfix(KillButton __instance)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data == null || player.Data.IsDead) return;

        if (player.IsRole<BootRole>())
        {
            var options = OptionGroupSingleton<BootOptions>.Instance;
            if (options.SyncCooldowns && BootButton.Instance != null && __instance.isCoolingDown)
            {
                BootButton.Instance.Timer = options.BootCooldown;
            }
        }
    }
}
