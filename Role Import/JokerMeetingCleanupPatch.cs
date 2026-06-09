using System.Collections.Generic;
using HarmonyLib;
using TouMegaChujoweExtension.Buttons.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TouMegaChujoweExtension.Modules;

namespace TouMegaChujoweExtension.Patches.Joker;

[HarmonyPatch]
public static class JokerMeetingCleanupPatch
{
    private static readonly List<SurvivingCloneInfo> SurvivingClones = new();
    private static bool _needsRespawn;

    private record SurvivingCloneInfo(
        byte JokerId,
        byte AppearancePlayerId,
        UnityEngine.Vector3 WorldPosition,
        int PlacedAtMeeting);

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    public static void MeetingHud_Start_Postfix()
    {
        SurvivingClones.Clear();
        foreach (var clone in JokerCloneSystem.Clones)
        {
            if (clone == null || clone.IsPreview) continue;
            SurvivingClones.Add(new SurvivingCloneInfo(
                clone.JokerId,
                clone.AppearancePlayerId,
                clone.WorldPosition,
                clone.PlacedAtMeeting));
        }

        JokerCloneSystem.IncrementMeetingCount();
        JokerCloneSystem.ClearAll();

        var local = PlayerControl.LocalPlayer;
        if (local?.Data?.Role is JokerRole jokerRole)
            jokerRole.DestroyPiP();

        _needsRespawn = true;
        JokerPlaceCloneButton.LocalInstance?.ResetStage();
    }

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
    [HarmonyPostfix]
    public static void ExileController_WrapUp_Postfix() => RespawnSurvivingClones();

    [HarmonyPatch(typeof(ExileController), nameof(ExileController.ReEnableGameplay))]
    [HarmonyPostfix]
    public static void ExileController_ReEnableGameplay_Postfix() => RespawnSurvivingClones();

    private static void RespawnSurvivingClones()
    {
        if (!_needsRespawn) return;
        _needsRespawn = false;

        JokerCloneSystem.ClearClonesWithoutDestroying();
        if (SurvivingClones.Count == 0) return;

        foreach (var data in SurvivingClones)
        {
            var appearancePlayer = FindPlayerById(data.AppearancePlayerId);
            if (appearancePlayer == null || appearancePlayer.Data == null ||
                appearancePlayer.Data.IsDead || appearancePlayer.Data.Disconnected)
                continue;

            var joker = FindPlayerById(data.JokerId);
            if (joker == null || joker.Data == null ||
                joker.Data.IsDead || joker.Data.Disconnected)
                continue;

            JokerCloneSystem.RespawnClone(
                data.JokerId, appearancePlayer,
                data.WorldPosition, data.PlacedAtMeeting);
        }

        SurvivingClones.Clear();
    }

    private static PlayerControl? FindPlayerById(byte id)
    {
        foreach (var pc in PlayerControl.AllPlayerControls)
            if (pc != null && pc.PlayerId == id) return pc;
        return null;
    }
}