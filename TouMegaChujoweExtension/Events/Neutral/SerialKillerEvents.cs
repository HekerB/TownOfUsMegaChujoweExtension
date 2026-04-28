using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Events;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using TouMegaChujoweExtension.Buttons.Neutral;

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
            // Flag the kill as processing so ExitVentPostfix won't clear the target mid-kill
            SerialKillerVentKillSystem.SetProcessingVentKill(source.PlayerId, true);

            VentOccupancySystem.ClearForPlayer(source.PlayerId);
            VentOccupancySystem.ClearForPlayer(target.PlayerId);

            // Grant the Serial Killer 2 escape vent usages so they can exit now and exit one more time to escape
            SerialKillerVentKillSystem.SetEscapeVentUsages(source.PlayerId, 2);

            // Clear the processing flag
            SerialKillerVentKillSystem.SetProcessingVentKill(source.PlayerId, false);
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
            if (killer.AmOwner && killer.inVent)
            {
                if (killer.inVent && Vent.currentVent != null)
                {
                    killer.MyPhysics.RpcExitVent(Vent.currentVent.Id);
                    killer.MyPhysics?.ExitAllVents();
                }

                killer.inVent = false;
                Vent.currentVent = null;
            }

            if (victim.AmOwner && victim.inVent)
            {
                if (victim.inVent && Vent.currentVent != null)
                {
                    victim.MyPhysics.RpcExitVent(Vent.currentVent.Id);
                    victim.MyPhysics?.ExitAllVents();
                }

                victim.inVent = false;
                Vent.currentVent = null;
            }

            SerialKillerVentKillSystem.ClearForPlayer(killer.PlayerId);
        }

        if (killer.TryGetModifier<SerialKillerManiacModifier>(out var maniacMod))
        {
            maniacMod.ResetOnKill();
        }

        var options = OptionGroupSingleton<SerialKillerOptions>.Instance;
        if (!options.KillCooldownReductionEnabled)
        {
            return;
        }

        if (killer.AmOwner)
        {
            CustomButtonSingleton<SerialKillerKillButton>.Instance.ResetCooldownAndOrEffect();
        }
    }
}