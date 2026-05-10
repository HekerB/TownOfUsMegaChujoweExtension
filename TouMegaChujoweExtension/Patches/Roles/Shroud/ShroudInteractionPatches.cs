using MiraAPI.Modifiers;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using System.Linq;
using TownOfUs.Networking;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles.Shroud;

public static class ShroudInteractionPatches
{
    internal static bool IsProcessingShroud;

    public static void ExecuteShroudRetaliation(PlayerControl interactor, ShroudedModifier shroudMod)
    {
        shroudMod.WasInteractedWith = true;

        if (!(TutorialManager.InstanceExists || interactor.AmOwner)) return;

        interactor.RpcSyncShroudInteraction(shroudMod.Player.PlayerId);

        var shroudOwner = PlayerControl.AllPlayerControls.ToArray()
            .FirstOrDefault(x => x.PlayerId == shroudMod.ShroudOwnerId && !x.HasDied());
        var killer = shroudOwner ?? shroudMod.Player;

        IsProcessingShroud = true;
        try
        {
            killer.RpcSpecialMurder(
                interactor,
                isIndirect: true,
                ignoreShield: false,
                resetKillTimer: false,
                createDeadBody: true,
                teleportMurderer: false,
                showKillAnim: true,
                playKillSound: true,
                causeOfDeath: "Shroud");
        }
        finally
        {
            IsProcessingShroud = false;
        }
    }

    [MethodRpc((uint)19900, LocalHandling = RpcLocalHandling.None)]
    public static void RpcSyncShroudInteraction(this PlayerControl sender, byte shroudedPlayerId)
    {
        var player = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(x => x.PlayerId == shroudedPlayerId);
        
        if (player != null && player.TryGetModifier<ShroudedModifier>(out var mod))
        {
            mod.WasInteractedWith = true;
        }
    }
}















