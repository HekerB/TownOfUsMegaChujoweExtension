using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Events;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class SerialKillerEvents
{
    [RegisterEvent]
    public static void RoundStartHandler(RoundStartEvent @event)
    {
        ModifierUtils.GetActiveModifiers<SerialKillerManiacModifier>().Do(x => x.OnRoundStart());
    }

    [RegisterEvent]
    public static void PlayerDeathEventHandler(PlayerDeathEvent @event)
    {
        var victim = @event.Player;
        if (victim == null)
        {
            return;
        }

        VentOccupancySystem.ClearForPlayer(victim.PlayerId);
    }

    [RegisterEvent]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        var source = @event.Source;
        var target = @event.Target;

        if (source == null || target == null || !source.IsRole<SerialKillerRole>() || @event.IsCancelled)
        {
            return;
        }

        if (SerialKillerVentKillSystem.TryGetVentKillTarget(source.PlayerId, out var ventTarget) && ventTarget != null && ventTarget.PlayerId == target.PlayerId)
        {
            if (source.AmOwner && source.inVent)
            {
                if (source.inVent && Vent.currentVent != null)
                {
                    source.MyPhysics.RpcExitVent(Vent.currentVent.Id);
                    source.MyPhysics?.ExitAllVents();
                }

                source.inVent = false;
                Vent.currentVent = null;
            }

            if (target.AmOwner && target.inVent)
            {
                if (target.HasDied())
                {
                    return;
                }

                if (target.inVent && Vent.currentVent != null)
                {
                    target.MyPhysics.RpcExitVent(Vent.currentVent.Id);
                    target.MyPhysics?.ExitAllVents();
                }

                target.inVent = false;
                Vent.currentVent = null;
            }

            VentOccupancySystem.ClearForPlayer(source.PlayerId);
            VentOccupancySystem.ClearForPlayer(target.PlayerId);

            if (!source.HasModifier<SerialKillerNoVentModifier>())
            {
                source.AddModifier<SerialKillerNoVentModifier>();
            }
        }

        if (source.AmOwner)
        {
            DeathHandlerModifier.UpdateDeathHandlerImmediate(
                target,
                TouLocale.Get("DiedToSerialKiller"),
                DeathEventHandlers.CurrentRound,
                (!MeetingHud.Instance && !ExileController.Instance)
                    ? DeathHandlerOverride.SetTrue
                    : DeathHandlerOverride.SetFalse,
                TouLocale.GetParsed("DiedByStringBasic")
                    .Replace("<player>", source.Data.PlayerName),
                lockInfo: DeathHandlerOverride.SetTrue
            );
        }
    }

    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        var killer = @event.Source;
        var victim = @event.Target;

        if (killer == null || victim == null || !killer.IsRole<SerialKillerRole>())
        {
            return;
        }

        var serialKiller = killer.GetRole<SerialKillerRole>();
        if (serialKiller == null)
        {
            return;
        }

        if (MeetingHud.Instance || ExileController.Instance)
        {
            return;
        }

        if (SerialKillerVentKillSystem.TryGetVentKillTarget(killer.PlayerId, out var ventTarget) && ventTarget != null && ventTarget.PlayerId == victim.PlayerId)
        {
            SerialKillerVentKillSystem.ClearForPlayer(killer.PlayerId);
        }

        if (killer.TryGetModifier<SerialKillerManiacModifier>(out var maniacMod))
        {
            maniacMod.ResetOnKill();
        }
    }
}