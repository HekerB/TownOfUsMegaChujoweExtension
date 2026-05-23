using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles.Jackal;

[HarmonyPatch]
public static class JackalNoTasksPatch
{
    [HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.RpcSetTasks))]
    [HarmonyPrefix]
    public static void RpcSetTasksPrefix(NetworkedPlayerInfo __instance, ref Il2CppStructArray<byte> taskTypeIds)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (taskTypeIds == null || __instance == null) return;

        var player = __instance.Object;
        if (player != null && (player.GetRole<JackalRole>() != null || (player.Data?.Role != null && player.Data.Role is JackalRole)))
        {
            taskTypeIds = new Il2CppStructArray<byte>(0);
        }
    }
}
