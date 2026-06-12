using System;
using MiraAPI.GameOptions;
using TownOfUs.Patches.Options;
using TownOfUs.Options;
using TownOfUs.Utilities;
using TownOfUs;
using TouMegaChujoweExtension.Roles.Classic.Crewmate;
using TouMegaChujoweExtension.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles.Agent
{
    public static class AgentTeamChatRegistration
    {
        private static TeamChatPatches.ExtensionTeamChatHandler? _handler;

        public static void Register()
        {
            try
            {
                if (_handler != null)
                {
                    TeamChatPatches.ExtensionTeamChatRegistry.UnregisterHandler(_handler);
                }

                _handler = new TeamChatPatches.ExtensionTeamChatHandler
                {
                    Priority = 30,
                    IsForced = false,
                    IsChatAvailable = IsAgentImpostorChatAvailable,
                    SendMessage = (sender, message) => TeamChatPatches.RpcSendImpTeamChat(sender, message),
                    GetDisplayText = () => "Impostor Chat",
                    DisplayTextColor = TownOfUsColors.ImpSoft,
                    CanDeadPlayerSee = deadPlayer => deadPlayer != null &&
                                                     deadPlayer.IsRole<AgentRole>() &&
                                                     AgentUtils.CanUseImpostorChat
                };

                TeamChatPatches.ExtensionTeamChatRegistry.RegisterHandler(_handler);
            }
            catch (Exception)
            {
            }
        }

        private static bool IsAgentImpostorChatAvailable()
        {
            var local = PlayerControl.LocalPlayer;
            if (!MeetingHud.Instance ||
                local == null ||
                !local.IsRole<AgentRole>() ||
                !AgentUtils.CanUseImpostorChat)
            {
                return false;
            }

            var genOpt = OptionGroupSingleton<GeneralOptions>.Instance;
            return genOpt is { FFAImpostorMode: false, ImpostorChat.Value: true };
        }
    }
}
