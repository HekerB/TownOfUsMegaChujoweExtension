using System;
using System.Collections;
using System.Linq;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.GameOptions;
using Reactor.Utilities;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
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

namespace TouMegaChujoweExtension.Events.Impostor;

public static class PossessorEvents
{
    [RegisterEvent(10001)]
    public static void EjectionEventHandler(EjectionEvent @event)
    {
        if (!AmongUsClient.Instance.AmHost ||
            !OptionGroupSingleton<PossessorOptions>.Instance.Enabled)
            return;

        var exiled = @event.ExileController?.initData?.networkedPlayer?.Object;
        Coroutines.Start(CoAssignPossessor(exiled));
    }

    private static IEnumerator CoAssignPossessor(PlayerControl? exiled)
    {
        yield return new WaitForSeconds(1.5f);

        var possessorData = MiscUtils.GetAssignData((RoleTypes)RoleId.Get<PossessorRole>());

        if (CustomRoleUtils.GetActiveRoles().OfType<PossessorRole>().Count() >= possessorData.Count)
            yield break;

        var isSkipped = possessorData.Chance < 100 && HashRandom.Next(101) > possessorData.Chance;
        if (isSkipped)
            yield break;

        var deadImpostors = PlayerControl.AllPlayerControls.ToArray().Where(x =>
            (x.Data.IsDead || x == exiled) &&
            x.GetRoleWhenAlive().IsImpostor() &&
            x.CanGetGhostRole() &&
            !x.HasModifier<AllianceGameModifier>()
        ).ToList();

        if (deadImpostors.Count > 0)
        {
            TownOfUs.Utilities.Extensions.Shuffle(deadImpostors);
            var player = deadImpostors.FirstOrDefault();
            if (player != null)
            {
                player.RpcChangeRole(RoleId.Get<PossessorRole>());
            }
        }
    }

    [RegisterEvent]
    public static void CompleteTaskEventHandler(CompleteTaskEvent @event)
    {
        if (@event.Player?.Data?.Role is not PossessorRole possessor)
            return;

        possessor.CheckTaskRequirements();
    }
}