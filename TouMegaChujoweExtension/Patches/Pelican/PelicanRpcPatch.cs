using HarmonyLib;
using Hazel;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Patches.Pelican;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
public static class PelicanRpcPatch
{
    [HarmonyPostfix]
    public static void Postfix(
        PlayerControl __instance,
        [HarmonyArgument(0)] byte callId,
        [HarmonyArgument(1)] MessageReader reader)
    {
        switch (callId)
        {
            case (byte)ExtensionRpc.PelicanSwallow:
            {
                var victimId = reader.ReadByte();
                PelicanSystem.SwallowPlayer(__instance.PlayerId, victimId);
                break;
            }

            case (byte)ExtensionRpc.PelicanDigest:
            {
                PelicanSystem.DigestAll(__instance.PlayerId);
                break;
            }

            case (byte)ExtensionRpc.PelicanRelease:
            {
                PelicanSystem.ReleaseAll(__instance.PlayerId);
                break;
            }
        }
    }
}
