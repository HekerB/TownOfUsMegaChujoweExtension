using AmongUs.GameOptions;
using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using Reactor.Networking.Attributes;
using Reactor.Utilities.Extensions;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Patches.Options;
using TownOfUs.Utilities;
using TownOfUs;
using System.Linq;

namespace TouMegaChujoweExtension.Patches.Roles.Jackal;

public static class JackalChatPatches
{
    [MethodRpc((uint)ExtensionRpc.SendSidekickChat)]
    public static void RpcSendSidekickChat(PlayerControl player, string text)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null) return;

        var isJackal = localPlayer.GetRole<JackalRole>() != null;
        var isSidekick = localPlayer.TryGetModifier<SidekickModifier>(out _);
        var isDeadAndKnows = DeathHandlerModifier.IsFullyDead(localPlayer) &&
                             OptionGroupSingleton<TownOfUs.Options.GeneralOptions>.Instance.TheDeadKnow;

        var shouldMarkUnread = false;

        if (player.AmOwner)
        {
            MiscUtils.AddTeamChat(player.Data,
                $"<color=#{TouExtensionColors.Jackal.ToHtmlStringRGBA()}>{player.Data.PlayerName} (SIDE)</color>",
                text, bubbleType: BubbleType.Other, onLeft: false);
            shouldMarkUnread = true;
        }
        else if (isJackal || isSidekick || isDeadAndKnows)
        {
            MiscUtils.AddTeamChat(player.Data,
                $"<color=#{TouExtensionColors.Jackal.ToHtmlStringRGBA()}>{player.Data.PlayerName} (SIDE)</color>",
                text, bubbleType: BubbleType.Other, onLeft: true);
            shouldMarkUnread = true;
        }

        if (shouldMarkUnread && MeetingHud.Instance != null)
        {
            TeamChatPatches.TeamChatManager.MarkChatAsUnread(55);
        }
    }
}
