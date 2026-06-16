using HarmonyLib;
using MiraAPI.Modifiers;
using System.Collections.Generic;

namespace TouMegaChujoweExtension.Patches.BugFixes;

[HarmonyPatch]
public static class ModifierCleanupPatch
{
    private static readonly HashSet<byte> ClearedPlayers = new();

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    [HarmonyPostfix]
    public static void OnShipStatusStartPostfix()
    {
        ClearedPlayers.Clear();
    }

    public static void ClearAllPlayerModifiers()
    {
        try
        {
            if (PlayerControl.AllPlayerControls == null) return;

            foreach (var player in PlayerControl.AllPlayerControls.ToArray())
            {
                if (player != null && player.gameObject != null)
                {
                    if (ClearedPlayers.Contains(player.PlayerId)) continue;

                    var component = player.GetComponent<ModifierComponent>();
                    if (component != null)
                    {
                        if (component.ActiveModifiers.Count > 0)
                        {
                            player.ClearModifiers();
                            ClearedPlayers.Add(player.PlayerId);
                        }
                    }
                }
            }
            Info("Cleared all player modifiers successfully.");
        }
        catch (System.Exception ex)
        {
            Error($"Error while clearing player modifiers: {ex}");
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    [HarmonyPostfix]
    public static void OnGameEndPostfix()
    {
        ClearAllPlayerModifiers();
    }

    [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
    [HarmonyPostfix]
    public static void LobbyBehaviourStartPostfix()
    {
        ClearAllPlayerModifiers();
    }
}
