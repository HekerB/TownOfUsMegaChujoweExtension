using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using static TownOfUs.Patches.Options.TeamChatPatches;
using TownOfUs.Patches.Options;
using TownOfUs;
using UnityEngine;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles.Jackal;

public static class JackalTeamChatRegistration
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
            Priority = 55,
            IsChatAvailable = () =>
            {
                if (!MeetingHud.Instance)
                {
                    return false;
                }

                var localPlayer = PlayerControl.LocalPlayer;
                if (localPlayer == null || localPlayer.Data == null)
                {
                    return false;
                }

                var genOpt = OptionGroupSingleton<Options.ExtensionGeneralOptions>.Instance;
                if (!genOpt.JackalChat)
                {
                    return false;
                }

                var isJackal = localPlayer.GetRole<JackalRole>() != null;
                var isSidekick = localPlayer.TryGetModifier<SidekickModifier>(out _);

                return isJackal || isSidekick;
            },
            SendMessage = (sender, message) =>
            {
                var localPlayer = PlayerControl.LocalPlayer;
                if (localPlayer.TryGetModifier<SidekickModifier>(out _))
                {
                    JackalChatPatches.RpcSendSidekickChat(sender, message);
                }
            },
            GetDisplayText = () =>
            {
                return "Infiltrator Chat";
            },
            DisplayTextColor = TouExtensionColors.Jackal,
            BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f),
            CanDeadPlayerSee = (deadPlayer) =>
            {
                var genOpt = OptionGroupSingleton<TownOfUs.Options.GeneralOptions>.Instance;
                if (!genOpt.TheDeadKnow)
                {
                    return false;
                }

                var isJackal = deadPlayer.GetRole<JackalRole>() != null;
                var isSidekick = deadPlayer.TryGetModifier<SidekickModifier>(out _);

                return isJackal || isSidekick;
            }
        };

        TeamChatPatches.ExtensionTeamChatRegistry.RegisterHandler(_handler);
    }

    public static void Unregister()
    {
        if (_handler != null)
        {
            TeamChatPatches.ExtensionTeamChatRegistry.UnregisterHandler(_handler);
            _handler = null;
        }
    }
}
