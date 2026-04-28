using MiraAPI.Modifiers;
using TownOfUs;
using TownOfUs.Modifiers.Crewmate;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs.Patches.Options;
using UnityEngine;
using TouMegaChujoweExtension.Modifiers;
using static TownOfUs.Patches.Options.TeamChatPatches;
using HarmonyLib;
using Reactor.Utilities.Extensions;

namespace TouMegaChujoweExtension.Patches.Lovers;

public static class LoverMeetingChatRegistration
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
            Priority = 45,
            IsForced = false,
            IsChatAvailable = () =>
            {
                var genOpt = MiraAPI.GameOptions.OptionGroupSingleton<Options.GeneralOptions>.Instance;
                if (genOpt == null || !genOpt.LoversChat || !MeetingHud.Instance)
                {
                    return false;
                }

                var localPlayer = PlayerControl.LocalPlayer;
                if (localPlayer == null || localPlayer.Data == null)
                {
                    return false;
                }

                if (!localPlayer.HasModifier<LoverModifier>())
                {
                    return false;
                }

                // Jailed lover (not jailor!) cannot use lover chat
                if (MeetingHud.Instance && localPlayer.HasModifier<JailedModifier>())
                {
                    return false;
                }

                // Pelican block
                if (localPlayer.HasModifier<PelicanSwallowedModifier>())
                {
                    return false;
                }

                return true;
            },
            SendMessage = (sender, message) =>
            {
                TeamChatPatches.RpcSendLoveChat(sender, message);
            },
            GetDisplayText = () => "Lover Chat",
            DisplayTextColor = new Color(0.8f, 0.1f, 0.5f, 1f),
            BackgroundColor = new Color(1f, 0.8f, 0.9f, 0.95f),
            CanDeadPlayerSee = (deadPlayer) =>
            {
                return deadPlayer.HasModifier<LoverModifier>();
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

[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetCosmetics))]
public static class LoverChatBubblePatch
{
    public static void Postfix(ChatBubble __instance, NetworkedPlayerInfo info)
    {
        var rawText = System.Text.RegularExpressions.Regex.Replace(__instance.NameText.text, "<.*?>", string.Empty);

        // If the bubble text contains "(Lover Chat)", apply pink bubble style
        if (rawText.Contains("(Lover Chat)"))
        {
            __instance.Background.sprite = TownOfUs.Assets.TouChatAssets.LoveBubble.LoadAsset();
            __instance.Background.color = Color.white;
            __instance.TextArea.color = Color.black; 
        }
        // Ensure Lawyer style remains intact as well
        else if (rawText.Contains("(Lawyer)"))
        {
            __instance.Background.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
            __instance.TextArea.color = Color.white;
        }
    }
}
