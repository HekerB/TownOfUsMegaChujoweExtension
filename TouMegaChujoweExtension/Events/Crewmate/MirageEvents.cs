using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Events.Crewmate;

public static class MirageEvents
{


    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (!@event.TriggeredByIntro)
        {
            return;
        }



        if (PlayerControl.LocalPlayer?.Data?.Role is not MirageRole)
        {
            return;
        }

        var btn = CustomButtonSingleton<MirageDecoyButton>.Instance;
        btn.SetUses((int)OptionGroupSingleton<MirageOptions>.Instance.InitialUses);
    }

    [RegisterEvent]
    public static void CompleteTaskEvent(CompleteTaskEvent @event)
    {
        // Task reward removed as per Mirage rework
    }

    [RegisterEvent]
    public static void OnMeetingStart(StartMeetingEvent @event)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.Data?.Role is not MirageRole)
        {
            return;
        }

        if (!OptionGroupSingleton<MirageOptions>.Instance.RevealInteractorRole)
        {
            return;
        }

        var title = $"<color=#{ColorUtility.ToHtmlStringRGBA(TouExtensionColors.Mirage)}>Mirage Feedback</color>";
        string msg;

        if (MirageRole.TriggeredRoles.TryGetValue(localPlayer.PlayerId, out var roles) && roles.Count > 0)
        {
            TownOfUs.Utilities.Extensions.Shuffle(roles);
            var rolesStr = string.Join(", ", roles);
            msg = $"Roles seen interacting with your decoy:\n{rolesStr}";
        }
        else
        {
            msg = "No players interacted with your decoy";
        }

        MiscUtils.AddFakeChat(localPlayer.Data, title, msg, false, true);

        MirageRole.TriggeredRoles.Clear();
    }
}