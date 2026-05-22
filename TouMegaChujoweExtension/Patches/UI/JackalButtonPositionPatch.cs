using HarmonyLib;
using TouMegaChujoweExtension.Buttons.Neutral;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using MiraAPI.Modifiers;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs.Utilities;
using TownOfUs.Extensions;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.UI;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class JackalButtonPositionPatch
{
    private static bool _wasJackal = false;

    [HarmonyPostfix]
    public static void Postfix()
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return;

        var isJackal = PlayerControl.LocalPlayer.IsRole<TouMegaChujoweExtension.Roles.Classic.Neutral.JackalRole>();

        if (isJackal && !_wasJackal)
        {
            if (MiraAPI.GameOptions.OptionGroupSingleton<JackalOptions>.Instance != null)
            {
                var canVent = MiraAPI.GameOptions.OptionGroupSingleton<JackalOptions>.Instance.CanVent || 
                              (LocalSettingsTabSingleton<TownOfUs.TownOfUsLocalSettings>.Instance != null && LocalSettingsTabSingleton<TownOfUs.TownOfUsLocalSettings>.Instance.OffsetButtonsToggle.Value);
                var kill = MiraAPI.Hud.CustomButtonSingleton<JackalKillButton>.Instance;
                if (kill != null)
                {
                    Reactor.Utilities.Coroutines.Start(MiscUtils.CoMoveButtonIndex(kill, !canVent));
                }
            }
        }

        _wasJackal = isJackal;
    }
}
