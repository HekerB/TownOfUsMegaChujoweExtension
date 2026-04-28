using HarmonyLib;
using System.Reflection;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Utilities;
using TownOfUs;
using TownOfUs.Extensions;
using TouMegaChujoweExtension.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace TouMegaChujoweExtension.Patches.SchrodingersCat;

[HarmonyPatch]
public static class SchrodingersCatChatPatches
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        return typeof(TownOfUs.Patches.Options.TeamChatPatches).GetMethods()
            .Where(m => m.Name.StartsWith("RpcSend"));
    }

    public static void Postfix(MethodBase __originalMethod, object[] __args)
    {
        if (__args.Length < 2) return;
        var sender = __args[0] as PlayerControl;
        var text = __args[1] as string;
        
        if (sender == null || text == null) return;

        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null) return;

        // Case 1: Local player is the Cat seeing team messages
        if (localPlayer.IsRole<SchrodingersCatRole>())
        {
            var catRole = localPlayer.GetRole<SchrodingersCatRole>();
            if (catRole.IsAdopted)
            {
                var partner = MiscUtils.PlayerById(catRole.TeammateId);
                if (partner == null) return;

                bool shouldSee = false;
                string title = "Team Chat";

                if (__originalMethod.Name == "RpcSendImpostorChat" && partner.Data.Role.IsImpostor)
                {
                    shouldSee = true;
                    title = "(Impostor Chat)";
                }
                else if (__originalMethod.Name == "RpcSendVampireChat" && partner.IsRole<VampireRole>())
                {
                    shouldSee = true;
                    title = "(Vampire Chat)";
                }
                else if (__originalMethod.Name == "RpcSendLoveChat" && partner.HasModifier<TownOfUs.Modifiers.Game.Alliance.LoverModifier>())
                {
                    shouldSee = true;
                    title = "(Lover Chat)";
                }

                if (shouldSee)
                {
                    MiscUtils.AddTeamChat(sender.Data, $"{title} {sender.Data.PlayerName}", text, bubbleType: BubbleType.Other, onLeft: !sender.AmOwner);
                    if (MeetingHud.Instance != null && !sender.AmOwner)
                    {
                        TownOfUs.Patches.Options.TeamChatPatches.TeamChatManager.MarkChatAsUnread(65);
                    }
                }
            }
        }
        
        // Case 2: Local player is a partner seeing Cat messages sent via normal team chats (if any)
        // This is mostly covered by RpcSendCatChat, but good to keep in mind.
    }
}
