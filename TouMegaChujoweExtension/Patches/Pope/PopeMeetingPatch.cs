using System.Text;
using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs;
using TownOfUs.Options;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Patches.Pope;

[HarmonyPatch]
public static class PopeMeetingPatch
{
    private static float _lastUpdate;

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    [HarmonyPostfix]
    public static void UpdatePostfix(MeetingHud __instance)
    {
        if (UnityEngine.Time.time - _lastUpdate < 0.2f) return;
        _lastUpdate = UnityEngine.Time.time;
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null) return;

        var genOpt = OptionGroupSingleton<GeneralOptions>.Instance;
        bool isGhost = local.Data.IsDead;


        foreach (var pva in __instance.playerStates)
        {
            if (pva?.NameText == null) continue;
            var target = MiscUtils.PlayerById(pva.TargetPlayerId);


            // Symbols are now handled by UpdateRoleNameText calling UpdateTargetSymbols/UpdateProtectionSymbols
            // every frame. We don't need to append them here, which was causing flickering due to the 0.2s timer
            // fighting with the per-frame resets in HudManagerPatches.
        }
    }

    private static PlayerControl? GetDeathNoteTarget()
    {
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc.TryGetModifier<DeathNoteModifier>(out var dnMod))
            {
                return dnMod.CursedTarget;
            }
        }
        return null;
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    public static void StartPostfix()
    {
        var sabId = (SystemTypes)PopeJudgementSystem.SabotageId;
        if (ShipStatus.Instance == null || !ShipStatus.Instance.Systems.ContainsKey(sabId)) return;

        var sabotage = ShipStatus.Instance.Systems[sabId].Cast<PopeJudgementSystem>();
        if (!sabotage.IsActive) return;

        var reportBuilder = new StringBuilder();
        var popeName = TouLocale.Get("ExtensionRolePope");
        var text = TouLocale.GetParsed("ExtensionRolePopeGlobalWarning")
            .Replace("<role>", $"#{popeName.ToLowerInvariant().Replace(" ", "-")}");

        reportBuilder.Append(TownOfUsPlugin.Culture,
            $"{text.Replace("<time>", $"{(int)sabotage.TimeRemaining + 1}")}");

        var report = reportBuilder.ToString();

        if (HudManager.Instance && report.Length > 0)
        {
            var title = $"<color=#{ColorUtility.ToHtmlStringRGBA(TouExtensionColors.Pope)}>{TouLocale.Get("ExtensionRolePopeMessageTitle")}</color>";
            MiscUtils.AddFakeChat(PlayerControl.LocalPlayer.Data, title, report, false, true);
        }
    }
}
