using System.Collections;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.GameOptions;
using Reactor.Utilities;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TownOfUs.Modifiers;
using TownOfUs.Utilities;
using TownOfUs.Modifiers.Game;
using AmongUs.Data;
using AmongUs.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using TownOfUs.Extensions;
using TownOfUs.Roles;
using Reactor.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class PossessorEvents
{
    [RegisterEvent]
    public static void PlayerDeathEventHandler(PlayerDeathEvent @event)
    {
        if (!AmongUsClient.Instance.AmHost ||
            !OptionGroupSingleton<PossessorOptions>.Instance.Enabled)
            return;

        var player = @event.Player;
        if (player == null ||
            player.Data == null ||
            !player.IsImpostor())
            return;

        Coroutines.Start(CoAssignPossessor(player));
    }

    private static IEnumerator CoAssignPossessor(PlayerControl deadPlayer)
    {
        yield return new WaitForSeconds(1f);

        var possessorData =
            MiscUtils.GetAssignData((RoleTypes)RoleId.Get<PossessorRole>());

        if (CustomRoleUtils.GetActiveRoles().OfType<PossessorRole>().Count() >= possessorData.Count)
            yield break;

        var isSkipped = possessorData.Chance < 100 &&
                        HashRandom.Next(101) > possessorData.Chance;

        if (isSkipped)
            yield break;

        if (!deadPlayer.CanGetGhostRole() ||
            deadPlayer.HasModifier<AllianceGameModifier>() ||
            !RoleManager.IsGhostRole(deadPlayer.Data.Role.Role))
            yield break;

        deadPlayer.RpcChangeRole(RoleId.Get<PossessorRole>());
    }
}