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
