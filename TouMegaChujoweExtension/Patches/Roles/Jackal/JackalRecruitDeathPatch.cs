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
        if (__instance == null) return;
        
        if (__instance.TryGetModifier<SidekickModifier>(out var sidekick))
        {
            var jackalId = sidekick.JackalId;
            var jackalPlayer = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == jackalId);
            
            // Notify Jackal
            if (jackalPlayer != null)
            {
                var jackalRole = jackalPlayer.GetRole<JackalRole>();
                if (jackalRole != null)
                {
                    jackalRole.OnRecruitDie();
                    
                    if (jackalPlayer.AmOwner && OptionGroupSingleton<JackalOptions>.Instance.NotifySidekickDeath)
                    {
                        string alertMsg = string.Format(TouLocale.Get("ExtensionJackalSidekickDiedAlert"), __instance.Data.PlayerName);
                        Helpers.CreateAndShowNotification(
                            alertMsg, 
                            TouExtensionColors.Jackal, 
                            new Vector3(0f, 1f, -20f), 
                            spr: TouExtensionIcons.SidekickModifierIcon.LoadAsset()
                        ).AdjustNotification();
                        
                        Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Jackal));
                    }
                }
            }
            
        }
        
        // If Jackal dies, all their recruits die too
        bool isJackal = __instance.IsRole<JackalRole>();
        
        if (isJackal)
        {
            foreach (var recruit in PlayerControl.AllPlayerControls.ToArray())
            {
                if (recruit == null || recruit.Data == null || recruit.Data.IsDead) continue;
                
                if (recruit.TryGetModifier<SidekickModifier>(out var m) && m.JackalId == __instance.PlayerId)
                {
                    // Suicide via lifelink
                    if (recruit.AmOwner)
                    {
                        MiraAPI.Networking.CustomMurderRpc.RpcCustomMurder(recruit, recruit, MeetingCheck.OutsideMeeting, showKillAnim: false);
                    }
                }
            }
        }
    }
}
