using HarmonyLib;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using UnityEngine;
using System.Collections;
using Reactor.Utilities;

namespace TouMegaChujoweExtension.Patches.Roles.Jackal;

[HarmonyPatch]
public static class JackalIntroPatch
{
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
    [HarmonyPostfix]
    public static void BeginCrewmatePostfix(IntroCutscene __instance)
    {
        AddRecruitBlurb(__instance);
    }

    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
    [HarmonyPostfix]
    public static void BeginImpostorPostfix(IntroCutscene __instance)
    {
        AddRecruitBlurb(__instance);
    }

    private static void AddRecruitBlurb(IntroCutscene instance)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || instance == null) return;

        // Check if already has the modifier
        if (localPlayer.TryGetModifier<SidekickModifier>(out _))
        {
            ShowRecruitIntroText(instance);
            return;
        }

        // Check PendingAssignments (modifier might not be synced yet during intro)
        if (JackalStartPatch.PendingAssignments.ContainsKey(localPlayer.PlayerId))
        {
            ShowRecruitIntroText(instance);
            return;
        }

        // Start a coroutine to check again after a delay (assignment runs after 3s)
        Coroutines.Start(CoDelayedRecruitNotification(localPlayer));
    }

    private static void ShowRecruitIntroText(IntroCutscene instance)
    {
        var jackalInfo = TouLocale.GetParsed("SidekickIntroBlurb");
        
        if (instance.RoleText != null)
        {
            instance.RoleText.text = SidekickModifier.ShortName;
            instance.RoleText.color = TouExtensionColors.Jackal;
        }

        if (instance.BackgroundBar != null)
        {
            instance.BackgroundBar.material.color = TouExtensionColors.Jackal;
        }

        if (!string.IsNullOrEmpty(jackalInfo))
        {
            if (instance.RoleBlurbText != null)
            {
                instance.RoleBlurbText.text = $"<color=#{ColorUtility.ToHtmlStringRGBA(TouExtensionColors.Jackal)}>{jackalInfo}</color>";
            }
        }
    }

    private static IEnumerator CoDelayedRecruitNotification(PlayerControl localPlayer)
    {
        // Wait for the assignment to complete (it has a 3s delay)
        yield return new WaitForSeconds(5f);

        // Check again after assignment should have run
        if (localPlayer != null && localPlayer.Pointer != System.IntPtr.Zero && localPlayer.TryGetModifier<SidekickModifier>(out var mod) && !mod.WasNotified)
        {
            mod.WasNotified = true;

            // Show notification since we missed the intro
            Helpers.CreateAndShowNotification(
                TouLocale.Get("ExtensionSidekickRecruitedAlert"),
                TouExtensionColors.Jackal,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.SidekickModifierIcon.LoadAsset()
            ).AdjustNotification();
            Reactor.Utilities.Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Jackal));
        }
    }
}
