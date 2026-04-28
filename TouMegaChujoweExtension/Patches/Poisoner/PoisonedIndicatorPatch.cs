using HarmonyLib;
using TouMegaChujoweExtension.Modules;
using TownOfUs.Utilities;

namespace TouMegaChujoweExtension.Patches.Poisoner;

[HarmonyPatch]
public static class PoisonedIndicatorPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static void AddPoisonedMark(HudManager __instance)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null) return;
        if (!localPlayer.IsImpostorAligned()) return;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null || player.Data.IsDead) continue;

            var isPoisoned = PoisonSystem.IsTargetPoisonedByPoison(player.PlayerId);
            if (!isPoisoned) continue;

            if (player.cosmetics != null && player.cosmetics.nameText != null)
            {
                var name = player.cosmetics.nameText.text;
                if (!name.Contains("%"))
                {
                    player.cosmetics.nameText.text = "<color=#00FF00>" + name + " %</color>";
                }
            }

            if (MeetingHud.Instance != null)
            {
                foreach (var pva in MeetingHud.Instance.playerStates)
                {
                    if (pva == null || pva.TargetPlayerId != player.PlayerId) continue;
                    if (pva.NameText == null) continue;

                    var meetName = pva.NameText.text;
                    if (!meetName.Contains("%"))
                    {
                        pva.NameText.text = "<color=#00FF00>" + meetName + " %</color>";
                    }
                }
            }
        }
    }
}
