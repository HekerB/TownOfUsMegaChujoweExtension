using HarmonyLib;
using Hazel;
using System;

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
        // Everything here runs straight off HandleRpc. An exception escaping this method goes into the
        // native trampoline and kills processing of the REST OF THE PACKET, which drops the client with
        // no message and nothing in the log. A truncated or malformed payload must therefore never
        // leave this method.
        try
        {
            switch (callId)
            {
                case (byte)ExtensionRpc.DraftStart:
                    DraftNetworking.ReceiveDraftStartFromReader(reader);
                    break;

                case (byte)ExtensionRpc.DraftPick:
                {
                    // Guard the reads themselves - a short packet would otherwise throw inside Hazel.
                    if (reader.BytesRemaining < 3)
                    {
                        Warning($"[Draft] DraftPick payload too short ({reader.BytesRemaining} B) - ignored.");
                        break;
                    }

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
        catch (Exception ex)
        {
            Error($"[Draft] Exception handling RPC {callId} - swallowed so the client is not dropped: {ex}");
        }
    }
}
