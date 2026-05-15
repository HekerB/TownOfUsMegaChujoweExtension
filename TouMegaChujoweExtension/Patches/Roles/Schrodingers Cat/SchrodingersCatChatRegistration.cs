using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TownOfUs.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TouMegaChujoweExtension.Utilities;
using TownOfUs.Patches.Options;
using static TownOfUs.Patches.Options.TeamChatPatches;
using UnityEngine;
using MiraAPI.Modifiers;

namespace TouMegaChujoweExtension.Patches.SchrodingersCat;

public static class SchrodingersCatChatRegistration
{
    private static ExtensionTeamChatHandler? _handler;

    public static void Register()
    {
        if (_handler != null) return;

        _handler = new ExtensionTeamChatHandler
        {
            Priority = 65,
            IsChatAvailable = () =>
            {
                var localPlayer = PlayerControl.LocalPlayer;
                if (localPlayer == null || localPlayer.Data == null) return false;
                if (!MiraAPI.GameOptions.OptionGroupSingleton<TouMegaChujoweExtension.Options.GeneralOptions>.Instance.CatChat) return false;

                if (localPlayer.IsRole<SchrodingersCatRole>())
                {
                    return localPlayer.GetRole<SchrodingersCatRole>().IsAdopted;
                }

                // Are we the partner of a cat?
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p != null && p.IsRole<SchrodingersCatRole>() && p.GetRole<SchrodingersCatRole>().TeammateId == localPlayer.PlayerId)
                    {
                        return true;
                    }
                }

                return false;
            },
            SendMessage = (sender, message) =>
            {
                SchrodingersCatRole.RpcSendCatChat(sender, message);
            },
            GetDisplayText = () =>
            {
                var localPlayer = PlayerControl.LocalPlayer;
                if (localPlayer.IsRole<SchrodingersCatRole>())
                {
                    var cat = localPlayer.GetRole<SchrodingersCatRole>();
                    var partner = MiscUtils.PlayerById(cat.TeammateId);
                    if (partner != null)
                    {
                        if (partner.Data.Role.IsImpostor) return "Impostor Chat (Cat)";
                        if (partner.IsRole<VampireRole>()) return "Vampire Chat (Cat)";
                        if (partner.HasModifier<TownOfUs.Modifiers.Game.Alliance.LoverModifier>()) return "Lover Chat (Cat)";
                        return "Team Chat";
                    }
                }
                return "Cat Chat";
            },
            DisplayTextColor = TouExtensionColors.SchrodingersCat,
            BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.85f),
            CanDeadPlayerSee = (deadPlayer) =>
            {
                if (deadPlayer.IsRole<SchrodingersCatRole>()) return true;
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p != null && p.IsRole<SchrodingersCatRole>() && p.GetRole<SchrodingersCatRole>().TeammateId == deadPlayer.PlayerId)
                    {
                        return true;
                    }
                }
                return false;
            }
        };

        ExtensionTeamChatRegistry.RegisterHandler(_handler);
    }

    public static void Unregister()
    {
        if (_handler != null)
        {
            ExtensionTeamChatRegistry.UnregisterHandler(_handler);
            _handler = null;
        }
    }
}
