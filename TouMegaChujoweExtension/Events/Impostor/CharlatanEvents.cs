using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using TouMegaChujoweExtension.Buttons.Impostor;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Buttons;
using TownOfUs.Modules;
using TownOfUs.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class CharlatanEvents
{
    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (!@event.TriggeredByIntro)
        {
            return;
        }

        CharlatanConcealSystem.ClearAll();
        CharlatanDeceiveSystem.ClearAll();
    }

    [RegisterEvent]
    public static void GameEndEventHandler(GameEndEvent @event)
    {
        CharlatanConcealSystem.ClearAll();
        CharlatanDeceiveSystem.ClearAll();
    }

    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        var source = @event.Source;
        if (source == null || source.Data.Role is not CharlatanRole)
        {
            return;
        }

        var target = @event.Target;
        if (target == null)
        {
            return;
        }

        var options = OptionGroupSingleton<CharlatanOptions>.Instance;
        var baseDuration = options.DeceiveBaseDuration;
        var increasePerKill = options.DeceiveDurationIncreasePerKill;

        var killCount = GameHistory.KilledPlayers.Count(k => k.KillerId == source.PlayerId);
        var duration = baseDuration + (killCount * increasePerKill);

        var body = Object.FindObjectsOfType<DeadBody>().FirstOrDefault(x => x.ParentId == target.PlayerId);
        if (body != null)
        {
            CharlatanDeceiveSystem.ActivateDeceive(source.PlayerId, target.PlayerId, duration);
        }

        if (source.AmOwner)
        {
            var concealButton = CustomButtonSingleton<CharlatanConcealButton>.Instance;
            if (concealButton != null && options.ResetKillConcealCooldownsTogether)
            {
                concealButton.Timer = concealButton.Cooldown;
            }
        }
    }
}
