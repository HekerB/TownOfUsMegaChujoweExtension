using MiraAPI.GameOptions;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Networking;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Patches.Options;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Roles.Apocalypse;

public static class ApocalypseChatPatches
{
    public const int ChatPriority = 65;

    [MethodRpc((uint)ExtensionRpc.SendApocalypseChat)]
    public static void RpcSendApocalypseChat(PlayerControl player, string text)
    {
        if (!ApocalypseUtils.MeetingChatEnabled || player == null || !ApocalypseUtils.IsApocalypsePlayer(player))
        {
            return;
        }

        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null)
        {
            return;
        }

        var isDeadAndKnows = DeathHandlerModifier.IsFullyDead(localPlayer) &&
                             OptionGroupSingleton<TownOfUs.Options.GeneralOptions>.Instance.TheDeadKnow;
        var canSee = player.AmOwner || ApocalypseUtils.IsApocalypsePlayer(localPlayer) || isDeadAndKnows;
        if (!canSee)
        {
            return;
        }

        var title = $"<color=#{ColorUtility.ToHtmlStringRGBA(TouExtensionColors.Death)}>{TouLocale.Get("ExtensionApocalypseChatTitle", "Apocalypse")}</color>";
        MiscUtils.AddTeamChat(player.Data, title, text, bubbleType: BubbleType.Other, onLeft: !player.AmOwner);

        if (MeetingHud.Instance == null)
        {
            return;
        }

        var chats = TeamChatPatches.TeamChatManager.GetAllAvailableChats();
        var hasForcedChat = chats.Any(c => c.IsForced);
        var currentChat = TeamChatPatches.CurrentChatIndex >= 0 && TeamChatPatches.CurrentChatIndex < chats.Count
            ? chats[TeamChatPatches.CurrentChatIndex]
            : null;
        if ((!TeamChatPatches.TeamChatActive || currentChat == null || currentChat.Priority != ChatPriority) && !hasForcedChat)
        {
            TeamChatPatches.TeamChatManager.MarkChatAsUnread(ChatPriority);
        }
    }
}
