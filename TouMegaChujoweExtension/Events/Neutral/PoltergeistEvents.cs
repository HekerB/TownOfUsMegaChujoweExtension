using System;
using System.Collections;
using System.Linq;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.GameOptions;
using Reactor.Utilities;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Modifiers;
using TownOfUs.Utilities;
using TownOfUs.Modifiers.Game;
using AmongUs.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using TownOfUs.Extensions;
using TownOfUs.Roles;
using TownOfUs.Modules;
using UnityEngine;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class PoltergeistEvents
{
    [RegisterEvent(10002)]
    public static void EjectionEventHandler(EjectionEvent @event)
    {
        if (!AmongUsClient.Instance.AmHost)
            return;

        var poltergeistData = MiscUtils.GetAssignData((RoleTypes)RoleId.Get<PoltergeistRole>());
        if (poltergeistData.Count == 0)
            return;

        var exiled = @event.ExileController?.initData?.networkedPlayer?.Object;
        Coroutines.Start(CoAssignPoltergeist(exiled));
    }

    private static IEnumerator CoAssignPoltergeist(PlayerControl? exiled)
    {
        yield return new WaitForSeconds(1.5f);

        var poltergeistData = MiscUtils.GetAssignData((RoleTypes)RoleId.Get<PoltergeistRole>());

        if (CustomRoleUtils.GetActiveRoles().OfType<PoltergeistRole>().Count() >= poltergeistData.Count)
            yield break;

        var isSkipped = poltergeistData.Chance < 100 && HashRandom.Next(101) > poltergeistData.Chance;
        if (isSkipped)
            yield break;

        var deadNeutrals = PlayerControl.AllPlayerControls.ToArray().Where(x =>
            (x.Data.IsDead || x == exiled) &&
            x.GetRoleWhenAlive().IsNeutral() &&
            !x.GetRoleWhenAlive().DidWin(GameOverReason.CrewmatesByVote) &&
            x.CanGetGhostRole() &&
            !x.HasModifier<AllianceGameModifier>() &&
            !(x.GetRoleWhenAlive() is ITownOfUsRole touRole && touRole.WinConditionMet())
        ).ToList();

        if (deadNeutrals.Count > 0)
        {
            TownOfUs.Utilities.Extensions.Shuffle(deadNeutrals);
            var player = deadNeutrals.FirstOrDefault();
            if (player != null)
            {
                player.RpcChangeRole(RoleId.Get<PoltergeistRole>());
            }
        }
    }
}
