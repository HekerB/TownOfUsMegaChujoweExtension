using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Networking;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using System.Linq;
using UnityEngine;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Assets;
using TownOfUs.Modules.Localization;
using TownOfUs;
using Reactor;

namespace TouMegaChujoweExtension.Patches.Roles.Jackal;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
public static class JackalRecruitDeathPatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerControl __instance)
    {
        if (__instance == null || __instance.Data == null || AmongUsClient.Instance == null) return;
        
        if (__instance.TryGetModifier<SidekickModifier>(out var sidekick) && sidekick != null)
        {
            var jackalId = sidekick.JackalId;
            var jackalPlayer = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p != null && p.PlayerId == jackalId);
        
            if (jackalPlayer != null && jackalPlayer.Pointer != System.IntPtr.Zero)
            {
                var jackalRole = jackalPlayer.GetRole<JackalRole>();
                if (jackalRole != null)
                {
                    jackalRole.OnRecruitDie();
                    
                    if (jackalPlayer.AmOwner && OptionGroupSingleton<JackalOptions>.Instance != null && OptionGroupSingleton<JackalOptions>.Instance.NotifySidekickDeath)
                    {
                        try
                        {
                            string alertMsg = string.Format(TouLocale.Get("ExtensionJackalSidekickDiedAlert"), __instance.Data.PlayerName ?? "Recruit");
                            var notification = Helpers.CreateAndShowNotification(
                                alertMsg, 
                                TouExtensionColors.Jackal, 
                                new Vector3(0f, 1f, -20f), 
                                spr: TouExtensionIcons.SidekickModifierIcon.LoadAsset()
                            );
                            if (notification != null)
                            {
                                notification.AdjustNotification();
                            }
                            Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Jackal));
                        }
                        catch (System.Exception ex)
                        {
                            UnityEngine.Debug.LogError($"[TOUMCE] Error showing Sidekick death notification: {ex}");
                        }
                    }
                }
            }
        }
    }
}
