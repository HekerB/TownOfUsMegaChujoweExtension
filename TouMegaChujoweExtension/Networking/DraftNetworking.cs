using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using MiraAPI.Utilities;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Patches.Draft;

namespace TouMegaChujoweExtension.Modules;

public static class DraftNetworking
{
    public static void SendDraftStart(HashSet<byte> impostorIds)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var writer = AmongUsClient.Instance.StartRpcImmediately(
            PlayerControl.LocalPlayer.NetId,
            (byte)ExtensionRpc.DraftStart,
            SendOption.Reliable, -1);

        writer.Write((byte)impostorIds.Count);
        foreach (var id in impostorIds)
            writer.Write(id);

        writer.Write((byte)DraftSystem.PickOrder.Count);
        foreach (var playerId in DraftSystem.PickOrder)
            writer.Write(playerId);

        writer.Write((byte)DraftSystem.PlayerFactions.Count);
        foreach (var kvp in DraftSystem.PlayerFactions)
        {
            writer.Write(kvp.Key);
            writer.Write((byte)kvp.Value);
        }

        AmongUsClient.Instance.FinishRpcImmediately(writer);

        ReceiveDraftStart(impostorIds);
    }

    public static void ReceiveDraftStart(HashSet<byte> impostorIds)
    {
        DraftSystem.DraftPicks.Clear();
        DraftSystem.AlreadyPicked.Clear();
        DraftSystem.LocalPlayerPicked = false;
        DraftSystem.CurrentOfferedRoles = null;
        DraftSystem.SelectedAlignment = null;
        DraftSystem.DraftComplete = false;

        DraftSystem.ImpostorPlayerIds = impostorIds;
        DraftSystem.DraftActiveThisRound = true;
        DraftSystem.IsRunning = true;

        var myId = PlayerControl.LocalPlayer?.PlayerId ?? 255;
        // Info($"[DraftNetworking] Draft started. I am {(impostorIds.Contains(myId) ? "IMPOSTOR" : "CREWMATE")}");

        DraftLobbyPatch.ShowSystemMessage("<color=#FF0000>Draft Mode</color> has Started. Be Ready to Pick Your Role!");
    }

    public static void ReceiveDraftStartFromReader(MessageReader reader)
    {
        var count = reader.ReadByte();
        var impostorIds = new HashSet<byte>();
        for (int i = 0; i < count; i++)
            impostorIds.Add(reader.ReadByte());

        var orderCount = reader.ReadByte();
        DraftSystem.PickOrder.Clear();
        for (var i = 0; i < orderCount; i++)
            DraftSystem.PickOrder.Add(reader.ReadByte());

        var factionCount = reader.ReadByte();
        DraftSystem.PlayerFactions.Clear();
        for (int i = 0; i < factionCount; i++)
        {
            var playerId = reader.ReadByte();
            var faction = (DraftFaction)reader.ReadByte();
            DraftSystem.PlayerFactions[playerId] = faction;
        }

        // Info($"[DraftNetworking] Received combined draft data: {orderCount} players, {factionCount} factions.");

        ReceiveDraftStart(impostorIds);
        DraftSystem.DraftActiveThisRound = true;
        DraftSystem.IsRunning = true;

        DraftLobbyPatch.OnDraftStartedAsClient();
    }

    public static void SendPick(byte playerId, ushort roleId)
    {
        var writer = AmongUsClient.Instance.StartRpcImmediately(
            PlayerControl.LocalPlayer.NetId,
            (byte)ExtensionRpc.DraftPick,
            SendOption.Reliable, -1);

        writer.Write(playerId);
        writer.Write(roleId);

        AmongUsClient.Instance.FinishRpcImmediately(writer);

        ReceivePick(playerId, roleId);
    }

    public static void ReceivePick(byte playerId, ushort roleId)
    {
        DraftSystem.RegisterPick(playerId, roleId);

        var role = RoleManager.Instance.GetRole((RoleTypes)roleId);
        // Info($"[DraftNetworking] Player {playerId} picked {role?.GetRoleName() ?? $"Unknown({roleId})"}");

        DraftLobbyPatch.OnPickReceived(playerId, roleId);
    }

    public static void SendDraftComplete()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var writer = AmongUsClient.Instance.StartRpcImmediately(
            PlayerControl.LocalPlayer.NetId,
            (byte)ExtensionRpc.DraftComplete,
            SendOption.Reliable, -1);

        AmongUsClient.Instance.FinishRpcImmediately(writer);

        ReceiveDraftComplete();
    }

    public static void ReceiveDraftComplete()
    {
        DraftSystem.IsRunning = false;
        DraftSystem.DraftComplete = true;
        // Info("[DraftNetworking] Draft complete!");

        DraftLobbyPatch.ShowSystemMessage("<color=#00FF00>Draft Complete!</color> The game is starting soon.");
    }

    public static void SendDraftCancel()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var writer = AmongUsClient.Instance.StartRpcImmediately(
            PlayerControl.LocalPlayer.NetId,
            (byte)ExtensionRpc.DraftCancel,
            SendOption.Reliable, -1);

        AmongUsClient.Instance.FinishRpcImmediately(writer);

        ReceiveDraftCancel();
    }

    public static void ReceiveDraftCancel()
    {
        DraftSystem.Reset();
        DraftLobbyPatch.ForceCancelDraft();
        // Info("[DraftNetworking] Draft cancelled by host.");

        DraftLobbyPatch.ShowSystemMessage("<color=#FF4444>Draft Cancelled</color> by the Host.");
    }
}
