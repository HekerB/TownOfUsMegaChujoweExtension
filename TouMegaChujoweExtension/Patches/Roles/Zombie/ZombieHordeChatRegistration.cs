using MiraAPI.Modifiers;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs;
using TownOfUs.Extensions;
using TownOfUs.Patches.Options;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Neutral;

public static class ZombieHordeChatRegistration
{
    private static TeamChatPatches.ExtensionTeamChatHandler? _handler;

    public static void Register()
    {
        if (_handler != null) return;

        _handler = new TeamChatPatches.ExtensionTeamChatHandler
        {
            Priority = 60,
            IsChatAvailable = () =>
            {
                var options = OptionGroupSingleton<ZombieOptions>.Instance;
                if (!options.PrivateChat || !MeetingHud.Instance) return false;

                var localPlayer = PlayerControl.LocalPlayer;
                if (localPlayer == null || localPlayer.Data == null) return false;

                return localPlayer.GetModifiers<ZombieModifier>().Any();
            },
            SendMessage = (sender, message) =>
            {
                ZombieRole.RpcSendHordeChat(sender, message);
            },
            GetDisplayText = () => "Horde Chat",
            DisplayTextColor = TouExtensionColors.Zombie,
            BackgroundColor = new Color(0.1f, 0.2f, 0.1f, 0.8f),
            CanDeadPlayerSee = (deadPlayer) =>
            {
                var genOpt = OptionGroupSingleton<TownOfUs.Options.GeneralOptions>.Instance;
                return genOpt.TheDeadKnow && deadPlayer.GetModifiers<ZombieModifier>().Any();
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
