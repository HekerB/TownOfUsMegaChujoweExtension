using MiraAPI.GameOptions;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Patches.Options;
using UnityEngine;
using static TownOfUs.Patches.Options.TeamChatPatches;

namespace TouMegaChujoweExtension.Patches.Roles.Apocalypse;

public static class ApocalypseTeamChatRegistration
{
    private static ExtensionTeamChatHandler? _handler;

    public static void Register()
    {
        if (_handler != null)
        {
            return;
        }

        _handler = new ExtensionTeamChatHandler
        {
            Priority = ApocalypseChatPatches.ChatPriority,
            IsChatAvailable = () =>
            {
                if (!OptionGroupSingleton<ExtensionGameMechanicOptions>.Instance.ApocalypseMeetingChat || !MeetingHud.Instance)
                {
                    return false;
                }

                var localPlayer = PlayerControl.LocalPlayer;
                return localPlayer != null && ApocalypseUtils.IsApocalypsePlayer(localPlayer);
            },
            SendMessage = (sender, message) => ApocalypseChatPatches.RpcSendApocalypseChat(sender, message),
            GetDisplayText = () => TouLocale.Get("ExtensionApocalypseChatDisplayName", "Apocalypse Chat"),
            DisplayTextColor = TouExtensionColors.Death,
            BackgroundColor = new Color(0.08f, 0.12f, 0.1f, 0.85f),
            CanDeadPlayerSee = deadPlayer =>
            {
                if (!OptionGroupSingleton<TownOfUs.Options.GeneralOptions>.Instance.TheDeadKnow)
                {
                    return false;
                }

                return deadPlayer != null &&
                       (ApocalypseUtils.IsApocalypsePlayer(deadPlayer) || DeathHandlerModifier.IsFullyDead(deadPlayer));
            }
        };

        ExtensionTeamChatRegistry.RegisterHandler(_handler);
    }

    public static void Unregister()
    {
        if (_handler == null)
        {
            return;
        }

        ExtensionTeamChatRegistry.UnregisterHandler(_handler);
        _handler = null;
    }
}
