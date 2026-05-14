using HarmonyLib;
using Hazel;

namespace TouMegaChujoweExtension.Patches.Draft;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
public static class DraftRpcPatch
{
    [HarmonyPostfix]
    public static void Postfix(
        PlayerControl __instance,
        [HarmonyArgument(0)] byte callId,
        [HarmonyArgument(1)] MessageReader reader)
    {
        switch (callId)
        {
            case (byte)ExtensionRpc.DraftStart:
                DraftNetworking.ReceiveDraftStartFromReader(reader);
                break;

            case (byte)ExtensionRpc.DraftPick:
            {
                var playerId = reader.ReadByte();
                var roleId = reader.ReadUInt16();
                DraftNetworking.ReceivePick(playerId, roleId);
                break;
            }

            case (byte)ExtensionRpc.DraftComplete:
                DraftNetworking.ReceiveDraftComplete();
                break;

            case (byte)ExtensionRpc.DraftCancel:
                DraftNetworking.ReceiveDraftCancel();
                break;
        }
    }
}